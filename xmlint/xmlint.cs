//
// xmlint : xml validation tool.
//
// Chris Lovett, September 2001
//
using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Collections;

namespace xmlint
{
    class XmlValidator
    {
        [STAThread]
        static void Main(string[] args)
        {
            ArrayList files = new ArrayList();
            ValidationType validation = ValidationType.Auto;

            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }
            XmlSchemaSet sc = new XmlSchemaSet();
            for (int i = 0; i < args.Length; i++)
            {
                String arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    switch (arg[1])
                    {
                        case 'w':
                            validation = ValidationType.None;
                            break;
                        case 's':
                            if (i + 1 < args.Length)
                            {
                                i++;
                                string xsd = args[i];
                                Console.WriteLine("Adding schema:" + xsd);
                                try
                                {
                                    using (FileStream stream = new FileStream(xsd, FileMode.Open, FileAccess.Read, FileShare.None))
                                    {
                                        XmlSchema s = XmlSchema.Read(stream, null);
                                        sc.Add(s);
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Schema Error: " + e.Message);
                                }
                                validation = ValidationType.Schema;
                            }
                            break;
                        case 't':
                            string vt = arg.Substring(3);
                            try
                            {
                                validation = (ValidationType)Enum.Parse(typeof(ValidationType), vt, true);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Invalid validation type '{0}', expecting a valid System.Xml.ValidationType value", vt);
                            }
                            break;
                        case '?':
                            PrintUsage();
                            return;
                    }
                }
                else
                {
                    string path = Path.GetDirectoryName(args[i]);
                    if (path == "") path = Directory.GetCurrentDirectory();
                    DirectoryInfo di = new DirectoryInfo(path);
                    foreach (FileInfo fi in di.GetFiles(Path.GetFileName(args[i])))
                    {
                        files.Add(fi.FullName);
                    }
                }
            }
            try
            {
                sc.Compile();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Schema doesn't compile: " + ex.Message);
            }

            XmlValidator v = new XmlValidator();
            foreach (string filename in files)
            {
                Console.Write(filename);
                v.Check(filename, validation, sc);
            }
        }

        bool firstError;
        void Check(String filename, ValidationType validation, XmlSchemaSet sc)
        {
            firstError = true;
            try
            {
                using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    if (validation != ValidationType.None)
                    {
                        XmlReaderSettings settings = new XmlReaderSettings();
                        settings.Schemas = sc;
                        settings.ValidationFlags = XmlSchemaValidationFlags.AllowXmlAttributes | XmlSchemaValidationFlags.ProcessIdentityConstraints | XmlSchemaValidationFlags.ProcessInlineSchema | XmlSchemaValidationFlags.ProcessSchemaLocation | XmlSchemaValidationFlags.ReportValidationWarnings;
                        settings.ValidationType = validation;
                        settings.ValidationEventHandler += new ValidationEventHandler(this.ValidationCallback);
                        using (XmlReader reader = XmlReader.Create(stream, settings))
                        {
                            while (reader.Read())
                            {
                                // it validates while we read !
                            }
                        }
                    }
                }
                Console.WriteLine(", ok");
            }
            catch (XmlSchemaException e)
            {
                ReportParseError(e);
            }
            catch (Exception e)
            {
                if (firstError) Console.WriteLine(", failed");
                firstError = false;
                Console.WriteLine("  ### Exception:" + e.Message);
            }
        }

        public void ReportParseError(XmlSchemaException e)
        {
            if (firstError) Console.WriteLine(", failed");
            firstError = false;
            Console.WriteLine("  Error: " + e.Message);
            if (e.SourceUri != null)
            {
                Console.Write("  File:");
                Console.WriteLine(e.SourceUri);
            }
            if (e.LineNumber > 0)
            {
                Console.Write("  Line:");
                Console.Write(e.LineNumber);
                Console.Write(", Pos:");
                Console.WriteLine(e.LinePosition);
            }
        }

        public void ValidationCallback(object sender, ValidationEventArgs args)
        {
            ReportParseError(args.Exception);
        }

        public static void PrintUsage()
        {
            Console.WriteLine("Usage: xmlint [-w] [-s:nsuri,xsdfile] [-t:type] <filename>");
            Console.WriteLine("Reports whether the given xml file is valid.");
            Console.WriteLine("Options:");
            Console.WriteLine("The -w option makes it just report well-formedness");
            Console.WriteLine("The -t specifies the schema type to use:");
            Console.WriteLine("  Types are: Auto, DTD, Schema, XDR (the default is Auto)");
            Console.WriteLine("The -s option specifies a schema associated with the given namespace");
        }

    }
}
