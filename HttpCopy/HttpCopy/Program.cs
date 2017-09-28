using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using Microsoft.Win32;

namespace HttpCopy 
{
    class Program 
    {

        string http = null;
        string file = null;
        Dictionary<string, string> mimemap;

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
            catch (Exception e)
            {
                Console.WriteLine("### Error: " + e.Message);
                return 1;
            }
            return 0;
        }

        void Run()
        {
            Uri uri = new Uri(http);

            string name = file;
            if (uri.Segments.Length > 1)
            {
                name = uri.Segments[uri.Segments.Length - 1].Replace("/", "");
            }

            if (string.IsNullOrEmpty(name))
            {
                name = "default.htm";
            }

            byte[] buffer = new byte[64000];
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
            req.Credentials = CredentialCache.DefaultCredentials;
            HttpWebResponse resp = (HttpWebResponse )req.GetResponse();
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Http Error " + resp.StatusDescription);
                return;
            }

            string contentType = resp.ContentType;

            if (file == null && !string.IsNullOrEmpty(contentType))
            {
                string ext = "";
                string[] parts = contentType.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    string mime = parts[0];
                    
                    LoadMimeDatabase();

                    mimemap.TryGetValue(mime, out ext);
                }

                string ne = Path.GetExtension(name);
                if (!string.IsNullOrEmpty(ext) && string.Compare(ne, ext, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    name += ext;
                }
            }

            if (File.Exists(name))
            {
                Console.Write(string.Format("*** File '{0}' exists, are you sure you want to override it (Y|N) ?", name));
                string ans = Console.ReadLine();
                ans = ans.ToLower();
                if (ans != "y" && ans != "yes")
                    return;
            }


            FileStream fs = new FileStream(name, FileMode.Create, FileAccess.Write);
            using (fs)
            {
                using (Stream stm = resp.GetResponseStream())
                {
                    int i;
                    while ((i = stm.Read(buffer, 0, buffer.Length)) > 0)
                    {
                            
                        fs.Write(buffer, 0, i);
                    }
                }
            }
            Console.WriteLine("downloaded " + name);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: HttpCopy <url> [<filename>]");
            Console.WriteLine("Downloads the content at the given url and stores it in a local file");
            Console.WriteLine("If no local filename is provided it uses the last segment of the url as the filename");
        }


        bool ParseCommandLine(string[] args)
        {
            for (int i = 0, n = args.Length; i < n; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "h":
                        case "help":
                        case "?":
                            return false;
                    }
                }
                else if (http == null)
                {
                    http = arg;
                }
                else if (file == null)
                {
                    file = arg;
                }
                else
                {
                    Console.WriteLine("Error: too many command line arguments");
                    return false;
                }
            }
            if (http == null) 
            {
                Console.WriteLine("Missing http address");
                return false;
            }
            return true;
        }

        void LoadMimeDatabase()
        {
            mimemap = new Dictionary<string, string>();
            using (RegistryKey ct = Registry.ClassesRoot.OpenSubKey(@"MIME\Database\Content Type", false))
            {
                foreach (string subKey in ct.GetSubKeyNames())
                {
                    using (RegistryKey sk = ct.OpenSubKey(subKey, false))
                    {
                        string ext = (string)sk.GetValue("Extension");
                        if (!string.IsNullOrEmpty(ext))
                        {
                            mimemap[subKey] = ext;
                        }
                    }
                }
            }

            // not sure why this is missing.
            mimemap["text/javascript"] = ".js";
        }

    }
}
