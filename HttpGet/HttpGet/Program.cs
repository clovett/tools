using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HttpGet
{
    class Program
    {
        bool all;
        bool headers;
        string filename;
        string url;
        string rootDir;

        static void Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return;
            }

            try
            {
                p.Run();
            }
            catch (WebException e)
            {
                Console.WriteLine("### Error: {0}", e.Message);
                if (e.Response.ContentLength > 0)
                {
                    using (Stream s = e.Response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(s))
                        {
                            Console.WriteLine(reader.ReadToEnd());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("### Error: {0} {1}", e.GetType().FullName, e.Message);
            }
}

        private bool ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    string option = arg.Substring(1);
                    switch (option)
                    {
                        case "a":
                        case "all":
                            all = true;
                            break;
                        case "?":
                        case "h":
                        case "help":
                            return false;
                        case "filename":
                            if (i + 1 < args.Length)
                            {
                                this.filename = Path.GetFullPath(args[++i]);
                            }
                            else
                            {
                                Console.WriteLine("### missing file name argument");
                                return false;
                            }
                            break;
                        case "headers":
                            this.headers = true;
                            break;
                    }
                }
                else if (url == null)
                {
                    url = arg;
                }
                else
                {
                    Console.WriteLine("### Too many arguments");
                    return false;
                }
            }
            if (url == null)
            {
                Console.WriteLine("### Missing url argument");
                return false;
            }

            return true;
        }

        private void Run()
        {
            Uri baseUri = new Uri(url);
            string path = Download(baseUri, null);
            if (path == null)
            {
                return;
            }
            string fullPath = Path.GetFullPath(path);
            if (all)
            {
                XDocument doc = XDocument.Load(path);
                FetchCss(baseUri, doc);
                FetchAudio(baseUri, doc);
                FetchImages(baseUri, doc);
                FetchSvgImages(baseUri, doc);
                FetchScripts(baseUri, doc);

                doc.Save(fullPath);
            }
        }

        private void FetchCss(Uri baseUri, XDocument doc)
        {
            XNamespace ns = doc.Root.Name.Namespace;
            foreach (XElement link in doc.Descendants(ns + "link"))
            {
                string href = (string)link.Attribute("href");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    string local = Download(baseUri, href);
                    if (local != null && local != href)
                    {
                        link.SetAttributeValue("href", local);
                    }
                }
            }
        }

        private void FetchAudio(Uri baseUri, XDocument doc)
        {
            XNamespace ns = doc.Root.Name.Namespace;
            foreach (XElement link in doc.Descendants(ns + "audio"))
            {
                string href = (string)link.Attribute("src");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    string local = Download(baseUri, href);
                    if (local != null && local != href)
                    {
                        link.SetAttributeValue("src", local);
                    }
                }
            }
        }

        private void FetchImages(Uri baseUri, XDocument doc)
        {
            XNamespace ns = doc.Root.Name.Namespace;
            foreach (XElement link in doc.Descendants(ns + "img"))
            {
                string href = (string)link.Attribute("src");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    string local = Download(baseUri, href);
                    if (local != null && local != href)
                    {
                        link.SetAttributeValue("src", local);
                    }
                }
            }
        }

        private void FetchSvgImages(Uri baseUri, XDocument doc)
        {
            XNamespace svgns = XNamespace.Get("http://www.w3.org/2000/svg");
            XNamespace xlinkns = XNamespace.Get("http://www.w3.org/1999/xlink");
            foreach (XElement link in doc.Descendants(svgns + "image"))
            {
                string href = (string)link.Attribute(xlinkns + "href");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    string local = Download(baseUri, href);
                    if (local != null && local != href)
                    {
                        link.SetAttributeValue(xlinkns + "href", local);
                    }
                }
            }
        }
        private void FetchScripts(Uri baseUri, XDocument doc)
        {
            XNamespace ns = doc.Root.Name.Namespace;
            foreach (XElement link in doc.Descendants(ns + "script"))
            {
                string href = (string)link.Attribute("src");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    string local = Download(baseUri, href);
                    if (local != null && local != href)
                    {
                        link.SetAttributeValue("src", local);
                    }
                }
            }
        }

        private string Download(Uri uri, string relative)
        {
            string result = null;

            if (!string.IsNullOrWhiteSpace(relative))
            {
                if (relative.StartsWith("#") || relative.StartsWith("data:"))
                {
                    // content is inline
                    return null;
                }

                Uri resolved = new Uri(uri, relative);
                Uri rel = resolved.MakeRelativeUri(uri);
                if (rel.IsAbsoluteUri)
                {
                    // don't download from different places
                    return null;
                }
                string subs = "";
                string[] dirs = relative.Split('/', '\\');
                for (int i = 0; i < dirs.Length; i++)
                {
                    string dir = dirs[i];
                    if (dir == ".." || string.IsNullOrWhiteSpace(dir))
                    {
                        // ignore this
                    }
                    else if (i == dirs.Length - 1)
                    {
                        // the file
                        string fname = dir;
                        result = Path.Combine(subs, fname);
                        uri = resolved;
                        subs = "";
                    }
                    else
                    {
                        // a directory
                        subs = Path.Combine(subs, dir);
                        string newPath = Path.Combine(this.rootDir, subs);
                        if (!Directory.Exists(newPath))
                        {
                            Directory.CreateDirectory(newPath);
                        }
                        Directory.SetCurrentDirectory(newPath);
                    }
                }
            }
                    

            Console.WriteLine("Fetching: " + uri.AbsoluteUri);
            WebRequest req = WebRequest.Create(uri);
            req.Credentials = CredentialCache.DefaultNetworkCredentials;            
            req.Method = "GET";

            WebResponse resp = req.GetResponse();

            if (headers)
            {
                foreach (string key in resp.Headers.AllKeys)
                {
                    Console.WriteLine(key + "=" + resp.Headers[key]);
                }
                return null;
            }

            using (var stream = resp.GetResponseStream())
            {
                if (string.IsNullOrEmpty(relative))
                {
                    if (this.filename == null)
                    {
                        CopyToFile(stream, this.filename);
                        if (result == null)
                        {
                            result = this.filename;
                        }
                    }
                    else
                    {
                        string fname = uri.Segments[uri.Segments.Length - 1];
                        CopyToFile(stream, fname);
                        if (result == null)
                        {
                            result = fname;
                        }
                    }
                }
            }
            if (rootDir == null)
            {
                rootDir = Path.GetDirectoryName(Path.GetFullPath(result));
            }
            else
            {
                Directory.SetCurrentDirectory(rootDir);
            }
            return result;
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
            Console.WriteLine("HttpGet [options] <url>");
            Console.WriteLine("Fetches the resource at the given URL and saves it locally");
            Console.WriteLine("Options:");
            Console.WriteLine("   -all      if url is xhtml it brings down all locally referenced resources with the file");
            Console.WriteLine("   -filename the name of the file to save http content into (default writes to stdout)");
            Console.WriteLine("   -headers  just print http headers to stdout");
        }
    }
}
