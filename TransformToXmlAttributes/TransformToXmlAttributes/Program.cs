using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TransformToXmlAttributes
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            string path = args[0];
            string parent = null;
            if (args.Length == 2)
            {
                parent = args[1];
            }
            string dir = System.IO.Path.GetDirectoryName(path);
            string pattern = System.IO.Path.GetFileName(path);
            foreach (string name in System.IO.Directory.GetFiles(dir, pattern))
            {
                Console.Write("Transforming " + name + "...");
                XDocument doc = XDocument.Load(name);
                Transform(doc.Root, parent);
                doc.Save(name);
                Console.WriteLine("done");
            }

            return;
        }

        private static bool Transform(XElement e, string parent)
        {
            bool result = false;
            bool transformThisElement = (parent == null || parent == e.Name.LocalName);
            foreach (var child in e.Elements().ToArray())
            {
                if (!Transform(child, parent) && transformThisElement)
                {
                    e.SetAttributeValue(child.Name.LocalName, child.Value);
                    child.Remove();
                }
                result = true;
            }
            return result;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: TransformToXmlAttributes filename [ParentName]");
            Console.WriteLine("Converts all level valued XML elements to XML attributes");
            Console.WriteLine("Filename supports wildcards.  If parent name is provided ");
            Console.WriteLine("then it will only convert child elements inside that parent");
        }
    }
}
