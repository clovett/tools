using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace CapabilityMapping
{
    class Program
    {
        static void Main(string[] args)
        {
            Program p = new Program();
            p.MapWin81(args[0]);
            return;
        }

        private void LoadWin81()
        {
            const string mappingKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\SecurityManager\Capabilities\CapabilityMapping";
            const string windowsCapabilityKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\SecurityManager\WindowsCapabilities";

            using (Stream s = this.GetType().Assembly.GetManifestResourceStream("CapabilityMapping.win81map.txt"))
            {
                using (StreamReader sr = new StreamReader(s))
                {
                    string sid = null;
                    bool foundmapping = false;
                    bool foundWindowsCapabilities = false;
                    string line = sr.ReadLine().Trim();
                    while (line != null)
                    {
                        line = line.Trim();
                        if (line.StartsWith(mappingKey))
                        {
                            foundmapping = true;
                            sid = line.Substring(mappingKey.Length + 1);
                        }
                        else if (foundmapping && line.StartsWith("MappedCapabilities"))
                        {
                            string[] words = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (words.Length > 2)
                            {
                                string name = words[2];
                                map[sid] = name;
                            }
                            foundmapping = false;
                        }
                        else if (line.StartsWith(windowsCapabilityKey))
                        {
                            foundWindowsCapabilities = true;
                        }
                        else if (foundWindowsCapabilities)
                        {
                            if (string.IsNullOrEmpty(line))
                            {
                                // done!
                                foundWindowsCapabilities = false;
                            }
                            else
                            {
                                string[] words = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                if (words.Length > 2)
                                {
                                    sid = words[2];
                                    string name = words[0];
                                    map[sid] = name;
                                }
                            }
                        }

                        line = sr.ReadLine();
                    }
                }
            }
            return;
        }

        public Program()
        {
        }

        void MapWin81(string input)
        {
            LoadWin81();

            using (StreamReader sr = new StreamReader(input))
            {
                bool foundstart = false;
                string line = sr.ReadLine().Trim();
                while (line != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("Capabilities:")) {
                        foundstart = true;
                    }
                    else if (foundstart)
                    {
                        string[] words = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (words.Length > 1)
                        {
                            string sid = words[1];
                            string name = null;
                            if (map.TryGetValue(sid, out name))
                            {
                                Console.WriteLine(name);
                            }
                            else
                            {
                                Console.WriteLine(sid);
                            }
                        }
                    }

                    line = sr.ReadLine();
                }
            }
        }

        Dictionary<string, string> map = new Dictionary<string, string>();
    }
}
