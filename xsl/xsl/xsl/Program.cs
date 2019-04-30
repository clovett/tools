//
// xsl : xsl transform tool
//
// Chris Lovett, August 2018
//
using System;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace xsl
{
    class XslCommandLine
    {
        List<string> files = new List<string>();
        XslCompiledTransform transform;
        string outdir;

        static void Main(string[] args)
        {
            XslCommandLine v = new XslCommandLine();
            if (!v.ParseCommandLine(args))
            {
                PrintUsage();
                return;
            }
            try
            {
                v.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("### Transform error: {0}", ex.Message);
            }
            return;
        }

        bool ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                String arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "s":
                        case "xslt":
                            if (i + 1 < args.Length)
                            {
                                i++;
                                string filename = args[i];
                                Console.WriteLine("Loading transform: " + filename);
                                try
                                {
                                    transform = new XslCompiledTransform(true);

                                    XsltSettings settings = new XsltSettings();
                                    settings.EnableDocumentFunction = true;
                                    settings.EnableScript = true;
                                    
                                    transform.Load(filename, settings, new XmlUrlResolver());
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("### XSLT Load Error: " + e.Message);
                                    if (e.InnerException != null)
                                    {
                                        Console.WriteLine(e.InnerException.Message);
                                    }
                                    return false;
                                }
                            }
                            break;
                        case "o":
                        case "output":
                            if (i + 1 < args.Length)
                            {
                                i++;
                                outdir = args[i];
                                if (!System.IO.Directory.Exists(outdir))
                                {
                                    outdir = System.IO.Directory.CreateDirectory(outdir).FullName;
                                }

                            }
                            break;
                        case "?":
                        case "h":
                        case "help":
                            return false;                        
                    }
                }
                else
                {
                    // add xml files and resolve any wild cards
                    string filename = args[i];
                    string path = null;
                    if (Directory.Exists(filename))
                    {
                        path = filename;
                        filename = "*.*";
                    }
                    else
                    {
                        path = Path.GetDirectoryName(filename);
                        filename = Path.GetFileName(filename);
                    }
                    if (string.IsNullOrEmpty(path))
                    {
                        path = Directory.GetCurrentDirectory();
                    }
                    DirectoryInfo di = new DirectoryInfo(path);
                    int count = 0;
                    foreach (FileInfo fi in di.GetFiles(Path.GetFileName(filename)))
                    {
                        count++;
                        files.Add(fi.FullName);
                    }
                    if (count == 0)
                    {
                        Console.WriteLine("No files matching name: {0}", args[i]);
                    }
                }
            }
            if (files.Count == 0)
            {
                Console.WriteLine("Must specify at least one input XML file to transform.");
                return false;
            }

            return true;
        }

        void Run()
        {
            foreach (string filename in files)
            {
                Transform(filename);
            }
        }

        void Transform(string filename)
        {
            string dir = System.IO.Path.GetDirectoryName(filename);
            string basename = System.IO.Path.GetFileNameWithoutExtension(filename);
            string ext = ".txt";
            switch (transform.OutputSettings.OutputMethod)
            {
                case XmlOutputMethod.Xml:
                    ext = ".xml";
                    break;
                case XmlOutputMethod.Html:
                    ext = ".htm";
                    break;
                case XmlOutputMethod.Text:
                    ext = ".txt";
                    break;
            }
            if (outdir != null)
            {
                dir = outdir;
            }
            string resultsFile = System.IO.Path.Combine(dir, basename + ext);
            if (string.Compare(filename, resultsFile, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                System.IO.Path.Combine(dir, basename + "_output" + ext);
            }

            Console.WriteLine("Transforming {0} ==> {1}", filename, resultsFile);
            transform.Transform(filename, resultsFile);
        }

        public static void PrintUsage()
        {
            Console.WriteLine("Usage: xsl -s xsl_file -o output_dir <xml_filenames>");
            Console.WriteLine("Transforms the given xml files using the given xsl file");
            Console.WriteLine("Options:");
            Console.WriteLine("  -s xsl_file        the XSLT transform to use");
            Console.WriteLine("  -o output_dir      the folder to put the transformed output");
            Console.WriteLine("  xml_filenames      the XML files to transform (supports wildcards)");
            Console.WriteLine();
            Console.WriteLine("The default output directory is the same folder as the XML file, and the output files will have the same name as the XML files but have a different file extension");
        }

    }
}
