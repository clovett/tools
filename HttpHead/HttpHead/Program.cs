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
        static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("httphead url");
                    return 1;
                }
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
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("### Error: " + ex.Message);
                Console.ResetColor();
                return 1;
            }
            return 0;
        }
    }
}
