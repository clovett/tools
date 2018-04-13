using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DesktopBackgroundSlideshow
{

    class Program
    {
        string directory;

        static string ProgramName = "DesktopBackgroundSlideshow";

        static int Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                p.PrintUsage();
                return 1;
            }
            return p.Run();
        }

        string GetSettingsPath()
        {
            string localdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            string path = Path.Combine(localdata, ProgramName);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return Path.Combine(path, "Settings.xml");
        }

        XDocument LoadSettings()
        {
            string path = GetSettingsPath();
            if (File.Exists(path))
            {
                try
                {
                    return XDocument.Load(path);
                }
                catch
                {
                }
            }
            return new XDocument(new XElement("Settings"));
        }

        private void Install()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key != null)
                {
                    string cmdline = key.GetValue(ProgramName) as string;
                    key.SetValue(ProgramName, this.GetType().Assembly.Location);
                }
            }
        }

        private int Run()
        {
            Install();

            XDocument settings = LoadSettings();

            XElement e = settings.Root.Element("Path");
            if (directory == null)
            {
                if (e == null)
                {
                    Console.WriteLine("Please provide the location of the folder containing the images you want to display on your desktop background");
                    return 1;
                }
                directory = e.Value;
            }
            else
            {
                if (e == null)
                {
                    settings.Root.Add(new XElement("Path", directory));
                }
                else
                {
                    e.Value = directory;
                }
            }

            int index = 0;
            e = settings.Root.Element("Index");
            if (e != null)
            {
                int.TryParse(e.Value, out index);
                index++; // next image
                e.Value = index.ToString();
            }
            else
            {
                settings.Root.Add(new XElement("Index", index.ToString()));
            }

            string file = null;
            if (Directory.Exists(directory))
            {
                string[] files = Directory.GetFiles(directory);
                if (index >= files.Length)
                {
                    index = 0; // wrap around!
                }
                file = files[index];

                DesktopBackground.SetFromFile(file);
            }
            else
            {
                Console.WriteLine("directory not found: {0}", directory);
                return 1;
            }

            settings.Save(GetSettingsPath());

            return 0;
        }

        private void PrintUsage()
        {
            Console.WriteLine("Usage: {0} [<dir>]", ProgramName);
            Console.WriteLine("Sets a new picture from the folder each day");
        }

        private bool ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg[0] == '/' || arg[0] == '-')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "h":
                        case "help":
                        case "?":
                            return false;
                        default:
                            break;
                    }
                } else if (directory == null)
                {
                    directory = arg;
                }
                else
                {
                    Console.WriteLine("Too many arguments");
                    return false;
                }
            }
            if (directory != null && !Directory.Exists(directory))
            {
                Console.WriteLine("directory not found");
                return false;
            }
            return true;
        }
    }
}
