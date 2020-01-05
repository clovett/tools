using P2PLibrary;
using System;
using System.Linq;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Net;
using System.Threading;

namespace P2PServer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 2)
            {
                string localName = args[0];
                string remoteName = args[1];

                P2PSocket c = new P2PSocket();
                c.Connect("lovettsoftware.com");
                c.PublishEndPoint(localName);
                Client remote = null;
                Console.WriteLine("Waiting for remote system...");
                while (remote == null)
                {
                    try
                    {
                        remote = c.FindEndPoint(remoteName);
                    } 
                    catch (Exception)
                    {
                        // not found!
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                
                Console.WriteLine("Starting local TCP server...");
                c.ListenAsync();

                System.Threading.Thread.Sleep(100);

                Console.WriteLine("Connecting to remote TCP server...");
                c.P2PConnectAsync();

                System.Threading.Thread.Sleep(500);
                Console.Write("press enter to continue...");
                Console.ReadLine();

                c.RemoveEndPoint(localName);
                c.Close();
            }
            else if (args.Length == 1)
            {
                DatabaseUnitTest(args[0]);
            }

            return;
        }

        static void DatabaseUnitTest(string connectionString)
        { 
            // This code can be used to unit test the database.
            Client c = new Client()
            {
                Date = DateTime.Today.AddDays(-3).ToUniversalTime(),
                Name = "Ulysses",
                LocalAddress = "192.168.1.18",
                LocalPort = "2777",
                RemoteAddress = "24.22.219.243",
                RemotePort = "5151"
            };

            string json = JsonConvert.SerializeObject(c);

            Client d = (Client)JsonConvert.DeserializeObject(json, typeof(Client));

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                Model model = new Model(connection);

                model.EnsureDatabase();

                Console.WriteLine("Adding new client");

                var date = DateTime.Today.AddDays(-3).ToUniversalTime();
                var ds = date.ToString();

                model.AddClient(c);

                Console.WriteLine("Client added: {0}", c.Name);

                c = model.FindClientByName(c.Name);
                Console.WriteLine("Found client: " + c.Name);

                model.RemoveOldEntries();

                c = model.FindClientByName(c.Name);
                if (c == null)
                {
                    Console.WriteLine("Old client removed");
                }
            }
            return;
        }

        static void JSonTest()
        {

            Newtonsoft.Json.JsonTextWriter writer = new JsonTextWriter(Console.Out);
            writer.WriteStartObject();
            writer.WritePropertyName("error");
            writer.WriteValue("this is a \"test\"");
            writer.WriteEndObject();

        }
    }

}
