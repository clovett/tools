using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WebSiteMonitor
{
    class WebSiteUrl
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("success")]
        public string Success { get; set; }
    }

    class WebSiteConfig
    {
        public WebSiteConfig()
        {
            SleepSeconds = 3600;
        }

        /// <summary>
        /// Time to sleep between each batch of requests.
        /// </summary>
        [JsonProperty("sleepseconds")]
        public int SleepSeconds { get; set; }

        /// <summary>
        /// THe URL's to request
        /// </summary>
        [JsonProperty("websites")]
        public List<WebSiteUrl> WebSites { get; set; }
    }
    
    class PerformanceData
    {
        [JsonProperty("timeoftest")]
        public string TimeOfTest { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("length")]
        public long Length { get; set; }

        [JsonProperty("seconds")]
        public double Seconds { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }
    }

    class PerformanceLog
    {
        public List<PerformanceData> Results { get; set; }
    }

    class Program
    {
        string configFile;
        string resultFile;
        WebSiteConfig data;
        PerformanceLog log = new PerformanceLog();
        bool verbose = false;

        static int Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return 1;
            }
            return p.Run();
        }

        private int Run()
        {
            try
            {
                LoadConfig();

                resultFile = Path.Combine(Path.GetDirectoryName(this.configFile), Path.GetFileNameWithoutExtension(this.configFile) + ".log");
                LoadResults();

                while (true)
                {
                    RunTest();

                    if (verbose) Console.WriteLine("Sleeping for {0} seconds...", data.SleepSeconds);
                    System.Threading.Thread.Sleep(data.SleepSeconds * 1000);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error loading file '{0}' : {1}", configFile, e.Message);
                return 1;
            }
        }

        private void LoadConfig()
        {
            using (StreamReader reader = new StreamReader(this.configFile))
            {
                string json = reader.ReadToEnd();
                data = Newtonsoft.Json.JsonConvert.DeserializeObject<WebSiteConfig>(json);
            }
            if (data.WebSites == null)
            {
                Console.WriteLine("Config file '{0}' is missing 'websites' URL array", configFile);
                data.WebSites = new List<WebSiteUrl>();
            }
        }

        private void RunTest()
        {            
            var time = DateTime.Now;
            int i = 0;
            foreach (WebSiteUrl website in data.WebSites)
            {
                PerformanceData pd = new PerformanceData();
                pd.Url = website.Url;
                pd.TimeOfTest = time.ToLongDateString() + " " + time.ToLongTimeString();

                Stopwatch watch = new Stopwatch();
                watch.Start();

                try
                {
                    if (verbose) Console.WriteLine("Fetching: {0}", website.Url);
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(website.Url);
                    req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36 Edge/15.15063";
                    req.Credentials = CredentialCache.DefaultNetworkCredentials;
                    req.Method = "GET";

                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    {
                        if (resp.StatusCode == HttpStatusCode.OK)
                        {
                            using (Stream s = resp.GetResponseStream())
                            {
                                StreamReader reader = new StreamReader(s);
                                string content = reader.ReadToEnd();
                                string success = website.Success;
                                int index = content.IndexOf(success);
                                if (index < 0)
                                {
                                    pd.Error = string.Format("Success string '{0}' not found in returned web content", success);
                                }
                                pd.Length = content.Length;
                            }
                        }
                        else
                        {
                            pd.Error = resp.StatusDescription;
                        }
                    }
                }

                catch (Exception ex)
                {
                    pd.Error = ex.Message;
                }

                watch.Stop();
                pd.Seconds = (double)watch.ElapsedMilliseconds / 1000;
                log.Results.Add(pd);
                i++;

                if (verbose) Console.WriteLine("    Time: {0}", pd.Seconds);
                if (verbose) Console.WriteLine("    Error: {0}", pd.Error);
            }

            SaveResults();
        }

        private void LoadResults()
        {
            if (File.Exists(this.resultFile))
            {
                using (StreamReader reader = new StreamReader(this.resultFile))
                {
                    string json = reader.ReadToEnd();
                    this.log = Newtonsoft.Json.JsonConvert.DeserializeObject<PerformanceLog>(json);                    
                }
            }
            if (this.log.Results == null)
            {
                this.log.Results = new List<PerformanceData>();
            }
        }

        private void SaveResults()
        {
            using (StreamWriter writer = new StreamWriter(this.resultFile))
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(this.log, Formatting.Indented);
                writer.WriteLine(json);
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: WebSiteMonitor -config filename");
            Console.WriteLine("Monitors given URL's specified in config file");
            Console.WriteLine("Config file is json file with 'website' object which is array of URLs");
            Console.WriteLine("This generates a .log file to match, also json, with test tims and performance results");
        }

        bool ParseCommandLine(string[] args)
        {

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "h":
                        case "?":
                        case "help":
                            return false;
                        case "v":
                        case "verbose":
                            verbose = true;
                            break;
                        case "c":
                        case "config":
                            if (i + 1 < args.Length)
                            {
                                this.configFile = args[++i];
                            }
                            else
                            {
                                Console.WriteLine("Unexpected filename after -config");
                                return false;
                            }
                            break;
                        default:
                            Console.WriteLine("Unexpected argument: " + arg);
                            return false;
                    }
                }
            }

            if (string.IsNullOrEmpty(configFile))
            {
                Console.WriteLine("Missing config argument");
                return false;
            }

            if (!System.IO.File.Exists(configFile))
            {
                Console.WriteLine("Config file '{0}' not found", configFile);
                return false;
            }

            return true;
        }


    }
}
