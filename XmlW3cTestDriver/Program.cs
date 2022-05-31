using System;
using System.Xml;
using System.Xml.Linq;
using System.IO.Compression;

namespace MyApp // Note: actual namespace depends on the project name.
{

    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            foreach (string url in args)
            {
                string fileName = null;
                Console.WriteLine($"Running test from {url}");
                Uri uri = new Uri(url, uriKind: UriKind.RelativeOrAbsolute);
                if (!uri.IsAbsoluteUri)
                {
                    uri = new Uri(Path.GetFullPath(url));
                }
                if (uri.Scheme == "http" || uri.Scheme == "https")
                {
                    fileName = await DownloadTest(uri);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        return 1;
                    }
                }
                else if (uri.Scheme == "file")
                {
                    fileName = url;
                }
                else
                {
                    Console.WriteLine($"Unsupported Uri scheme: {uri.Scheme}");
                    return 1;
                }

                TestDriver driver = new TestDriver();
                driver.RunSuite(fileName);
                driver.Report();
            }
            return 0;
        }

        static async Task<string> DownloadTest(Uri uri)
        {
            var fileName = uri.Segments[uri.Segments.Length - 1];
            var dir = Directory.GetCurrentDirectory();
            var outDir = Path.Combine(dir, Path.GetFileNameWithoutExtension(fileName));
            if (Directory.Exists(outDir))
            {
                Console.WriteLine($"Local cache {outDir} already exists so using it");
            }
            else
            {
                Console.WriteLine($"Downloading {fileName}...");
                HttpClient client = new HttpClient();
                var response = await client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                var stream = response.Content.ReadAsStream();
                using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    await stream.CopyToAsync(fs);
                }
                Console.WriteLine($"Extracting to {outDir}...");
                Directory.CreateDirectory(outDir);
                ZipFile.ExtractToDirectory(fileName, outDir);
            }

            fileName = Path.Combine(outDir, "xmlconf", "xmlconf.xml");
            if (!File.Exists(fileName))
            {
                Console.WriteLine($"### Missing file {fileName} in zip archive {uri.OriginalString}");
                return null;
            }
            return fileName;
        }
    }

    internal class TestDriver
    {
        int totalTests;
        int testsFailed;
        int testsPassed;

        public void Report()
        {
            Console.WriteLine($"{totalTests} tests run ");
            Console.WriteLine($"{testsFailed} tests failed ");
            Console.WriteLine($"{testsPassed} tests passed ");
            var rate = (testsPassed * 100) / totalTests;
            Console.WriteLine($"pass rate {rate} %");
        }

        private XDocument LoadXml(string fileName)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ConformanceLevel = ConformanceLevel.Document;
            settings.DtdProcessing = DtdProcessing.Parse;
            settings.CheckCharacters = true;
            settings.ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.ProcessInlineSchema;
            settings.XmlResolver = new XmlUrlResolver();
            using (var reader = XmlReader.Create(fileName, settings))
            {
                return XDocument.Load(reader, LoadOptions.SetBaseUri);
            }
        }

        public void RunSuite(string xmlFile)
        {
            var dir = System.IO.Path.GetDirectoryName(xmlFile);
            XDocument doc = LoadXml(xmlFile);
            var xmlNs = XNamespace.Get("http://www.w3.org/XML/1998/namespace");
            foreach (XElement suite in doc.Root.Elements("TESTCASES"))
            {
                var profile = (string)suite.Attribute("PROFILE");
                Console.WriteLine($"============= {profile} ================================= ");
                foreach (XElement test in suite.Descendants("TEST"))
                {
                    RunTest(test);
                }
            }
        }

        public void RunTest(XElement test)
        {
            totalTests++;
            Uri baseUri = new Uri(test.BaseUri);
            var type = (string)test.Attribute("TYPE");
            var id = (string)test.Attribute("ID");
            var sections = (string)test.Attribute("SECTIONS");
            var uri = (string)test.Attribute("URI");
            Uri resolved = new Uri(baseUri, uri);
            Console.Write($"Test {id} section {sections} type {type}...");

            Exception error = null;
            try
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ConformanceLevel = ConformanceLevel.Document;
                settings.DtdProcessing = DtdProcessing.Parse;
                settings.CheckCharacters = true;
                settings.ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.ReportValidationWarnings;
                settings.XmlResolver = new XmlUrlResolver();
                using (var reader = XmlReader.Create(resolved.LocalPath, settings))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(reader);
                }
            } 
            catch (Exception ex)
            {
                // was it supposed to fail?
                error = ex;
            }

            switch (type)
            {
                case "valid":
                    if (error != null)
                    {
                        testsFailed++;
                        Console.WriteLine($"failed: {type} test raised unexpected error: {error.Message}");
                    }
                    else
                    {
                        testsPassed++;
                        Console.WriteLine("passed");
                    }
                    break;
                case "invalid":
                    goto case "error";
                case "not-wf":
                    goto case "error";
                case "error":
                    if (error == null)
                    {
                        testsFailed++;
                        Console.WriteLine($"failed: {type} test should have raised an error because");
                        Console.WriteLine(test.Value);
                    }
                    else
                    {
                        // ah but did it fail for the right reason???
                        testsPassed++;
                        Console.WriteLine("passed");
                    }
                    break;
                default:
                    Console.WriteLine($"### Unexpected test type {type}");
                    break;
            }
        }

    }
}