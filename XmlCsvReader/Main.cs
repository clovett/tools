/*
* 
* Test the XmlCsvReader class.
*
* Copyright (c) 2001 Microsoft Corporation. All rights reserved.
*
* Chris Lovett
* 
*/

using System;
using System.Xml;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Collections;

namespace Microsoft.Xml {
    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    class CommandLine {
        static void PrintUsage() {
            Console.WriteLine("Usage: XmlCsvReader [-a] [-c] [-d char] [-root name] [-row name] [-names a,b,c] [-e encoding] [customer.csv] [result.xml]");
            Console.WriteLine(" -a specifies that you want attributes instead of subelements.");
            Console.WriteLine(" -c specifies that the first row contains column names.");
            Console.WriteLine(" -d specifies the actual delimiter to expect (rather than trying to auto-detect it)");
            Console.WriteLine(" -e specifies an override for the encoding used to decode the .csv file.  Default is Encoding.Default");
            Console.WriteLine(" -root specifies the root element name (default 'root')");
            Console.WriteLine(" -row specifies the row element name (default 'row')");
            Console.WriteLine(" -names specifies the actual column names to use, separated by commas");
            Console.WriteLine("Default input is stdin, default output is stdout");
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            string infile = null;
            string outfile = null;
            bool useDoc = false;
            bool test = false;
            Encoding encoding = Encoding.Default;
            XmlCsvReader reader = new XmlCsvReader();

            for (int i = 0; i < args.Length; i++) {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/') {
                    arg = arg.Substring(1);
                    switch (arg) {
                        case "a":
                            reader.ColumnsAsAttributes = true;
                            break;
                        case "c":
                            reader.FirstRowHasColumnNames = true;
                            break;
                        case "d":
                            reader.Delimiter = ParseDelimiter(args[++i]);
                            break;
                        case "e":
                            encoding = Encoding.GetEncoding(args[++i]);
                            break;
                        case "doc":
                            useDoc = true;
                            break;
                        case "root":
                            reader.RootName = args[++i];
                            break;
                        case "row":
                            reader.RowName = args[++i];
                            break;
                        case "names":
                            string names = args[++i].Replace("\"","");
                            reader.ColumnNames = names.Split(new char[2] { ' ', ',' });
                            break;
                        case "test":
                            test = true;
                            break;
                        case "?":
                        case "help":
                            PrintUsage();
                            return;
                        default:
                            Console.WriteLine("### Unrecognized command line option: " + arg);
                            PrintUsage();
                            return;
                    }
                } 
                else {
                    if (infile == null) infile = arg;
                    else outfile = arg;
                }
            }

            // Create valid URI from the infile argument.  It could be a full URL
            // already or it could be a file name.
            if (infile != null) {
                reader.Href = infile;
                reader.Encoding = encoding;
            } 
            else {
                string path = "file://" + Application.StartupPath.Replace("\\","/") + "/";
                reader.BaseUri = path;
                reader.TextReader = Console.In;
            }
            TextWriter output = Console.Out;
            if (outfile != null) {
                try {
                    output = new StreamWriter(outfile, false, Encoding.UTF8);
                } 
                catch (Exception e) {
                    Console.WriteLine("###Error opening output file: " + e.Message);
                    return;
                }
            }

            if (test) {
                TestSuite ts = new TestSuite();
                ts.RunTest(infile);
                return;
            }

            XmlTextWriter w = new XmlTextWriter(output);
            w.Formatting = Formatting.Indented;

            if (useDoc) {
                XmlDocument doc = new XmlDocument();
                doc.Load(reader);
                doc.WriteTo(w);
            } else {
                while (!reader.EOF) {
                    w.WriteNode(reader, false);
                }
            }
            w.Close();
        }
        static char ParseDelimiter(string arg) {
            if (arg == "auto" || arg == "") {
                return '\0';
            }
            if (arg.Length > 1 && arg[0] == '\\') {
                if (arg[1] == 't') return '\t';
                Console.WriteLine("Invalid column delimiter '{0}'", arg);
                return '\0';
            }
            if (arg.Length > 1 || arg[0] == '\r' || arg[0] == '\n') {
                Console.WriteLine("Invalid column delimiter '{0}'", arg);
                return '\0';
            }
            return arg[0];
        }
    }

    class TestSuite {
        int tests;
        int passed;

        /**************************************************************************
         * Run a test suite.  Tests suites are organized into expected input/output
         * blocks separated by back quotes (`).  It runs the input and compares it
         * with the expected output and reports any failures.
         **************************************************************************/
        public void RunTest(string file) {
            Console.WriteLine(file);
            StreamReader sr = new StreamReader(file);
            StringBuilder input = new StringBuilder();
            StringBuilder expectedOutput = new StringBuilder();
            StringBuilder current = input;
            StringBuilder args = new StringBuilder();
            
            int start = 1;
            int line = 1;
            int pos = 1;
            bool readArgs = false;          
            int i;
            do {
                i = sr.Read();
                char ch = (char)i;
                if ((pos == 1 && ch == '`') || i == -1) {
                    if (input.ToString().Trim().Length == 0 ) {
                        // allow multiple '`' separators each with different args
                    } else if (current == input) {
                        current = expectedOutput;
                        current.Length = 0;
                    } else {
                        RunTest(args.ToString().Trim(), input.ToString(), start, expectedOutput.ToString());
                        input.Length = 0;
                        expectedOutput.Length = 0;
                        args.Length = 0;
                        current = input;
                        start = line;
                    }
                    readArgs = true;
                } else {
                    if (readArgs) args.Append(ch);
                    else if (current != null) current.Append(ch);
                    if (ch == '\r') {
                        line++; pos = 1;
                        if (sr.Peek() == '\n') {
                            i = sr.Read();
                            char c = (char)i;
                            if (!readArgs) {
                                current.Append(c);                            
                            }
                        }
                        readArgs = false;
                    } else if (ch == '\n'){
                        readArgs = false;
                        line++; pos = 1;
                    }
                }
            } while (i != -1);

            if (input.Length>0 && expectedOutput.Length>0) {
               RunTest(args.ToString(), input.ToString(), start, expectedOutput.ToString());
            }

            Console.WriteLine("Tests passed: " + this.passed + " of " + this.tests);
        }



        void RunTest(string args, string input, int line, string expectedOutput){
            this.tests++;

            XmlCsvReader reader = new XmlCsvReader();
            reader.TextReader = new StringReader(input.ToString());
            ParseArgs(reader, args);

            StringWriter output = new StringWriter();
            XmlTextWriter w = new XmlTextWriter(output);          
            w.Formatting = Formatting.Indented;
            reader.Read();
            while (!reader.EOF) {
                w.WriteNode(reader, true);
            }
            w.Close();
            string actualOutput = output.ToString();
            if (actualOutput.Trim() != expectedOutput.Trim()) {
                Console.WriteLine("ERROR: Test failed on line {0}", line);
                Console.WriteLine("---- Expected output");
                Console.WriteLine(expectedOutput);
                Console.WriteLine("---- Actual output");
                Console.WriteLine(actualOutput);
            } else {
                this.passed++;
            }
        }

        void ParseArgs(XmlCsvReader reader, string text){
            ArrayList args = new ArrayList();
            int start = 0;
            int i = 0;
            for (int n = text.Length; i<n; i++){
                char c = text[i];
                if (Char.IsWhiteSpace(c)){
                    if (start < i) {
                        string arg = text.Substring(start, i-start).Trim();
                        args.Add(arg);                  
                    }
                    start = i;
                } else if (start+1 == i && c == '"' || c == '\''){
                    i++;
                    int j = i;
                    while (j<n && text[j] != c){
                        j++;
                    }
                    string arg = text.Substring(i, j-i).Trim();
                    args.Add(arg);
                    start = i;
                }
            }            
            if (start < i) {
                string arg = text.Substring(start, i-start).Trim();
                args.Add(arg);
            }
 
            for (i = 0; i < args.Count; i++) {
                string arg = (string)args[i];
                if (arg.Length==0) continue;
                if (arg[0] == '-' || arg[0] == '/') {
                    arg = arg.Substring(1);
                    switch (arg) {
                        case "a":
                            reader.ColumnsAsAttributes = true;
                            break;
                        case "c":
                            reader.FirstRowHasColumnNames = true;
                            break;
                        case "root":
                            reader.RootName = (string)args[++i];
                            break;
                        case "row":
                            reader.RowName = (string)args[++i];
                            break;
                        case "names":
                            string names = ((string)args[++i]).Trim();
                            reader.ColumnNames = names.Split(new char[2] { ' ', ',' });
                            break;
                    }
                }                 
            }
        }
    }    
}
