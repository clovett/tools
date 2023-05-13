using System;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using System.Net;
using System.Text;
using System.Collections;

namespace dtd2xsd
{
    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    class Test
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Test t = new Test();
            t.Run(args);
            return;
        }

        string proxy = null;
        string input = null;
        string output = null;
        string errorlog = null;
        bool preserveGroups = true;
        string targetNamespace = null;
        bool sgml = false;
        string root = "";
        TextWriter logStream = null;
        XmlTextWriter outStream = null;
        ArrayList imports = new ArrayList(); // SchemaInclude

        Dtd dtd = null;

        void PrintUsage()
        {
            Console.WriteLine("Usage: dtd2xsd <options> [InputUri] [OutputFile]");
            Console.WriteLine("-root              Specifies the root element name, defaults to the first element declaration in the dtd");
            Console.WriteLine("-e                 Optional log file name, name of '$STDERR' will write errors to stderr");
            Console.WriteLine("-proxy             Proxy server to use for http requests");
            Console.WriteLine("-tns               Specifies the target namespace");
            Console.WriteLine("-s prefix ns url   Specifies a schema to add to the list of imported schemas.");
            Console.WriteLine("    prefix - is the namespace prefix to associate with the namespace");
            Console.WriteLine("    ns     - is the namespace URI of the schema to import");
            Console.WriteLine("    url    - is the schema location.");
            Console.WriteLine("-sgml              Parses SGML DTD's");
            Console.WriteLine("-nogroups          Turns off generation of <xsd:group> and <xsd:attributeGroup>");
            Console.WriteLine("InputUri           The input dtd file or http URL (default stdin)");
            Console.WriteLine("OutputFile         Output file name (default stdout)");
        }

        void Run(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg[0] == '-')
                {
                    switch (arg)
                    {
                        case "-e":
                            errorlog = args[++i];
                            break;
                        case "-proxy":
                            proxy = args[++i];
                            break;
                        case "-sgml":
                            sgml = true;
                            break;
                        case "-root":
                            root = args[++i];
                            break;
                        case "-tns":
                            targetNamespace = args[++i];
                            break;
                        case "-nogroups":
                            preserveGroups = false;
                            break;
                        case "-s":
                            if (i >= args.Length - 3)
                            {
                                Console.WriteLine("-s must be followed by prefix, namespace, and schema URI arguments.");
                                PrintUsage();
                                return;
                            }
                            imports.Add(new SchemaInclude(args[++i], args[++i], args[++i])); break;
                        default:
                            PrintUsage();
                            return;
                    }
                }
                else
                {
                    if (input == null) input = arg;
                    else if (output == null) output = arg;
                }
            }
            if (input == null)
            {
                PrintUsage();
                return;
            }

            XmlNameTable nt = new NameTable();
            Uri baseUri;
            if (input.IndexOf("://") > 0)
            {
                baseUri = new Uri(input);
            }
            else
            {
                // probably a local filename.
                baseUri = new Uri("file://" + Directory.GetCurrentDirectory().Replace(Path.DirectorySeparatorChar, '/') + "/");
                input = input.Replace("\\", "/");
            }

            logStream = Console.Error;
            if (errorlog != null)
            {
                logStream = new StreamWriter(errorlog);
            }


            try
            {
                dtd = Dtd.Parse(baseUri, logStream, root, null, input, "", proxy, nt, sgml, preserveGroups);
            }
            catch (Exception e)
            {
                logStream.WriteLine("Error parsing DTD:" + e.Message);
                return;
            }

            XmlSchema s = dtd.GetSchema(targetNamespace, (SchemaInclude[])imports.ToArray(typeof(SchemaInclude)));
            try
            {
                s.Compile(new ValidationEventHandler(OnValidationEvent));
                try
                {
                    if (output != null)
                    {
                        outStream = new XmlTextWriter(output, Encoding.UTF8);
                    }
                    else
                    {
                        outStream = new XmlTextWriter(Console.Out);
                    }
                    outStream.Formatting = Formatting.Indented;
                    try
                    {
                        s.Write(outStream);
                    }
                    catch (Exception e)
                    {
                        logStream.WriteLine("Error writing XSD:" + e.Message);
                    }
                }
                catch (Exception e)
                {
                    logStream.WriteLine("Error opening output file:" + e.Message);
                }
            }
            catch (Exception e)
            {
                logStream.WriteLine("Error compiling XSD:" + e.Message);
            }

            if (outStream != null) outStream.Close();
            if (logStream != null) logStream.Close();

        }

        void OnValidationEvent(Object sender, ValidationEventArgs e)
        {
            logStream.WriteLine("Warning:" + e.Message);
        }
    }
}
