using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GoogleAnalytics
{
    public class HttpProtocol
    {
        public static async Task PostMeasurements(Analytics a)
        {
            const string guide = "\r\nSee https://developers.google.com/analytics/devguides/collection/protocol/ga4";

            if (a.Events.Count > 25)
            {
                throw new Exception("A maximum of 25 events can be specified per request." + guide);
            }

            string baseUrl = "https://www.google-analytics.com/mp/collect";
            string query = a.ToQueryString();
            string url = baseUrl + "?" + query;

            HttpClient client = new HttpClient();

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(a);

            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            if (bytes.Length > 130000)
            {
                throw new Exception("The total size of analytics payloads cannot be greater than 130kb bytes" + guide);
            }

            var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, jsonContent);
            response.EnsureSuccessStatusCode();
       }
    }
}
