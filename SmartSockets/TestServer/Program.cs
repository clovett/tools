using System;
using Microsoft.Networking.SmartSockets;
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

            Console.WriteLine("Press any key to terminate...");
            Console.ReadLine();
        }

        private void OnClientDisconnected(object sender, SmartSocket e)
        {
            Console.WriteLine("Client '{0}' has gone byebye...", e.Name);
        }

        private void OnClientConnected(object sender, SmartSocket e)
        {
            Console.WriteLine("Client '{0}' is connected", e.Name);
            Task.Run(() => HandleClientAsync(e));
        }

        private async void HandleClientAsync(SmartSocket client)
        {
            while (client.IsConnected)
            {
                Message m = await client.ReceiveAsync();
                if (m is ClientMessage)
                {
                    ClientMessage e = (ClientMessage)m;
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
