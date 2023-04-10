using System.Xml;

namespace xmlformat
{
    internal class Program
    {
        bool strip;
        int indent = 2;
        char indentChar = ' ';
        List<string> filenames = new ();

        private bool ParseCommandLine(string[] args )
        {
            for (int i = 0, n = args.Length; i < n; i++)
            {
                var arg = args[i];
                if (arg.StartsWith('-'))
                {
                    switch (arg.TrimStart('-').ToLowerInvariant())
                    {
                        case "?":
                        case "help":
                            return false;
                        case "strip":
                            strip = true;
                            break;  
                        case "indent":
                            if (i + 1 < n)
                            {
                                if (!int.TryParse(args[++i], out indent))
                                {
                                    Console.WriteLine("Invalid indent size: " + args[i]);
                                    return false;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Missing indent size");
                                return false;
                            }
                            break;
                        case "usetabs":
                            indentChar = '\t';
                            break;
                        default:
                            Console.WriteLine($"### unknown argument {arg}");
                            return false;
                    }
                }
                else
                {
                    if (Directory.Exists(arg))
                    {
                        Console.WriteLine($"Input {arg} is a directory, if you want to process a directory enter a wild card pattern like 'name\\*.xml'");
                        return false;
                    }
                    filenames.Add(arg);
                }
            }
            if (filenames.Count == 0)
            {
                Console.WriteLine("Missing xml file names to format");
                return false;
            }
            return true;
        }

        void Run()
        {
            foreach (var file in this.filenames)
            {
                
                var dir = Path.GetDirectoryName(file);
                if (string.IsNullOrEmpty(dir))
                {
                    dir = ".";
                }
                foreach (var name in Directory.GetFiles(dir, Path.GetFileName(file)))
                {
                    Process(name);
                }
            }
        }

        void Process(string name)
        {
            var settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            XmlDocument doc = new XmlDocument();
            using (var reader = XmlReader.Create(name, settings))
            {
                doc.Load(reader);
            }

            var ws = new XmlWriterSettings();
            if (strip)
            {
                ws.Indent = false;
            }
            else
            {
                ws.Indent = true;
                ws.IndentChars = new string(this.indentChar, this.indent);
            }
            using (var w = XmlWriter.Create(name, ws))
            {
                doc.Save(w);
            }
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            if (p.ParseCommandLine(args)) 
            {
                p.Run();
            }
            else
            {
                p.PrintUsage();
            }
        }

        private void PrintUsage()
        {
            Console.WriteLine("Formats the given xml document, supports the following options:");
            Console.WriteLine("--strip     stip all non-preserved whitespace");
            Console.WriteLine("--indent N  set formatting indent size in chars");
            Console.WriteLine("--useTabs   set formatting indent character to a TAB character (default is a space)");
        }
    }
}