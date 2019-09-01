using System;
using System.Threading;
using System.Threading.Tasks;
using LovettSoftware.Networking.SmartSockets;
using ConsoleInterface;
using System.Diagnostics;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Client!");

            Program p = new Program();
            p.RunTest().Wait();
        }

        string name = "client1";

        private async Task RunTest()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            using (SmartSocketClient client = await SmartSocketClient.FindServerAsync("CoyoteTester", name, new SmartSocketTypeResolver(typeof(ServerMessage), typeof(ClientMessage)), source.Token))
            {
                client.Error += OnClientError;
                client.ServerName = "CoyoteTester";
                for (int i = 0; i < 10; i++)
                {
                    SocketMessage response = await client.SendAsync(new ClientMessage("Howdy partner " + i, this.name, DateTime.Now));
                    ServerMessage e = (ServerMessage)response;
                    Console.WriteLine("Client Received message '{0}' from '{1}' at '{2}'", e.Id, e.Sender, e.Timestamp);
                }

                Stopwatch watch = new Stopwatch();
                watch.Start();
                for (int i = 0; i < 1000; i++)
                {
                    SocketMessage response = await client.SendAsync(new ClientMessage("test", this.name, DateTime.Now));
                    ServerMessage e = (ServerMessage)response;
                }
                watch.Stop();

                Console.WriteLine("Sent 1000 messages in {0} milliseconds", watch.ElapsedMilliseconds);
            }

            await Task.Delay(5000);
        }

        private void OnClientError(object sender, Exception e)
        {
            var saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(e.Message);
            Console.ForegroundColor = saved;
        }
    }
}
