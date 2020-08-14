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
        bool toWin = true;
        bool recurse;
        Encoding encoding;
        byte[] utf8bom = new byte[] { 0xef, 0xbb, 0xbf };

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

        bool HasUTF8BOM(byte[] buffer)
        {
            return buffer[0] == utf8bom[0] && buffer[1] == utf8bom[1] && buffer[2] == utf8bom[2];
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: normalizenewlines [options] files...");
            Console.WriteLine("Normalizes the newlines in the given files from \r\n to \n and strip any training whitespace");
            Console.WriteLine("Options:");
            Console.WriteLine("    /win   - normalize everything to windows style \r\n (this is the default)");
            Console.WriteLine("    /linux - normalize everything to linux style \n");
            Console.WriteLine("    /encoding name - use the specified encoding (default is whatever the file had)");
            Console.WriteLine("    /r     - recurse down the folder hierarchy");
        }

        private void Run()
        {
            foreach (string file in FindFiles())
            {
                NormalizeFile(file);
            }
        }

        private IEnumerable<string> FindFiles()
        {
            foreach (var path in this.files)
            {
                foreach (var file in FindFiles(Path.GetDirectoryName(path), Path.GetFileName(path)))
                {
                    yield return file;
                }
            }
        }

        private IEnumerable<string> FindFiles(string dir, string name)
        {
            // expand wild cards.
            foreach (string file in Directory.GetFiles(dir, name))
            {
                yield return file;
            }

            if (recurse)
            {
                foreach (string child in Directory.GetDirectories(dir))
                {
                    foreach(var file in FindFiles(child, name))
                    {
                        yield return file;
                    }
                }
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
                int trimmed = 0;
                string tempPath = Path.GetTempFileName();
                byte[] preamble = new byte[10];
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(preamble, 0, 10);
                }
                StringBuilder sb = new StringBuilder();
                using (StreamReader r = new StreamReader(file))
                {
                    Encoding e = encoding;
                    int next = r.Read();
                    if (e == null)
                    {
                        e = r.CurrentEncoding;
                        if (e.WebName == "utf-8" && !HasUTF8BOM(preamble))
                        {
                            e = new UTF8Encoding(false); // no BOM
                        }
                    }
                    char previous = '\0';
                    using (TextWriter w = new StreamWriter(tempPath, false, e))
                    {
                        while (next != -1)
                        {
                            char ch = Convert.ToChar(next);
                            if (ch == '\r' || ch == '\n')
                            {
                                string line = sb.ToString();
                                if (line.Length > 0 && char.IsWhiteSpace(line[line.Length - 1]))
                                {
                                    line = line.TrimEnd();
                                    trimmed++;
                                }
                                sb.Length = 0;
                                w.Write(line);
                            }
                            else
                            {
                                // keep trailing whitespace in case it is trailing.
                                sb.Append(ch);
                            }
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
                                    }
                                    hasReturn = false;
                                    w.Write('\r');
                                    w.Write('\n');
                                }
                                else
                                {
                                    hasReturn = false;
                                }
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
                                }
                            }
                            next = r.Read();
                            previous = ch;
                        }
                        {
                            // write the last line!
                            string line = sb.ToString();
                            if (line.Length > 0 || hasReturn)
                            {
                                if (char.IsWhiteSpace(line[line.Length - 1]))
                                {
                                    line = line.TrimEnd();
                                    trimmed++;
                                }
                                sb.Length = 0;
                                w.Write(line);
                                if (toWin)
                                {
                                    w.Write('\r');
                                    w.Write('\n');
                                }
                                else
                                {
                                    w.Write('\n');
                                }
                            }
                        }
                    }
                }
                string msg = "";
                bool clean = normalized == 0 && trimmed == 0;
                if (!clean)
                {
                    File.Copy(tempPath, file, true);
                } else { 
                    msg = "clean";
                }
                File.Delete(tempPath);

                if (normalized > 0)
                {
                    msg = string.Format("normalized {0} lines", normalized);
                }
                if (trimmed > 0)
                {
                    if (msg.Length > 0)
                    {
                        msg += ", ";
                    }
                    msg += string.Format("trimmed {0} lines", trimmed);
                }
                Console.WriteLine(msg);
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
                        case "r":
                        case "recurse":
                            recurse = true;
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
                        files.Add(path);
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
