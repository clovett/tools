
using System;
using System.IO;
using System.Xml.Linq;

namespace version
{
    class Program
    {
        const string versionFile = "Common\\version.props";

        static void Main(string[] args)
        {
            string version = null;
            if (args.Length > 0)
            {
                version = args[0];
            }

            if (File.Exists(versionFile))
            {
                XDocument doc = XDocument.Load(versionFile);
                XElement prefix = doc.Root.Element("PropertyGroup").Element("VersionPrefix");
                if (version == null)
                {
                    Console.WriteLine("Current version is: {0}", prefix.Value);
                }
                else if (version != prefix.Value)
                {
                    Console.WriteLine("Updating version to: {0}", version);
                    prefix.SetValue(version);
                    doc.Save(versionFile);
                }
                else
                {
                    Console.WriteLine("Version is already set to {0}", prefix.Value);
                }
            } 
            else
            {
                Console.WriteLine("File not found: {0}", versionFile);
            }
        }
    }
}
