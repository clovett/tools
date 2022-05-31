using System;
using System.Xml;
using System.Xml.Linq;
using System.IO.Compression;

namespace TestDriver
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

                XmlTestDriver driver = new XmlTestDriver();
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
}