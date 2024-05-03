﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace dumphex
{
    class Program
    {
        List<string> files = new List<string>();
        int count;
        bool cpp = false;
        bool decimalHeader = false;
        bool lineNumbers = false;
        string headerFormat = null;

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
                PrintUsage();
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: DumpHex [options] <files>");
            Console.WriteLine("Outputs the content of the given binary file(s) in hex format");
            Console.WriteLine("Options:");
            Console.WriteLine("   -c count    dumps the first count bytes only");
            Console.WriteLine("   -cpp        outputs in c++ format");
            Console.WriteLine("   -hex        add hex offset to each line");
            Console.WriteLine("   -dec        add decimal offset");
            Console.WriteLine("   -line       add line number with offset");
        }

        private void Run()
        {
            foreach (string file in this.files)
            {
                try
                {
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        if (decimalHeader)
                        {
                            var digits = fs.Length.ToString().Length;
                            if (this.lineNumbers)
                            {
                                this.headerFormat = "{0:D" + digits.ToString() + "}, {1:D" + digits.ToString() + "}: ";
                            }
                            else
                            {
                                this.headerFormat = "{0:D" + digits.ToString() + "}: ";
                            }
                        } else { 
                            var digits = fs.Length.ToString("x").Length;
                            if (this.lineNumbers)
                            {
                                this.headerFormat = "{0:X" + digits.ToString() + "}, {1:X" + digits.ToString() + "}: ";
                            }
                            else
                            {
                                this.headerFormat = "{0:X" + digits.ToString() + "}: ";
                            }
                        }

                        if (this.files.Count > 1)
                        {
                            Console.WriteLine();
                            Console.WriteLine(file);
                            Console.WriteLine("-------------------------------------------------------------------------------------");
                        }
                        if (cpp)
                        {
                            DumpCpp(fs);
                        }
                        else
                        {
                            DumpHex(fs);
                        }
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
                        case "hex":
                            // this is the default
                            break;
                        case "dec":
                            decimalHeader = true;
                            break;
                        case "line":
                            lineNumbers = true;
                            break;
                        case "c":
                            int count = 0;
                            if (i + 1 < n && int.TryParse(args[i+1], out count))
                            {
                                this.count = count;
                                i++;
                            }
                            break;
                        case "cpp":
                            cpp = true;
                            break;
                    }
                }
                else
                {
                    Uri baseUri = new Uri(Directory.GetCurrentDirectory() + "/");
                    Uri resolved = new Uri(baseUri, arg);
                    string fullPath = resolved.LocalPath;
                    string dir = Path.GetDirectoryName(fullPath);
                    string pattern = Path.GetFileName(fullPath);
                    int count = 0;
                    try
                    {
                        foreach (string file in Directory.GetFiles(dir, pattern))
                        {
                            count++;
                            this.files.Add(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("### " + ex.Message);
                        return false;
                    }

                    if (count == 0)
                    {
                        Console.WriteLine("### Error finding files in " + dir + " matching pattern " + pattern);
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
            long position = 0;
            long lineNumber = 0;
            byte[] line = new byte[16];
            while (true)
            {
                PrintHeader(lineNumber, position);
                int lineLength = stream.Read(line, 0, 16);
                if (lineLength == 0)
                {
                    break;
                }

                int j = 0;
                for (j = 0; j < lineLength; j++)
                {
                    if (j > 0)
                    {
                        Console.Write(" ");
                    }
                    byte b = line[j];
                    Console.Write(b.ToString("x2"));
                }

                while (j++ < 16)
                {
                    Console.Write("   ");
                }

                Console.Write("  ");

                for (j = 0; j < lineLength; j++)
                {
                    byte b = line[j];

                    if (lineNumbers)
                    {
                        if (b == '\n')
                        {
                            lineNumber++;
                            position = 0;
                        }
                        else
                        {
                            position++;
                        }
                    }
                    else
                    {
                        position++;
                    }
                    char c = Convert.ToChar(b);
                    if (Char.IsLetterOrDigit(c) || Char.IsPunctuation(c) || (c >= 0x21 && c <= 0x7e))
                    {
                        Console.Write(c);
                    }
                    else
                    {
                        Console.Write(".");
                    }
                }

                Console.WriteLine();

                if (this.count != 0 && stream.Position >= this.count)
                {
                    break;
                }
            }
        }

        private void PrintHeader(long lineNumber, long position)
        {
            if (this.headerFormat != null)
            {
                if (this.lineNumbers)
                {
                    Console.Write(this.headerFormat, lineNumber, position);
                }
                else {
                    Console.Write(this.headerFormat, position);
                }
            }
        }

        private void DumpCpp(FileStream stream)
        {
            int position = 0;
            byte[] line = new byte[16];
            int lineNumber = 0;
            while (true)
            {
                PrintHeader(lineNumber, stream.Position);

                int lineLength = stream.Read(line, 0, 16);
                if (lineLength == 0)
                {
                    break;
                }

                for (int j = 0; j < lineLength; j++)
                {
                    if (j > 0)
                    {
                        Console.Write(", ");
                    }
                    byte b = line[j];
                    if (b == '\n')
                    {
                        lineNumber++;
                        position = 0;
                    }
                    else
                    {
                        position++;
                    }
                    Console.Write("0x" + b.ToString("x2") );
                }
                
                Console.WriteLine(",");

                if (this.count != 0 && stream.Position >= this.count)
                {
                    break;
                }
            }
        }

    }
}
