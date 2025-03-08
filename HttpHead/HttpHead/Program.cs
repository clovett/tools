using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HttpHead
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("httphead url");
                    return 1;
                }
                HttpClient client = new HttpClient();
                var resp = await client.GetAsync(args[0]);

                Console.WriteLine("Response: " + resp.StatusCode);
                foreach (var keyValuePair in resp.Headers)
                {
                    var values = string.Join(",", keyValuePair.Value);
                    Console.WriteLine(keyValuePair.Key + "=" + values);
                }
                foreach (var keyValuePair in resp.Content.Headers)
                {
                    var values = string.Join(",", keyValuePair.Value);
                    Console.WriteLine(keyValuePair.Key + "=" + values);
                }
                
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("### Error: " + ex.Message);
                Console.ResetColor();
                return 1;
            }
            return 0;
        }
    }
}
