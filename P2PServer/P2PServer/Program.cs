using P2PLibrary;
using System;
using System.Linq;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Net;

namespace P2PServer
{
    class Program
    {
        static IPEndPoint GetLocalAddress()
        {
            var entry = System.Net.Dns.GetHostEntry("lovettsoftware.com");
            var addr = (from a in entry.AddressList where a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork select a).FirstOrDefault();
            using (System.Net.Sockets.Socket s = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp))
            {
                s.Connect(new IPEndPoint(addr.Address, 80));
                return (IPEndPoint)s.LocalEndPoint;
            }
        }

        static void PublishEndPoint(string name)
        {
            IPEndPoint localAddress = GetLocalAddress();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://lovettsoftware.com/p2pserver.aspx?type=add");
            request.Method = "POST";
            request.ContentType = "text/json";
            using (Stream s = request.GetRequestStream())
            {

                Client c = new Client()
                {
                    Date = DateTime.Now.ToUniversalTime(),
                    Name = name,
                    LocalAddress = localAddress.Address.ToString(),
                    LocalPort = localAddress.Port.ToString()
                };

                string json = JsonConvert.SerializeObject(c);
                byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
                s.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using (var rs = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(rs, Encoding.UTF8))
                    {
                        string result = reader.ReadToEnd();
                        Console.WriteLine(result);
                    }
                }
            }
            else
            {
                Console.WriteLine("Request failed: " + response.StatusDescription);
            }

        }

        static void FindEndPoint(string name)
        {
            IPEndPoint localAddress = GetLocalAddress();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://lovettsoftware.com/p2pserver.aspx?type=find");
            request.Method = "POST";
            request.ContentType = "text/json";
            using (Stream s = request.GetRequestStream())
            {

                Client c = new Client()
                {
                    Date = DateTime.Now.ToUniversalTime(),
                    Name = name
                };

                string json = JsonConvert.SerializeObject(c);
                byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
                s.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using (var rs = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(rs, Encoding.UTF8))
                    {
                        string result = reader.ReadToEnd();
                        Console.WriteLine(result);
                    }
                }
            }
            else
            {
                Console.WriteLine("Request failed: " + response.StatusDescription);
            }
        }

        static void Main(string[] args)
        {
            //PublishEndPoint("Ulysses");
            FindEndPoint("Ulysses");
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

                Console.WriteLine("Client added: {0}", c.Id);

                c = model.FindClientById(c.Id);
                Console.WriteLine("Found client: " + c.Name);

                model.RemoveOldEntries();

                c = model.FindClientById(c.Id);
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
