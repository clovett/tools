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
                Console.WriteLine("Usage Test GoogleAnalyticsTrackingId");
            }

            string trackingId = args[0];

            await GoogleAnalytics.HttpProtocol.PostMeasurements(new PageMeasurement()
            {
                TrackingId = trackingId,
                ClientId = "123",
                HostName = "microsoft.github.io",
                Path = "/XmlNotepad/help/clipboard",
                Title = "Schemas"
            });

            Console.WriteLine("measurement sent!!");
        }
    }
}
