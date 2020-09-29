using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace HttpGet
{
    class Program
    {
        bool all;
        bool deep;
        bool headers;
        string filename;
        string root;
        Uri baseUrl;
        string rootDir;
        bool stats;
        int depth;
        int errors;
        List<Uri> merge = new List<Uri>();
        Dictionary<Uri, string> fetched = new Dictionary<Uri, string>();

        static int Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return 1;
            }

            try
            {
                p.Run();
            }
            catch (WebException e)
            {
                p.WriteError("### Error: {0}", e.Message);
            }
            catch (Exception e)
            {
                p.WriteError("### Error: {0} {1}", e.GetType().FullName, e.Message);
            }

            if (p.errors > 0)
            {
                p.WriteError("### Found {0} errors", p.errors);
                return 1;
            }
            else
            {
                Console.WriteLine("### success");
            }

            return 0;
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
                        case "d":
                        case "deep":
                            deep = true;
                            break;
                        case "m":
                        case "merge":
                            if (i + 1 < args.Length)
                            {
                                merge.Add(new Uri(args[++i]));
                            }
                            break;
                        case "root":
                            if (i + 1 < args.Length)
                            {
                                root = args[++i];
                            }
                            break;
                        case "s":
                        case "stats":
                            stats = true;
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
                                WriteError("### missing file name argument");
                                return false;
                            }
                            break;
                        case "headers":
                            this.headers = true;
                            break;
                    }
                }
                else if (baseUrl == null)
                {
                    baseUrl = new Uri(arg);
                }
                else
                {
                    WriteError("### Too many arguments");
                    return false;
                }
            }
            if (baseUrl == null)
            {
                WriteError("### Missing url argument");
                return false;
            }

            return true;
        }

        private void Run()
        {
            Process(this.baseUrl, new HashSet<Uri>());
        }

        Stack<Uri> stack = new Stack<Uri>();

        private string Process(Uri uri, HashSet<Uri> pending)
        {
            if (fetched.ContainsKey(uri))
            {
                // already done!
                return fetched[uri];
            }

            string path = Download(uri);
            if (string.IsNullOrEmpty(path))
            {
                return "";
            }
            if (all || deep)
            {
                string fullPath = Path.GetFullPath(path);
                string ext = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
                if (ext == "htm" || ext == "html" || ext == "")
                {
                    XDocument doc = null;
                    using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            // setup SgmlReader
                            Sgml.SgmlReader sgmlReader = new Sgml.SgmlReader();
                            sgmlReader.DocType = "HTML";
                            sgmlReader.WhitespaceHandling = WhitespaceHandling.All;
                            sgmlReader.CaseFolding = Sgml.CaseFolding.ToLower;
                            sgmlReader.InputStream = reader;

                            try
                            {
                                doc = XDocument.Load(sgmlReader);
                                FetchCss(uri, doc);
                                FetchAudio(uri, doc);
                                FetchImages(uri, doc);
                                FetchSvgImages(uri, doc);
                                FetchScripts(uri, doc);
                                FetchZipFiles(uri, doc);
                            }
                            catch (Exception)
                            {
                                WriteError("### Error processing HTML file: " + fullPath);
                                return path;
                            }
                        }
                    }

                    if (deep)
                    {
                        stack.Push(uri);
                        TraverseLinks(uri, doc, pending);
                        stack.Pop();
                    }

                    // save valid XML version
                    try
                    {
                        doc.Save(fullPath);
                    }
                    catch (Exception ex)
                    {
                        WriteError("### Error saving XML file: " + ex.Message);
                    }

                }
            }
            return path;
        }

        private void TraverseLinks(Uri baseUri, XDocument doc, HashSet<Uri> pending)
        {
            // ok, now check all <a> links in this page and if they are local download them, if they are remote
            // check that they are valid, but don't traverse them.
            depth++;
            XNamespace ns = doc.Root.Name.Namespace;
            List<Tuple<XElement, Uri>> local = new List<Tuple<XElement, Uri>>();
            foreach (XElement link in doc.Descendants(ns + "a"))
            {
                string href = (string)link.Attribute("href");
                if (!string.IsNullOrWhiteSpace(href) && !href.StartsWith("#"))
                {
                    Uri resolved = new Uri(baseUri, href);
                    if (resolved != baseUri && !pending.Contains(resolved))
                    {
                        pending.Add(resolved);
                        local.Add(new Tuple<XElement, Uri>(link, resolved));
                    }
                    else if (fetched.ContainsKey(resolved))
                    {
                        var localFile = fetched[resolved];
                        link.SetAttributeValue("href", MakeRelative(baseUri, resolved, localFile));
                    }
                    else
                    {
                        var localFile = ComputeLocalPath(resolved);
                        if (!string.IsNullOrWhiteSpace(localFile))
                        {
                            link.SetAttributeValue("href", MakeRelative(baseUri, resolved, localFile));
                        }
                    }
                }
            }

            foreach (var pair in local)
            {
                var link = pair.Item1;
                var uri = pair.Item2;
                string localPath = Process(uri, pending);
                if (!string.IsNullOrEmpty(localPath))
                {
                    link.SetAttributeValue("href", MakeRelative(baseUri, uri, localPath));
                }
            }
            depth--;
        }

        private void FetchCss(Uri baseUri, XDocument doc)
        {
            XNamespace ns = doc.Root.Name.Namespace;
            foreach (XElement link in doc.Descendants(ns + "link"))
            {
                string href = (string)link.Attribute("href");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    try
                    {
                        Uri resolved = new Uri(baseUri, href);
                        string local = Download(resolved);
                        if (!string.IsNullOrEmpty(local))
                        {
                            link.SetAttributeValue("href", MakeRelative(baseUri, resolved, local));
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError("### Error: " + ex.Message);
                        WriteError("### Error: broken link: " + href);
                        WriteError("### Error: referenced : " + baseUri.ToString());
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
                    Uri resolved = new Uri(baseUri, href);
                    string local = Download(resolved);
                    if (local != null && local != href)
                    {
                        link.SetAttributeValue("src", MakeRelative(baseUri, resolved, local));
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
                    Uri resolved = new Uri(baseUri, href);
                    string local = Download(resolved);
                    if (local != null && local != href)
                    {
                        link.SetAttributeValue("src", MakeRelative(baseUri, resolved, local));
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
                    Uri resolved = new Uri(baseUri, href);
                    string local = Download(resolved);
                    if (local != null && local != href)
                    {
                        link.SetAttributeValue(xlinkns + "href", MakeRelative(baseUri, resolved, local));
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
                    Uri resolved = new Uri(baseUri, href);
                    string local = Download(resolved);
                    if (local != null && local != href)
                    {
                        link.SetAttributeValue("src", MakeRelative(baseUri, resolved, local));
                    }
                }
            }
        }


        private void FetchZipFiles(Uri baseUri, XDocument doc)
        {
            XNamespace ns = doc.Root.Name.Namespace;
            foreach (XElement link in doc.Descendants(ns + "a"))
            {
                string href = (string)link.Attribute("href");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    Uri resolved = new Uri(baseUri, href);
                    string ext = System.IO.Path.GetExtension(resolved.LocalPath);
                    if (string.Compare(ext, ".zip", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        string local = Download(resolved);
                        if (local != null && local != href)
                        {
                            link.SetAttributeValue("href", MakeRelative(baseUri, resolved, local));
                        }
                    }
                }
            }
        }

        private string ComputeLocalPath(Uri uri)
        {
            bool external = IsExternal(uri);
            if (external)
            {
                return null;
            }

            Uri simpleUri = new Uri(uri.Scheme + "://" + uri.Host + uri.AbsolutePath);
            Uri rel = this.baseUrl.MakeRelativeUri(simpleUri);
            string relative = rel.ToString();
            string result = "";

            if (string.IsNullOrEmpty(relative))
            {
                result = "index.html";
            }
            else
            {
                string[] dirs = relative.Split('/', '\\');
                for (int i = 0; i < dirs.Length; i++)
                {
                    string dir = dirs[i];
                    if (dir == "..")
                    {
                        dir = "parent";
                    }

                    if (i == dirs.Length - 1)
                    {
                        // the file
                        string fname = string.IsNullOrEmpty(dir) ? "index.html" : dir;
                        if (string.IsNullOrEmpty(Path.GetExtension(fname)))
                        {
                            // default to html file types.
                            fname += ".htm";
                        }
                        result = Path.Combine(result, fname);
                    }
                    else
                    {
                        // a directory
                        result = Path.Combine(result, dir);
                        if (!Directory.Exists(result))
                        {
                            Directory.CreateDirectory(result);
                        }
                    }
                }
            }
            return result;
        }

        private bool IsExternal(Uri uri)
        {
            if (uri.Host != this.baseUrl.Host)
            {
                return true;
            }

            bool result = uri.AbsolutePath.StartsWith(this.baseUrl.AbsolutePath);
            if (!result)
            {
                return true;
            }

            return false;
        }

        private string Download(Uri uri)
        {
            string result = "";

            if (fetched.ContainsKey(uri))
            {
                // already taken care of
                return fetched[uri];
            }

            bool external = IsExternal(uri);
            Uri original = uri;

            Uri baseuri = this.baseUrl;

            if (external)
            {
                foreach (var other in this.merge)
                {
                    if (uri.Host == other.Host && uri.Segments.Length > 1)
                    {
                        external = false;
                        baseuri = other;
                        break;
                    }
                }
            }


            try
            {
                if (uri.Scheme != "http" && uri.Scheme != "https")
                {
                    // mark it as done even if request fails so we don't keep retrying.
                    fetched[uri] = "";
                    return null;
                }

                string verb = external ? "Checking" : "Fetching";
                string indent = new string(' ', depth);
                WriteNormal(depth + ": " + indent + verb + ": " + uri.AbsoluteUri);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36 Edge/15.15063";
                req.Credentials = CredentialCache.DefaultNetworkCredentials;
                req.Method = external ? "HEAD" : "GET";

                using (WebResponse resp = req.GetResponse())
                {
                    uri = resp.ResponseUri;
                    external = IsExternal(uri); // may have been redirected somewhere else!
                    if (!external)
                    {
                        result = ComputeLocalPath(uri);
                    }

                    // in case it is an HTTP redirect.
                    fetched[uri] = result;
                    fetched[original] = result;

                    if (headers)
                    {
                        foreach (string key in resp.Headers.AllKeys)
                        {
                            WriteNormal(key + "=" + resp.Headers[key]);
                        }
                        return null;
                    }

                    if (external)
                    {
                        // stop here, don't traverse into external links, just check they are ok.
                        return null;
                    }
                    using (var stream = resp.GetResponseStream())
                    {
                        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                        watch.Start();
                        long length = 0;
                        try
                        {
                            length = CopyToFile(stream, result);
                        }
                        catch (Exception ex)
                        {
                            WriteError("### error writing new file {0}: {1}", result, ex.Message);
                        }
                        watch.Stop();
                        if (length > 0)
                        {
                            if (stats)
                            {
                                double bps = (double)length / watch.Elapsed.TotalSeconds;
                                WriteInfo("Download speed: {0} bytes per second", Math.Round(bps, 3));
                            }
                            if (stats)
                            {
                                WriteInfo("Saved local file: " + Path.GetFullPath(result));
                            }
                        }
                    }
                }
                if (rootDir == null)
                {
                    rootDir = Path.GetDirectoryName(Path.GetFullPath(result));
                }
            }
            catch (Exception ex)
            {
                WriteError("### Error downloading URL: " + ex.Message);
                fetched[uri] = "";
                return null;
            }
            return result;
        }

        private string MakeRelative(Uri baseUri, Uri resolved, string localFile)
        {
            if (string.IsNullOrEmpty(localFile))
            {
                return ""; // this file was not downloaded.
            }

            string filename = System.IO.Path.GetFileName(localFile);
            if (resolved.Segments[resolved.Segments.Length - 1].EndsWith("/"))
            {
                resolved = new Uri(resolved, filename);
            }
            else if (resolved.Segments[resolved.Segments.Length - 1] != filename)
            {
                resolved = new Uri(resolved, filename);
            }
            Uri relative = baseUri.MakeRelativeUri(resolved);
            return relative.ToString();
        }

        private void WriteError(string format, params object[] args)
        {
            string msg = format;
            if (args != null)
            {
                msg = string.Format(format, args);
            }
            var saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);

            var s = this.stack.ToArray();
            foreach (var item in s.Reverse())
            {
                Console.WriteLine("Included by: {0}", item.ToString());
            }

            Console.ForegroundColor = saved;

            this.errors++;
        }

        private static void WriteInfo(string format, params object[] args)
        {
            string msg = format;
            if (args != null)
            {
                msg = string.Format(format, args);
            }
            var saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(msg);
            Console.ForegroundColor = saved;
        }

        private static void WriteNormal(string format, params object[] args)
        {
            string msg = format;
            if (args != null)
            {
                msg = string.Format(format, args);
            }
            Console.WriteLine(msg);
        }

        private static long CopyToFile(Stream stream, string path)
        {
            long length = 0;
            byte[] buffer = new byte[64000];
            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                int len = stream.Read(buffer, 0, buffer.Length);
                while (len > 0)
                {
                    file.Write(buffer, 0, len);
                    length += len;
                    len = stream.Read(buffer, 0, buffer.Length);
                }
            }
            return length;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("HttpGet [options] <url>");
            Console.WriteLine("Fetches the resource at the given URL and saves it locally");
            Console.WriteLine("Options:");
            Console.WriteLine("   -all       if url is html it brings down all locally referenced resources with the file including css, scripts and images");
            Console.WriteLine("   -deep      does -all deeply (traverses <a> links in same domain)");
            Console.WriteLine("   -filename  the name of the file to save http content into (default writes to stdout)");
            Console.WriteLine("   -headers   just print http headers to stdout");
            Console.WriteLine("   -merge uri with -all this merges content from another baseUri.");
            Console.WriteLine("   -root      a root path to remove from all relative links.");
        }
    }
}
