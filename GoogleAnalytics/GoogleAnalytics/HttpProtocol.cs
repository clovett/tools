using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

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

            var settings = new DataContractJsonSerializerSettings();
            settings.EmitTypeInformation = EmitTypeInformation.Never;
            settings.UseSimpleDictionaryFormat = true;

            DataContractJsonSerializer s = new DataContractJsonSerializer(typeof(Analytics), settings);
            MemoryStream ms = new MemoryStream();
            s.WriteObject(ms, a);
            var  bytes = ms.Position;
            if (bytes > 130000)
            {
                throw new Exception("The total size of analytics payloads cannot be greater than 130kb bytes" + guide);
            }
            ms.Seek(0, SeekOrigin.Begin);
            string json = System.Text.Encoding.UTF8.GetString(ms.GetBuffer());

            var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, jsonContent);
            response.EnsureSuccessStatusCode();
       }
    }
}
