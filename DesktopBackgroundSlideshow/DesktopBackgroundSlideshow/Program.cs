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

        Settings LoadSettings()
        {
            string path = GetSettingsPath();
            return Settings.Load(path);
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

        private void Shuffle(List<string> values, Random r)
        {
            List<string> result = new List<string>();
            while(values.Count > 0)
            {
                int next = r.Next(values.Count);
                result.Add(values[next]);
                values.RemoveAt(next);
            }
            values.AddRange(result);
        }

        private bool IsPicture(string filename)
        {
            string ext = System.IO.Path.GetExtension(filename);
            if (string.Compare(ext, ".png", true) == 0 ||
                string.Compare(ext, ".jpg", true) == 0 ||
                string.Compare(ext, ".gif", true) == 0 ||
                string.Compare(ext, ".bmp", true) == 0)
            {
                // todo: see if we have permission to read this file.
                return true;
            }
            return false;
        }

        private int Run()
        {
            Install();

            Settings settings = LoadSettings();
            
            if (directory == null)
            {
                if (string.IsNullOrEmpty(settings.Path))
                {
                    Console.WriteLine("Please provide the location of the folder containing the images you want to display on your desktop background");
                    return 1;
                }
                directory = settings.Path;
            }
            else
            {
                settings.Path = directory;
            }

            int seed = settings.Seed;
            if (seed == 0)
            {
                seed = new Random(Environment.TickCount).Next();
                settings.Seed = seed;
            }

            var rand = new Random(seed);
            int index = settings.Index;

            string file = null;
            if (Directory.Exists(directory))
            {
                List<string> files = new List<string>(Directory.GetFiles(directory));
                for (int i = files.Count - 1; i >= 0; i--)
                {
                    string item = files[i];
                    if (!IsPicture(item))
                    {
                        files.RemoveAt(i);
                    }
                }
                Shuffle(files, rand);

                if (index >= files.Count)
                {
                    index = 0; // wrap around!
                }
                file = files[index];
                settings.Index++;

                if (!DesktopBackground.SetFromFile(file))
                {
                    int hr = DesktopBackground.NativeMethods.GetLastError();
                    Console.WriteLine("Error {0} with SET_DESKTOP_BACKGROUND", hr);
                }
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
