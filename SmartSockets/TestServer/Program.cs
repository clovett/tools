using System;
using LovettSoftware.Networking.SmartSockets;
using ConsoleInterface;
using System.Threading.Tasks;

namespace ConsoleServer
{
    class Program
    {
        const string Name = "CoyoteTester";

        static void Main(string[] args)
        {
            Console.WriteLine("Starting Server:");
            Program p = new Program();
            p.Start();
        }

        void Start()
        {
            SmartSocketServer server = new SmartSocketServer(Name, new SmartSocketTypeResolver(typeof(ServerMessage), typeof(ClientMessage)));
            server.ClientConnected += OnClientConnected;
            server.ClientDisconnected += OnClientDisconnected;
            server.StartListening();

            System.Diagnostics.Process client = System.Diagnostics.Process.Start(@"E:\git\lovettsoftware\Tools\SmartSockets\TestClient\bin\Debug\net46\TestClient.exe");

            Console.WriteLine("Press any key to terminate...");
            Console.ReadLine();
        }

        private void OnClientDisconnected(object sender, SmartSocket e)
        {
            Console.WriteLine("Client '{0}' has gone byebye...", e.Name);
        }

        private void OnClientConnected(object sender, SmartSocket e)
        {
            e.Error += OnClientError;
            Console.WriteLine("Client '{0}' is connected", e.Name);
            Task.Run(() => HandleClientAsync(e));
        }

        private void OnClientError(object sender, Exception e)
        {
            var saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(e.Message);
            Console.ForegroundColor = saved;
        }

        private async void HandleClientAsync(SmartSocket client)
        {
            while (client.IsConnected)
            {
                ClientMessage e = await client.ReceiveAsync() as ClientMessage;
                if (e != null)
                {
                    if (e.Id == "test")
                    {
                        _ = client.SendResponseAsync(new ServerMessage("test", Name, DateTime.Now));
                    }
                    else
                    {
                        Console.WriteLine("Received message '{0}' from '{1}' at '{2}'", e.Id, e.Sender, e.Timestamp);
                        _ = client.SendResponseAsync(new ServerMessage("Server says hi!", Name, DateTime.Now));
                    }
                }
            }
        }
    }
}
