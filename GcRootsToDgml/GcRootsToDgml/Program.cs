using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.GraphModel;

namespace GcRootsToDgml
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var arg in args)
            {
                if (System.IO.File.Exists(arg))
                {
                    GcRootsDgmlConverter p = new GcRootsDgmlConverter();
                    p.Convert(arg);
                }
            }
        }
    }

    class GcRootsDgmlConverter
    {
        Graph graph = new Graph();

        public void Convert(string filename)
        {
            GraphNode caller = null;
            using (var reader = new StreamReader(filename))
            {
                int line_number = 0;
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }
                    line_number++;
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line))
                    {
                        caller = null; // starting new chain.
                    }
                    else if (line.StartsWith("->"))
                    {
                        int pos = SkipWhitespace(line, 2);
                        string address = ReadToken(line, ref pos);
                        pos = SkipWhitespace(line, pos);
                        var name = line.Substring(pos);

                        var node = GetOrCreate(address, name);
                        if (caller != null)
                        {
                            graph.Links.GetOrCreate(caller, node);
                        }
                        caller = node;
                    }
                }
            }

            var output = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".dgml");
            Console.WriteLine("Saving: " + output);
            graph.Save(output);
        }

        private string ReadToken(string line, ref int pos)
        {
            int start = pos;
            while (!Char.IsWhiteSpace(line[pos]))
            {
                pos++;
            }
            return line.Substring(start, pos - start);
        }

        private int SkipWhitespace(string line, int pos)
        {
            while (Char.IsWhiteSpace(line[pos]))
            {
                pos++;
            }
            return pos;
        }

        NameParser parser = new NameParser();

        private GraphNode GetOrCreate(string id, string mangledName)
        {
            string name = parser.Parse(mangledName);

            // var name = Unmangle(mangledName);
            //string[] classes = name.Split('+');
            //if (classes.Length > 1)
            //{
            //    GraphNode parent = null;
            //    // we have a nested class
            //    foreach (var c in classes)
            //    {
            //        var node = graph.Nodes.GetOrCreate(id, c, null);
            //        if (parent != null)
            //        {
            //            // add containment link
            //            parent[GraphCommonSchema.Group] = GraphGroupStyle.Expanded;
            //            graph.Links.GetOrCreate(parent, node, null, GraphCommonSchema.Contains);
            //        }
            //        parent = node;
            //    }
            //    return parent;
            //}
            //else
            //{
                return graph.Nodes.GetOrCreate(id, name, null);
            //}
        }

        private class NameParser
        {
            /*
             Turns something like this:
                    System.Collections.Concurrent.ConcurrentDictionary`2+Tables[[System.Type, System.Private.CoreLib],
                        [System.Collections.Generic.Dictionary`2[[System.Type, System.Private.CoreLib],[System.Reflection.MethodInfo, System.Private.CoreLib]], System.Private.CoreLib]]

             into this:      ConcurrentDictionary<Type, Dictionary<Type, System.Reflection.MethodInfo>>

             Notice the generic types can be nested in generic parameters, so this parse method is recursive.
            */

            string buffer;
            int pos;
            StringBuilder unmangled = new StringBuilder();
            StringBuilder token = new StringBuilder();

            public NameParser()
            {
            }

            char GetChar()
            {
                return buffer[pos++];
            }

            bool EOF => pos >= buffer.Length;

            public string Parse(string managledName)
            {
                buffer = managledName;
                pos = 0;
                unmangled.Length = 0;
                while (!EOF)
                {
                    char ch = GetChar();
                    if (ch == '`')
                    {
                        // beginning of a generic
                        pos--;
                        var typeParams = UnmangleTypeParams();
                        string name = Simplify(unmangled.ToString());
                        return name + typeParams;
                    }
                    else if (ch == ']')
                    {
                        // end of generic?
                    }
                    else
                    {
                        unmangled.Append(ch);
                    }
                }

                return Simplify(unmangled.ToString());
            }

            string UnmangleTypeParams()
            {
                List<string> names = new List<string>();
                // Converts "`2[[System.Type, System.Private.CoreLib],[System.Reflection.MethodInfo, System.Private.CoreLib]]" into a list containing
                // the simplified names "System.Type" and "System.Reflection.MethodInfo".

                string prefix;
                char ch;
                (prefix, ch) = ParseToken("[");

                // the prefix be a simple number, or it could include a nested generic like this "`3+Nested`2" meaning we have 3 type parameters
                // on the outer class, and 2 more on a nested class, so we expect a total of 5 type parameters in this case.

                while (ch == '[' || ch == ',')
                {
                    // this is the beginning of the list of qualified type name pairs.
                    ch = GetChar();
                    if (ch == '[')
                    {
                        
                        // this is beginning of one qualified type name pair.
                        string name;
                        (name, ch) = ParseToken(",`");
                        name = Simplify(name);
                        if (ch == '`')
                        {
                            // ooh, we have a nested generic type here!
                            pos--;
                            name += UnmangleTypeParams();
                            ch = GetChar(); // should be a comma here.
                        }
                        names.Add(name);

                        string assembly;
                        (assembly, ch) = ParseToken(",]");
                    }
                    else
                    {
                        throw new Exception("Badly formed name?");
                    }

                    // consume the outer ']'
                    ch = GetChar(); 
                }

                // ok, now build the unmangled type name using the parse type parameters
                token.Length = 0;
                var parts = prefix.Split('+');
                int index = 0;
                foreach (var item in parts)
                {
                    if (token.Length != 0)
                    {
                        token.Append("+"); // nested type
                    }
                    int k = item.IndexOf('`');
                    if (k >= 0)
                    {
                        // this is a generic and so it takes some of the type parameter names.
                        token.Append(item.Substring(0, k));
                        token.Append("<");
                        int count = int.Parse(item.Substring(k + 1));
                        for (int i = 0; i < count && index < names.Count; i++)
                        {
                            if (i > 0)
                            {
                                token.Append(",");
                            }

                            token.Append(names[index]);
                            index++;
                        }
                        token.Append(">");
                    }
                }

                return token.ToString();
            }

            private (string, char) ParseToken(string terminators)
            {
                token.Length = 0;
                char ch = GetChar();
                bool started = false;
                while (terminators.IndexOf(ch) == -1)
                {
                    if (started || !Char.IsWhiteSpace(ch))
                    {
                        token.Append(ch);
                        started = true;
                    }
                    ch = GetChar();
                }
                return (token.ToString(), ch);
            }

            private string Simplify(string typeInfo)
            {
                var parts = typeInfo.Split(',');
                string t = parts[0];
                switch (t)
                {
                    case "System.Char":
                        return "char";
                    case "System.Byte":
                        return "byte";
                    case "System.SByte":
                        return "sbyte";
                    case "System.Boolean":
                        return "bool";
                    case "System.Int16":
                        return "short";
                    case "System.UInt16":
                        return "ushort";
                    case "System.Int32":
                        return "int";
                    case "System.UInt32":
                        return "uint";
                    case "System.UInt64":
                        return "ulong";
                    case "System.Int64":
                        return "long";
                    case "System.Single":
                        return "float";
                    case "System.Double":
                        return "double";
                    case "System.DateTime":
                        return "DateTime";
                    case "System.TimeSpan":
                        return "TimeSpan";
                    case "System.Void":
                        return "void";
                    case "System.String":
                        return "string";
                    case "System.Type":
                        return "Type";
                    case "System.Collections.Generic.Dictionary":
                        return "Dictionary";
                    case "System.Collections.Generic.List":
                        return "List";
                    case "System.Collections.Generic.Queue":
                        return "Queue";
                    case "System.Reflection.MethodInfo":
                        return "MethodInfo";
                    case "System.Reflection.PropertyInfo":
                        return "PropertyInfo";
                    case "System.Reflection.Assembly":
                        return "Assembly";
                    case "System.Reflection.TypeInfo":
                        return "TypeInfo";
                    default:
                        return t;
                }
            }

        }

    }
}
