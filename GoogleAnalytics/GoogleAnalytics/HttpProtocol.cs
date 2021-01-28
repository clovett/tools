using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GoogleAnalytics
{
    public class HttpProtocol
    {
        public static async Task PostMeasurements(params Measurement[] args)
        {
            const string guide = "\r\nSee: https://developers.google.com/analytics/devguides/collection/protocol/v1/devguide";

            if (args.Length > 20)
            {
                throw new Exception("A maximum of 20 hits can be specified per request." + guide);
            }

            StringBuilder sb = new StringBuilder();
            foreach(var m in args)
            {
                var line = m.ToString();
                if (Encoding.UTF8.GetByteCount(line) > 8192)
                {
                    throw new Exception("No single hit payload can be greater than 8K bytes" + guide);
                }
                sb.Append(line);
                sb.AppendLine();
            }

            string msg = sb.ToString();
            if (Encoding.UTF8.GetByteCount(msg) > 16384)
            {
                throw new Exception("The total size of all hit payloads cannot be greater than 16K bytes" + guide);
            }

            string baseUrl = "http://www.google-analytics.com/";
            string verb = args.Length > 1 ? "batch" : "collect";

            HttpClient client = new HttpClient();
            var response = await client.PostAsync(baseUrl + verb, new StringContent(msg, Encoding.UTF8));
            response.EnsureSuccessStatusCode();
       }
    }
}
