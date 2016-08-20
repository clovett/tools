using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NormalizeNewlines
{
    class Program
    {
        List<string> files = new List<string>();
        bool toWin;
        Encoding encoding = Encoding.UTF8;

        static void Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return;
            }
            p.Run();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: normalizenewlines [options] files...");
            Console.WriteLine("Normalizes the newlines in the given files from \r\n to \n");
            Console.WriteLine("Options:");
            Console.WriteLine("    /win   - normalize everything to windows style \r\n");
            Console.WriteLine("    /linux - normalize everything to linux style \n (this is the default)");
            Console.WriteLine("    /encoding name - use the specified encoding");
        }

        private void Run()
        {
            foreach (string file in files)
            {
                NormalizeFile(file);
            }
        }

        private void NormalizeFile(string file)
        {
            Console.Write(file);
            Console.Write("...");
            try
            {
                bool hasReturn = false;
                int normalized = 0;
                string tempPath = Path.GetTempFileName();
                using (TextWriter w = new StreamWriter(tempPath, false, encoding))
                {
                    using (TextReader r = new StreamReader(file))
                    {
                        int next = r.Read();
                        while (next != -1)
                        {
                            char ch = Convert.ToChar(next);
                            if (toWin)
                            {
                                if (ch == '\r')
                                {
                                    hasReturn = true;
                                }
                                else if (ch == '\n')
                                {
                                    if (!hasReturn)
                                    {
                                        normalized++;
                                        w.Write('\r');
                                    }
                                    hasReturn = false;
                                }
                                else
                                {
                                    hasReturn = false;
                                }
                                w.Write(ch);
                            }
                            else
                            {
                                if (ch == '\r')
                                {
                                    if (hasReturn)
                                    {
                                        // two \r's in a row, that's odd.  Let's treat this as multiple lines then.
                                        normalized++;
                                        w.Write('\n');
                                    }
                                    hasReturn = true;
                                }
                                else if (ch == '\n')
                                {
                                    if (hasReturn)
                                    {
                                        hasReturn = false;
                                        normalized++;
                                    }
                                    w.Write(ch);
                                }
                                else
                                {
                                    if (hasReturn)
                                    {
                                        // odd, we found a '\r' without any '\n', so treat this as a newline.
                                        normalized++;
                                        hasReturn = false;
                                        w.Write('\n');
                                    }
                                    w.Write(ch);
                                }
                            }
                            next = r.Read();
                        }
                    }
                }

                File.Copy(tempPath, file, true);
                File.Delete(tempPath);

                if (normalized > 0)
                {
                    Console.WriteLine("normalized {0} lines", normalized);
                }
                else
                {
                    Console.WriteLine("clean");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("failed\n### Error: " + ex.Message);
            }
        }

        bool ParseCommandLine(string[] args)
        {
            Uri baseUri = new Uri(Environment.CurrentDirectory + Path.DirectorySeparatorChar);
            for (int i = 0, n = args.Length; i < n; i++)
            {
                string arg = args[i];
                if (arg[0] == '/' || arg[0] == '/')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "h":
                        case "?":
                        case "help":
                            return false;
                        case "win":
                            toWin = true;
                            break;
                        case "linux":
                            toWin = false;
                            break;
                        case "encoding":
                            if (i + 1 < n)
                            {
                                encoding = Encoding.GetEncoding(args[i + 1]);
                                i++;
                            }
                            else
                            {
                                Console.WriteLine("Error: encoding argument missing encoding name");
                            }
                            break;
                        default:
                            Console.WriteLine("Error: unexpected argument: " + arg);
                            return false;
                    }
                }
                else
                {
                    try
                    {
                        Uri resolved = new Uri(baseUri, arg);
                        string path = resolved.LocalPath;
                        // expand wild cards.
                        foreach (string file in Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path)))
                        {
                            files.Add(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: invalid file name : " + arg);
                        Console.WriteLine(ex.Message);
                        return false;
                    }
                }
            }
            if (files.Count == 0)
            {
                Console.WriteLine("Error: missing file name(s)");
                return false;
            }
            return true;
        }
    }
}
