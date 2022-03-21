using System;
using System.Threading.Tasks;
using GoogleAnalytics;

namespace Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage Test Google Analytics Tracking Id");
                return;
            }

            string clientId = Guid.NewGuid().ToString(); 
            string trackingId = args[0];

            await GoogleAnalytics.HttpProtocol.PostMeasurements(new PageMeasurement()
            {
                TrackingId = trackingId,
                ClientId = clientId,
                HostName = "microsoft.github.io",
                Path = "/App/Launch",
                Title = "Launch"
            });

            Console.WriteLine("measurement sent!!");
        }
    }
}
