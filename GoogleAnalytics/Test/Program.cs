using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using GoogleAnalytics;

namespace Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage Test <Google Analytics Measurement Id> <Api Secret>");
                return;
            }

            string clientId = Guid.NewGuid().ToString();
            string trackingId = args[0];
            string apiSecret = args[1];

            var analytics = new Analytics()
            {
                MeasurementId = trackingId,
                ApiSecret = apiSecret,
                ClientId = clientId
            };
            var m = new PageMeasurement()
            {
                Path = "https://microsoft.github.io/XmlNotepad/App/FormSearch",
                Title = "FormOptions"
            };

            // m.Params["debug_mode"] = "1";

            analytics.Events.Add(m);

            await GoogleAnalytics.HttpProtocol.PostMeasurements(analytics);

            Console.WriteLine("measurement sent!!");
        }
    }
}
