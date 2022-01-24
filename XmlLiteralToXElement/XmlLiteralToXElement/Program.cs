using System.Xml.Linq;

public class Program
{
    public static void Main(string[] args)
    {
        foreach(var arg in args)
        {
            Convert(arg);
        }
    }

    static void Convert(string filename)
    {
        XDocument doc = XDocument.Load(filename);
        PrintCode(doc.Root, "    ", "");
    }

    private static void PrintCode(XObject n, string indent, string connector)
    {
        switch (n.NodeType)
        {
            case System.Xml.XmlNodeType.Element:
                XElement e = n as XElement;
                // special case for elements that contain a single child text node.
                if (!e.Attributes().Any() && e.Nodes().Count() == 1 && e.Nodes().First() is XText)
                {
                    var t = e.Nodes().First() as XText;
                    Console.WriteLine("{0}new XElement(\"{1}\", \"{2}\"){3}", indent, e.Name.LocalName, t.Value.Trim(), connector);
                }
                else
                {
                    Console.WriteLine("{0}new XElement(\"{1}\",", indent, e.Name.LocalName);
                    foreach (var a in e.Attributes())
                    {
                        PrintCode(a, indent + "    ", a == e.Attributes().Last() ? "" : ", ");
                    }
                    foreach (var child in e.Nodes())
                    {
                        PrintCode(child, indent + "    ", child == e.Nodes().Last() ? "" : ", ");
                    }
                    Console.WriteLine(indent + "){0}", connector);
                }
                break;
            case System.Xml.XmlNodeType.Attribute:
                var attr = n as XAttribute;
                Console.WriteLine("{0}new XAttribute(\"{1}\", \"{2}\"){3}", indent, attr.Name.LocalName, attr.Value.Trim(), connector);
                break;
            case System.Xml.XmlNodeType.Text:
                var text = n as XText;
                Console.WriteLine("{0}\"{1}\"{2}", indent, text.Value.Trim(), connector);
                break;
            case System.Xml.XmlNodeType.CDATA:
                var cdata = n as XCData;
                Console.WriteLine("{0}new XCData(\"{1}\"){2}", indent, cdata.Value.Trim(), connector);
                break;
            case System.Xml.XmlNodeType.ProcessingInstruction:
                var pi = n as XProcessingInstruction;
                Console.WriteLine("{0}new XProcessingInstruction(\"{1}\", \"{2}\"){3}", indent, pi.Target, pi.Data, connector);
                break;
            case System.Xml.XmlNodeType.Comment:
                var comment = n as XComment;
                Console.WriteLine("{0}new XComment(\"{1}\"){2}", indent, comment.Value.Trim(), connector);
                break;
            case System.Xml.XmlNodeType.XmlDeclaration:
                break;
            default:
                break;
        }
       
    }
}
