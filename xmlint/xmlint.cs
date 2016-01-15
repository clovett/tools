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
using System.Collections.Generic;
using System.Text;

namespace xmlint
{
    class XmlValidator
    {
        List<string> files = new List<string>();
        ValidationType validation = ValidationType.Auto;
        XmlSchemaSet sc = new XmlSchemaSet();
        Encoding encoding;
        private bool nobom;
        private bool reformat;

        static void Main(string[] args)
        {
            XmlValidator v = new XmlValidator();
            if (!v.ParseCommandLine(args))
            {
                PrintUsage();
                return;
            }
            v.Run();
            return;
        }

        bool ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                String arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "w":
                            validation = ValidationType.None;
                            break;
                        case "s":
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
                                    return false;
                                }
                                validation = ValidationType.Schema;
                            }
                            break;
                        case "t":
                            string vt = arg.Substring(3);
                            try
                            {
                                validation = (ValidationType)Enum.Parse(typeof(ValidationType), vt, true);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Invalid validation type '{0}', expecting a valid System.Xml.ValidationType value", vt);
                                return false;
                            }
                            break;
                        case "?":
                        case "h":
                        case "help":
                            return false;
                        case "e":
                            if (i + 1 < args.Length)
                            {
                                string e = args[++i];
                                try
                                {
                                    this.encoding = Encoding.GetEncoding(e);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Invalid encoding '{0}': {1}", e, ex.Message);
                                    return false;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Missing encoding name after -e optoin");
                                return false;
                            }
                            break;
                        case "f":
                            this.reformat = true;
                            break;
                        case "nobom":
                            this.nobom = true;
                            break;
                    }
                }
                else
                {
                    string path = Path.GetDirectoryName(args[i]);
                    if (string.IsNullOrEmpty(path)) path = Directory.GetCurrentDirectory();
                    DirectoryInfo di = new DirectoryInfo(path);
                    foreach (FileInfo fi in di.GetFiles(Path.GetFileName(args[i])))
                    {
                        files.Add(fi.FullName);
                    }
                }
            }
            if (files.Count == 0)
            {
                Console.WriteLine("Must specify at least one input file.");
                return false;
            }

            try
            {
                sc.Compile();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Schema doesn't compile: " + ex.Message);
                return false;
            }

            return true;
        }

        void Run()
        {
            foreach (string filename in files)
            {
                Check(filename, validation, sc);
            }
        }

        bool firstError;

        void Check(String filename, ValidationType validation, XmlSchemaSet sc)
        {
            Console.Write(filename);
            firstError = true;
            try
            {
                Encoding xmlEncoding = null;
                int codepage = 0;

                using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    codepage = ReadBom(stream);

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
                                if (reader.NodeType == XmlNodeType.XmlDeclaration)
                                {
                                    xmlEncoding = Encoding.GetEncoding(reader.GetAttribute("encoding"));
                                }
                            }
                        }
                    }
                }

                bool write = this.reformat;
                if (xmlEncoding == null)
                {                    
                    xmlEncoding = codepage == 0 ? Encoding.UTF8 : Encoding.GetEncoding(codepage);
                }

                if (this.encoding != null && xmlEncoding != this.encoding)
                {
                    Console.Write(", re-encoding");
                    write = true;
                }
                else if (this.reformat)
                {
                    Console.Write(", reformatting");
                    write = true;
                }
                else if (nobom && codepage != 0)
                {
                    Console.Write(", removing bom");
                    write = true;
                }

                if (write)
                {
                    XmlDocument doc = new XmlDocument();
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.IgnoreWhitespace = false;
                    using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        using (XmlReader reader = XmlReader.Create(stream, settings))
                        {
                            doc.PreserveWhitespace = !this.reformat;
                            doc.Load(reader);
                        }
                    }

                    XmlWriterSettings ws = new XmlWriterSettings();
                    ws.Encoding = this.encoding != null ? this.encoding : xmlEncoding;
                    ws.Indent = this.reformat;

                    MemoryStream ms = new MemoryStream();

                    using (XmlWriter writer = XmlWriter.Create(ms, ws))
                    {
                        doc.Save(writer);
                    }
                    ms.Seek(0, SeekOrigin.Begin);

                    if (nobom)
                    {
                        int cp = ReadBom(ms);                        
                        if (cp == 65001)
                        {
                            ms.Seek(3, SeekOrigin.Begin);
                        }
                        else if (cp == 1200 || cp == 1201)
                        {
                            ms.Seek(2, SeekOrigin.Begin);
                        }
                    }

                    using (Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                    {
                        ms.CopyTo(stream);
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

        private static int ReadBom(Stream stream)
        {
            int codepage = 0;
            byte[] buffer = new byte[3];
            int read = stream.Read(buffer, 0, 3);
            if (read == 3 && buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
            {
                codepage = 65001;
            }
            else if (read >= 2 && buffer[0] == 0xff && buffer[1] == 0xfe)
            {
                codepage = 1200;
            }
            else if (read >= 2 && buffer[0] == 0xfe && buffer[1] == 0xff)
            {
                codepage = 1201;
            }
            stream.Seek(0, SeekOrigin.Begin);
            return codepage;
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
            Console.WriteLine("  -w      makes it just report well-formedness");
            Console.WriteLine("  -t      specifies the schema type to use:");
            Console.WriteLine("          Types are: Auto, DTD, Schema, XDR (the default is Auto)");
            Console.WriteLine("  -s      specifies a schema associated with the given namespace");
            Console.WriteLine("  -f      reformats the given file so it is easier to read");
            Console.WriteLine("  -e name re-encodes the given file using the given encoding (e.g. -e utf-8)");
            Console.WriteLine("  -nobom  removes the byte order mark at the beginning of the file");
        }

    }
}
