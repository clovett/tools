using Sgml;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace HtmlScraper
{
    class Program
    {
        string scope;
        string select;
        bool inner;
        Uri baseUrl;
        string userid;
        string password;
        WebClient client;

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
            Console.WriteLine("Usage: HtmlScraper [options] url ");
            Console.WriteLine("Finds elements matching given xpath and prints them");
            Console.WriteLine("Arguments:");
            Console.WriteLine("    url              a file or http:// address of the HTML page");
            Console.WriteLine("    /select xpath    an xpath expression for selecting things in the page, for example: //a/@href");
            Console.WriteLine("    [/scope  xpath]  an xpath expression for selecting the scope of the search, example: /html/body limits search to the body tag.");
            Console.WriteLine("    /inner           print the inner text of the matching nodes");
            Console.WriteLine("    [/userid userid]         credentials for https login if required");
            Console.WriteLine("    [/password password]     credentials for https login if required");
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
                        case "select":
                            if (i + 1 < args.Length)
                            {
                                select = args[++i];
                            }
                            continue;
                        case "scope":
                            if (i + 1 < args.Length)
                            {
                                scope = args[++i];
                            }
                            continue;
                        case "inner":
                            inner = true;
                            continue;
                        case "userid":
                            if (i + 1 < args.Length)
                            {
                                userid = args[++i];
                            }
                            continue;
                        case "password":
                            if (i + 1 < args.Length)
                            {
                                password = args[++i];
                            }
                            continue;                             
                    }
                }
                
                if (baseUrl == null)
                {
                    Uri currentDir = new Uri(Directory.GetCurrentDirectory() + "/");
                    if (!Uri.TryCreate(currentDir, arg, out baseUrl))
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

            if (baseUrl == null || select == null)
            {
                Console.WriteLine("### error: missing arguments");
                return false;
            }
            return true;
        }

        private void Run()
        {
            try
            {
                Scrape(this.baseUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine("### error: " + ex.Message);
            }
        }

        XmlNodeList Select(XmlNode scope, string xpath)
        {
            XmlDocument doc = scope.OwnerDocument;
            if (doc == null)
            {
                doc = scope as XmlDocument;
            }
            int letterA = Convert.ToInt32('a');
            int prefix = 0;
            XmlNamespaceManager mgr = new XmlNamespaceManager(doc.NameTable);
            StringBuilder sb = new StringBuilder();
            for (int i = 0, n = xpath.Length; i < n; i++ )
            {
                char c = xpath[i];
                if (c == '{')
                {
                    i++;
                    int j = xpath.IndexOf('}', i);
                    if (j > i)
                    {
                        if (prefix == 26)
                        {
                            throw new Exception("too many namespaces in your xpath, we only support 26");                            
                        }
                        string nsUri = xpath.Substring(i, j - i);
                        char nsPrefix = Convert.ToChar(letterA + prefix++);
                        sb.Append(nsPrefix);
                        sb.Append(':');
                        mgr.AddNamespace(nsPrefix.ToString(), nsUri);
                        i = j;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            string prefixedXPath = sb.ToString();
            return scope.SelectNodes(prefixedXPath, mgr);
        }


        private void Scrape(Uri url)
        {
            XmlDocument doc = LoadHtmlPage(url);

            List<XmlNode> scopes = new List<XmlNode>();
            if (scope != null)
            {
                try
                {
                    foreach (XmlNode e in Select(doc, scope))
                    {
                        scopes.Add(e);
                    }
                    if (scopes.Count == 0)
                    {
                        Console.WriteLine("no elements matching scope: " + scope);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("### error with selection: " + ex.Message);
                    return;
                }
            }
            else
            {
                scopes.Add(doc.DocumentElement);
            }

            int count = 0;

            foreach (XmlNode root in scopes)
            {
                foreach (XmlNode d in Select(root, select))
                {
                    count++;
                    if (inner)
                    {
                        Console.WriteLine(d.InnerText);
                    }
                    else
                    {
                        Console.WriteLine(d.OuterXml);
                    }
                }
            }
            if (count == 0)
            {
                Console.WriteLine("no elements matching: " + select);
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("found {0} matches", count);
            }
        }

        private XmlDocument LoadHtmlPage(Uri url)
        {
            try
            {
                if (url.Scheme == "file")
                {
                    using (var stream = new FileStream(url.LocalPath, FileMode.Open, FileAccess.Read))
                    {
                        return ParseHtmlAsXml(stream, "UTF-8");
                    }
                }
                else
                {
                    this.client = new WebClient();
                    byte[] data = this.client.DownloadData(url);
                    XmlDocument doc = ParseHtmlAsXml(new MemoryStream(data), "utf-8");

                    string title = null;
                    foreach (XmlNode node in Select(doc, "//title"))
                    {
                        title = node.InnerText;
                    }

                    if (title.Contains("Login"))
                    {
                        doc = Login(url, doc);
                    }

                    //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    //request.Method = "GET";
                    //request.UseDefaultCredentials = true;
                    //HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    //if (response.StatusCode == HttpStatusCode.OK)
                    //{
                    //    string encoding = response.ContentEncoding;
                    //    using (var stream = response.GetResponseStream())
                    //    {
                    //        return ParseHtmlAsXml(stream, encoding);
                    //    }
                    //}
                    //else
                    //{
                    //    Console.WriteLine("### error: " + response.StatusDescription);
                    //}
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("### error: " + ex.Message);
            }
            return null;
        }

        private XmlDocument Login(Uri baseUrl, XmlDocument loginForm)
        {
            XmlElement form = Select(loginForm, "//form").FirstOrDefault() as XmlElement;
            if (form == null)
            {
                throw new Exception("Login failed to find html form");                
            }

            string action = form.GetAttribute("action");
            if (action == null)
            {
                throw new Exception("Login failed to find html form 'action' attribute");   
            }

            Uri loginUri = new Uri(baseUrl, action);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(loginUri);
            request.Method = "POST";
            request.KeepAlive = true;
            request.Credentials = new NetworkCredential(this.userid, this.password);            

            StringBuilder sb = new StringBuilder();
            foreach (XmlElement node in Select(form, ".//input"))
            {
                string id = node.GetAttribute("id");
                if (!string.IsNullOrEmpty(id))
                {
                    if (sb.Length > 0)
                    {
                        sb.Append("&");
                    }

                    string value = node.GetAttribute("value");
                    string type = node.GetAttribute("type");

                    if (string.IsNullOrEmpty(value) && type != "hidden")
                    {
                        string lid = id.ToLowerInvariant();
                        if (lid.Contains("user") || lid.Contains("email"))
                        {
                            value = this.userid;
                        }
                        else if (lid.Contains("password"))
                        {
                            value = this.password;
                        }
                    }
                    if (value == null) value = "";
                    sb.Append(id);
                    sb.Append("=");
                    sb.Append(value);
                }
            }

            request.UserAgent = "Agent=Mozilla/5.0 (Windows NT 6.3; Win64; x64; Trident/7.0; rv:11.0)";
            using (Stream stream = request.GetRequestStream())
            {
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    writer.WriteLine(sb.ToString());
                }
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            
            return ParseHtmlAsXml(response.GetResponseStream(), response.ContentEncoding);
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
