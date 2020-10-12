using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LovettSoftware.Tools
{
    class Program
    {
        List<string> files = new List<string>();
        string bom = null; // type of BOM to add
        
        enum BomType
        {
            None,
            BigEndian,
            LittleEndian
        }
        BomType requested = BomType.None;

        byte[] utf8 = new byte[] { 0xef, 0xbb, 0xbf };
        byte[] utf16big = new byte[] { 0xfe, 0xff };
        byte[] utf16little = new byte[] { 0xff, 0xfe };
        byte[] utf32big = new byte[] { 0x00, 0x00, 0xfe, 0xff };
        byte[] utf32little = new byte[] { 0x00, 0x00, 0xff, 0xfe };

        class ByteOrderMark
        {
            public byte[] bom;
            public string name;
            public BomType type;

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bom)
                {
                    if(sb.Length > 0)
                    {
                        sb.Append(' ');
                    }
                    sb.AppendFormat("0x{0:x2}", b);
                }
                var result = this.name;
                if (type == BomType.LittleEndian)
                {
                    result += " little endian";
                }
                else if (type == BomType.BigEndian)
                {
                    result += " big endian";
                }
                return result + " byte order mark (" + sb.ToString() + ")";
            }
        }

        List<ByteOrderMark> boms;

        public Program()
        {
            boms = new List<ByteOrderMark>();
            boms.Add(new ByteOrderMark() { bom = utf8, name = "utf-8", type = BomType.None });
            boms.Add(new ByteOrderMark() { bom = utf16big, name = "utf-16", type = BomType.BigEndian });
            boms.Add(new ByteOrderMark() { bom = utf16little, name = "utf-16", type = BomType.LittleEndian });
            boms.Add(new ByteOrderMark() { bom = utf32big, name = "utf-32", type = BomType.BigEndian });
            boms.Add(new ByteOrderMark() { bom = utf32little, name = "utf-32", type = BomType.LittleEndian });

        }

        static void Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                p.PrintUsage();
                return;
            }
            p.Run();
        }

        private void PrintUsage()
        {
            Console.WriteLine("Usage: bom [options] files...");
            Console.WriteLine("Edit byte order mark at beginning of the file (without re-encoding the file)");
            Console.WriteLine("-a encoding   -- add byte order mark for given encoding ({0})", string.Join(", ", GetEncodings()));
            Console.WriteLine("-e big|little -- for utf16 and utf32 bom");
            Console.WriteLine("-r            -- remove byte order mark");
        }

        private HashSet<string> GetEncodings()
        {
            HashSet<string> expected = new HashSet<string>();
            for (int j = 0; j < boms.Count; j++)
            {
                var b = boms[j];
                expected.Add(b.name);
            }
            return expected;
        }

        private void Run()
        {
            foreach (string file in files)
            {
                Process(file);
            }
        }


        private void Process(string file)
        {
            Console.Write(file);
            Console.Write("...");

            int bufsize = 1000000;
            byte[] buffer = new byte[bufsize];

            try
            {
                string initial = "no byte order mark found";
                string message = initial;
                bool changed = false;
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
                                foreach (ByteOrderMark b in boms)
                                {
                                    if (IsSameBom(b.bom, buffer, len))
                                    {
                                        message = "Removing " + b.ToString();
                                        start += b.bom.Length;
                                    }
                                }
                                changed = (start > 0);
                                first = false;

                                if (!string.IsNullOrEmpty(bom))
                                {
                                    for (int j = 0; j < boms.Count; j++)
                                    {
                                        var b = boms[j];
                                        // default to little endian if nothing requested.
                                        if (string.Compare(bom, b.name) == 0 && (requested == b.type || (b.type == BomType.LittleEndian && requested == BomType.None)))
                                        {
                                            output.Write(b.bom, 0, b.bom.Length);
                                            string addMessage = "Adding " + b.ToString();
                                            if (message == initial)
                                            {
                                                message = addMessage;
                                            }
                                            else
                                            {
                                                message += " " + addMessage;
                                            }
                                            changed = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            
                            output.Write(buffer, start, len - start);

                            len = input.Read(buffer, 0, bufsize);

                        }
                    }
                }

                Console.WriteLine(message);
                if (changed)
                {
                    File.Copy(tempPath, file, true);
                }
                File.Delete(tempPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("failed\n### Error: " + ex.Message);
            }
        }

        private bool IsSameBom(byte[] bom, byte[] buffer, int len)
        {
            if (len > bom.Length)
            {
                for (int i = 0; i < bom.Length; i++)
                {
                    if (bom[i] != buffer[i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        bool ParseCommandLine(string[] args)
        {
            Uri baseUri = new Uri(Environment.CurrentDirectory + Path.DirectorySeparatorChar);
            for (int i = 0, n = args.Length; i < n; i++)
            {
                string arg = args[i];
                if (arg[0] == '/' || arg[0] == '-')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "h":
                        case "?":
                        case "help":
                            return false;
                        case "a":
                            if (i + 1 < n)
                            {
                                bool found = false;
                                bom = args[++i].ToLowerInvariant();
                                HashSet<string> expected = GetEncodings();
                                found = expected.Where(e => string.Compare(bom, e) == 0).Any();
                                if (!found)
                                { 
                                    Console.WriteLine("Error: expected " + string.Join(", ", expected) + " after -a");
                                    return false;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Error: expected type of bom to add after -a");
                                return false;
                            }
                            break;
                        case "e":
                            if (i + 1 < n)
                            {
                                arg = args[++i];
                                if (string.Compare(arg, "little") == 0)
                                {
                                    requested = BomType.LittleEndian;
                                }
                                else
                                {
                                    requested = BomType.BigEndian;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Error: expected 'big' or 'little' after -e");
                                return false;
                            }
                            break;
                        case "r":
                            this.bom = null;
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
