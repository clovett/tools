using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HttpGet
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            try
            {
                FetchUrl(args[0]);
            }
            catch (Exception e)
            {
                Console.WriteLine("### Error: " + e.Message);
            }
        }

        private static void FetchUrl(string url)
        {
            Uri uri = new Uri(url);
            WebRequest req = WebRequest.Create(uri);
            req.Credentials = CredentialCache.DefaultCredentials;
            req.Method = "GET";

            WebResponse resp = req.GetResponse();
            using (var stream = resp.GetResponseStream())
            {
                CopyToFile(stream, uri.Segments[uri.Segments.Length - 1]);
            }
        }

        private static void CopyToFile(Stream stream, string path)
        {
            byte[] buffer = new byte[64000];
            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                int len = stream.Read(buffer, 0, buffer.Length);
                while (len > 0) 
                {
                    file.Write(buffer, 0, len);
                    len = stream.Read(buffer, 0, buffer.Length);
                }
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("HttpGet <url>");
            Console.WriteLine("Fetches the resource at the given URL and saves it locally");
        }
    }
}
