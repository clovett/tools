// ppxml : XML Pretty Printer.

using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Xml;

public class XmlPrettyPrinter 
{
  public static void PrintUsage() {
    Console.WriteLine(@"Usage: ppxml [options] [input] [output]
Pretty prints XML from specified input to specified output.
 -i:num      Specifies indent level
 -c:num      Specifies space char (32=space,9=tab)
 -e:encoding Specifies the encoding to use for output (default UTF8)
 -a          Causes attributes to be formatted each on their own line.
 input       Input file or URL, default is stdin
 output      Output file (cannot be same as input), default is stdout");
  }
  public static int Main(string[] args)
  {
    int indent = 4;
    char indentChar = ' ';
    string input = null;
    string output = null;
    bool formatAttributes = false;
    Encoding encoding = Encoding.UTF8;

    foreach (string arg in args) {
      if (arg[0] == '-' || arg[0] == '/') {
        string a = arg.Substring(1).ToLower();
        if (a.StartsWith("i:")) {
          string value = a.Substring(2);
          try {
            indent = Int32.Parse(value);
          } catch (Exception e) {
            Console.WriteLine("Error parsing indent '{0}' as an integer value\n{1}", value, e.Message);
          }
        } else if (a.StartsWith("c:")) {
          string value = a.Substring(2);
          try {
            indentChar = Convert.ToChar(Int32.Parse(value));
          } catch (Exception e) {
            Console.WriteLine("Error parsing indent character value '{0}' as an valid unicode character\n{1}", value, e.Message);
          }
        } else if (a.StartsWith("e:")) {
          string value = a.Substring(2);
          try {
            encoding = Encoding.GetEncoding(value);
          } catch (Exception e){
            Console.WriteLine("{0}", e.Message);
            encoding = Encoding.UTF8;
          }        
        } else if (a == "a") {
          formatAttributes = true;
        } else {
          PrintUsage();
          return 1;
        }
      } else if (input == null) {
        input = arg;
      } else if (output == null) {
        output = arg;
      } else {
        Console.WriteLine("Ignoring unexpected argument '{1}'", arg);
      }
    }

    XmlTextReader r = null;
    XmlTextWriter w = null;
    StreamWriter sw = null;
    FileStream fs = null;

    try {
      if (input == null) {
        r = new XmlTextReader(Console.In);
      } else {
        r = new XmlTextReader(input);
      }
      if (output == null) {
        if (formatAttributes) 
          w = new AttributeFormattingXmlWriter(Console.Out);
        else
          w = new XmlTextWriter(Console.Out);
      } else {
        fs = new FileStream(output, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        sw = new StreamWriter(fs, encoding);
        if (formatAttributes) 
          w = new AttributeFormattingXmlWriter(sw);
        else
          w = new XmlTextWriter(sw);
        w.WriteStartDocument();
      }
      r.WhitespaceHandling = WhitespaceHandling.None;
      r.Read();
      w.Formatting = Formatting.Indented;
      w.Indentation = indent;
      w.IndentChar = indentChar;

      while (!r.EOF) {
        if (output != null && r.NodeType == XmlNodeType.XmlDeclaration)
          r.Read();

        w.WriteNode(r, true);
      }
    } catch (Exception e) {
      Console.WriteLine("Error: " + e.Message);
    } finally {
      if (r != null && input != null) r.Close();
      if (w != null && output != null) w.Close();
      if (sw != null) sw.Close();
      if (fs != null) fs.Close();
    }

    return 0;
  }

}

