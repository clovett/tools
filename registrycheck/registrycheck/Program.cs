using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace registrycheck
{
    class Program
    {
        bool ascii;
        string path;
        bool verbose;

        readonly Dictionary<string, RegistryKey> hiveMap = new Dictionary<string, RegistryKey>()
        {
            { "HKCR",  Registry.ClassesRoot },
            { "HKEY_CLASSES_ROOT",  Registry.ClassesRoot },
            { "HKLM",  Registry.LocalMachine },
            { "HKEY_LOCAL_MACHINE",  Registry.LocalMachine },
            { "HKCU",  Registry.CurrentUser },
            { "HKEY_CURRENT_USER",  Registry.CurrentUser },
            { "HKEY_USERS",  Registry.Users },
            { "HKCC",  Registry.CurrentConfig },
            { "HKEY_CURRENT_CONFIG",  Registry.CurrentConfig },
        };

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
            Console.WriteLine("Usage: registrycheck [--ascii] path");
            Console.WriteLine("Check that all strings under the given registry path match a certain check.");
            Console.WriteLine("Options:");
            Console.WriteLine("     --ascii     Ensure all strings are in the valid ASCII range.");
        }

        private bool ParseCommandLine(string[] args)
        {
            for (int i = 0, n = args.Length; i < n; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    switch (arg.Trim('-').ToLowerInvariant())
                    {
                        case "h":
                        case "?":
                        case "help":
                            return false;
                        case "ascii":
                            ascii = true;
                            break;
                        case "debug":
                            System.Diagnostics.Debugger.Launch();
                            break;
                        case "verbose":
                            verbose = true;
                            break;
                    }
                }
                else if (path == null)
                {
                    path = arg;
                }
                else
                {
                    Console.WriteLine("### Error: too many path arguments");
                }
            }
            if (path == null)
            {
                Console.WriteLine("### Error: missing registry path argument");
                return false;
            }

            return true;
        }

        private void Run()
        {
            int pos = this.path.IndexOf('\\');
            if (pos > 0)
            {
                var root = this.path.Substring(0, pos);
                if (hiveMap.TryGetValue(root, out RegistryKey hive))
                {
                    var relpath = this.path.Substring(pos + 1);
                    try
                    {
                        using (var key = hive.OpenSubKey(relpath))
                        {
                            Check(key);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: {0}", ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Error: unsupported hive root: {0}", root);
                }
            }
            else
            {
                Console.WriteLine("Error: missing hive root path");
            }
        }

        private void Check(RegistryKey key)
        {
            if (verbose)
            {
                Console.WriteLine(key.Name);
            }

            // Check values of this key
            foreach (var name in key.GetValueNames())
            {
                CheckString(key.Name, "value name", name);
                switch (key.GetValueKind(name))
                {
                    case RegistryValueKind.String:
                        var s = (string)key.GetValue(name);
                        CheckString(key.Name, "value", s);
                        break;
                    case RegistryValueKind.ExpandString:
                        string es = (string)key.GetValue(name);
                        CheckString(key.Name, "value", es);
                        break;
                    case RegistryValueKind.MultiString:
                        var ms = (string[])key.GetValue(name);
                        foreach (var item in ms)
                        {
                            CheckString(key.Name, "value", item);
                        }
                        break;
                    default:
                        break;
                }
            }

            // recurse
            foreach (var name in key.GetSubKeyNames())
            {
                CheckString(key.Name, "key name", name);
                try
                {
                    using (var subkey = key.OpenSubKey(name))
                    {
                        Check(subkey);
                    }
                } 
                catch (System.Security.SecurityException)
                {
                    Console.WriteLine("Error: cannot access {0}", key.Name + "\\" + name);
                }
            }
        }

        private void CheckString(string path, string type, string value)
        {
            if (this.ascii && !string.IsNullOrEmpty(value))
            {
                foreach(var ch in value)
                {
                    int x = Convert.ToInt32(ch);
                    if (x < 0x20 || x > 0x128)
                    {
                        Console.WriteLine("Error: invalid ascii char found in {0} {1}", type, value);
                        Console.WriteLine("Error: found at registry path {0}", path);
                    }
                }
            }
        }
    }
}