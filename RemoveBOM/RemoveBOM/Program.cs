using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoveBOM
{
    class Program
    {
        List<string> files = new List<string>();

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
            Console.WriteLine("Usage: removebom [options] files...");
            Console.WriteLine("Removes byte order mark from beginning of the file");
        }

        private void Run()
        {
            foreach (string file in files)
            {
                RemoveByteOrderMark(file);
            }
        }

        private void RemoveByteOrderMark(string file)
        {
            Console.Write(file);
            Console.Write("...");

            int bufsize = 1000000;
            byte[] buffer = new byte[bufsize];

            try
            {
                string message = "no byte order mark found";
                bool removed = false;
                bool first = true;
                string tempPath = Path.GetTempFileName();
                using (Stream output = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                {
                    using (Stream input = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        int len = input.Read(buffer, 0, bufsize);
                        while (len > 0)
                        {
                            int start = 0;
                            if (first)
                            {
                                if (len >= 3 && buffer[0] == 0xef && buffer[1] == 0xbb  && buffer[2] == 0xbf)
                                {
                                    message = "removed UTF8 byte order mark (0xef 0xbb 0xbf)";
                                    start += 3;
                                }
                                else if (len >= 2 && buffer[0] == 0xfe && buffer[1] == 0xff)
                                {
                                    message = "removed big endian UTF-16 byte oder mark (0xfe 0xff)";
                                    start += 2;
                                }
                                else if (len >= 2 && buffer[0] == 0xff && buffer[1] == 0xfe)
                                {
                                    message = "removed little endian UTF-16 byte oder mark (0xff 0xfe)";
                                    start += 2;
                                }
                                else if (len >= 4 && buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0xfe && buffer[3] == 0xff)
                                {
                                    message = "removed big endian UTF-32 byte oder mark (0x00 0x00 0xfe 0xff)";
                                    start += 4;
                                }
                                else if (len >= 4 && buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0xff && buffer[3] == 0xfe)
                                {
                                    message = "removed big endian UTF-32 byte oder mark (0x00 0x00 0xff 0xfe)";
                                    start += 4;
                                }
                                removed = (start > 0);
                                first = false;
                            }

                            if (buffer[len-1] == '\0' && buffer[len - 2] == '\0' && buffer[len - 3] == '\0')
                            {
                                len -= 3;
                            }

                            output.Write(buffer, start, len - start);

                            len = input.Read(buffer, 0, bufsize);
                        }
                    }
                }

                if (removed)
                {
                    File.Copy(tempPath, file, true);
                }
                File.Delete(tempPath);

                Console.WriteLine(message);
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
