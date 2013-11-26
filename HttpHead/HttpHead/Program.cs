using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HttpHead
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(args[0]);
                req.Method = "HEAD";

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                Console.WriteLine("Response: " + resp.StatusCode);
                foreach (string key in resp.Headers.Keys)
                {
                    Console.WriteLine(key + "=" + resp.Headers[key]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("### Error: " + ex.Message);
            }
        }
    }
}
