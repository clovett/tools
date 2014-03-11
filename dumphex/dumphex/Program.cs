using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dumphex
{
    class Program
    {
        List<string> files = new List<string>();

        static void Main(string[] args)
        {
            //foreach (string uri in Directory.GetFiles(path, Path.GetFileName(inputUri))) {
            Program p = new Program();
            if (p.ParseCommandLine(args))
            {
                p.Run();
            }
            else
            {
                PrintUeage();
            }
        }

        private static void PrintUeage()
        {
            Console.WriteLine("Usage: DumpHex <files>");
            Console.WriteLine("Outputs the content of the given binary file(s) in hex format");
        }

        private void Run()
        {
            foreach (string file in this.files)
            {
                try
                {
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        if (this.files.Count > 1)
                        {
                            Console.WriteLine();
                            Console.WriteLine(file);
                            Console.WriteLine("-------------------------------------------------------------------------------------");
                        }
                        DumpHex(fs);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("### Error opening file: " + file);
                    Console.WriteLine("### " + ex.Message);
                }
            }
        }

        private bool ParseCommandLine(string[] args)
        {
            for (int i = 0, n = args.Length; i < n; i++)
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
                    }
                }
                else
                {
                    Uri baseUri = new Uri(Directory.GetCurrentDirectory() + "/");
                    Uri resolved = new Uri(baseUri, arg);
                    string fullPath = resolved.LocalPath;
                    string dir = Path.GetDirectoryName(fullPath);
                    string pattern = Path.GetFileName(fullPath);

                    try
                    {
                        foreach (string file in Directory.GetFiles(dir, pattern))
                        {
                            this.files.Add(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("### Error finding files in " + dir + " matching pattern " + pattern);
                        Console.WriteLine("### " + ex.Message);
                        return false;
                    }
                }
            }
            if (files.Count == 0)
            {
                Console.WriteLine("### Error: missing arguments");
                return false;
            }
            return true;
        }

        private void DumpHex(Stream stream)
        {
            byte[] line = new byte[16];
            while (true)
            {
                int lineLnegth = stream.Read(line, 0, 16);
                if (lineLnegth == 0)
                {
                    break;
                }
                for (int j = 0; j < lineLnegth; j++)
                {
                    if (j > 0)
                    {
                        Console.Write(" ");
                    }
                    byte b = line[j];
                    Console.Write(b.ToString("x2"));
                }

                Console.Write("  ");

                for (int j = 0; j < lineLnegth; j++)
                {
                    byte b = line[j];
                    char c = Convert.ToChar(b);
                    if (Char.IsLetterOrDigit(c) || Char.IsPunctuation(c))
                    {
                        Console.Write(c);
                    }
                    else
                    {
                        Console.Write(".");
                    }
                }

                Console.WriteLine();
            }
        }

    }
}
