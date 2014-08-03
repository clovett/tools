using Sgml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace UrlScraper
{
    class Program
    {
        string select;
        Uri baseUrl;

        static void Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return;
            }
            p.Run();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: UrlScraper [options] url");
            Console.WriteLine("Finds href references in the given page and prints them out");
            Console.WriteLine("Options:");
            Console.WriteLine("    /select xpath       - limits search to matching section of the page");
        }

        private bool ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "?":
                        case "h":
                        case "help":
                            return false;
                        case "s":
                        case "select":
                            if (i + 1 < args.Length)
                            {
                                select = args[++i];
                            }
                            break;
                    }
                }
                else if (baseUrl == null)
                {
                    if (!Uri.TryCreate(arg, UriKind.Absolute, out baseUrl))
                    {
                        Console.WriteLine("### error: invalid url: " + arg);
                    }
                }
                else
                {
                    Console.WriteLine("### error: too many arguments");
                    return false;
                }
            }
            return true;
        }

        private void Run()
        {
            Scrape(this.baseUrl);
        }


        private void Scrape(Uri url)
        {
            XmlDocument doc = LoadHtmlPage(url);
            XmlElement e = doc.DocumentElement;
            if (select != null)
            {
                try
                {
                    e = doc.SelectSingleNode(select) as XmlElement;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("### error with selection: " + ex.Message);
                    return;
                }
                if (e == null)
                {
                    Console.WriteLine("### nothing selected");
                }
            }

            foreach (XmlElement d in e.SelectNodes(".//*"))
            {
                string href = d.GetAttribute("href");
                if (!string.IsNullOrEmpty(href))
                {
                    Console.WriteLine(d.InnerText); 
                }
            }
        }

        private XmlDocument LoadHtmlPage(Uri url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string encoding = response.ContentEncoding;
                    using (var stream = response.GetResponseStream())
                    {
                        return ParseHtmlAsXml(stream, encoding);
                    }
                }
                else
                {
                    Console.WriteLine("### error: " + response.StatusDescription);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("### error: " + ex.Message);
            }
            return null;
        }

        private XmlDocument ParseHtmlAsXml(System.IO.Stream stream, string encoding)
        {
            Encoding e = Encoding.UTF8;
            try
            {
                e = Encoding.GetEncoding(encoding);
            }
            catch
            {

            }
            try
            {
                SgmlReader sgml = new SgmlReader();
                sgml.DocType = "html";
                sgml.InputStream = new StreamReader(stream, e);

                XmlDocument doc = new XmlDocument();
                doc.Load(sgml);
                return doc;
            }
            catch (Exception ex)
            {
                Console.WriteLine("### parse error: " + ex.Message);
            }
            return null;
        }
    }
}
