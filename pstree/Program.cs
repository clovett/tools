using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace pstree
{
    class ProcessNode
    {
        public List<ProcessNode> Children;
        public string Name;
        public string CommandLine;
        public int Id;
        public int ParentId;
        public ProcessNode Parent;

        public void AddChild(ProcessNode node)
        {
            if (Children == null) Children = new List<ProcessNode>();
            Children.Add(node);
        }

        internal void PrintTree(string indent, string match)
        {
            Console.WriteLine("{0}{1}: {2}", indent, Name, CommandLine);
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    if (match == null || child.ContainsMatch(match))
                    {
                        child.PrintTree(indent + "  ", match);
                    }
                }
            }
        }

        internal bool ContainsMatch(string match)
        {
            int pos = this.Name.IndexOf(match, StringComparison.OrdinalIgnoreCase);
            if (pos >= 0) 
                return true;
            if (this.Children != null)
            {
                foreach(var child in this.Children)
                {
                    var m = child.ContainsMatch(match);
                    if (m)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Process p = Process.Start(new ProcessStartInfo()
            {
                Arguments = "-c \"gcim win32_process | select ProcessName,ProcessId,ParentProcessId,CommandLine | out-string -width 8192\"",
                FileName = "powershell.exe",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8
            });
            string output = p.StandardOutput.ReadToEnd();
            string error = p.StandardError.ReadToEnd();

            List<ProcessNode> nodes = new List<ProcessNode>();
            Dictionary<int, ProcessNode> map = new Dictionary<int, ProcessNode>();

            List<string> headers = null;
            List<int> columnPositions = null;
            string[] lines = output.Split('\n');
            foreach(var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (headers == null)
                {
                    string[] cols = line.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    headers = new List<string>(cols);
                    columnPositions = new List<int>();
                    bool ws = true;
                    for(int i = 0; i < line.Trim().Length; i++)
                    {
                        bool isws = Char.IsWhiteSpace(line[i]);
                        if (ws && !isws)
                        {
                            columnPositions.Add(i);
                            ws = false;
                        }
                        else if (isws)
                        {
                            ws = true;
                        }
                    }
                }
                else if (line.StartsWith("----"))
                {
                    continue;
                }
                else
                {
                    ProcessNode node = new ProcessNode();
                    // find the columns.
                    for (int i = 0; i < columnPositions.Count; i++)
                    {
                        var pos = columnPositions[i];
                        string s = (i + 1 < columnPositions.Count) ? line.Substring(pos, columnPositions[i + 1] - pos).Trim() : line.Substring(pos).Trim();
                        switch (headers[i])
                        {
                            case "ProcessName":
                                node.Name = s;
                                break;
                            case "ProcessId":
                                int.TryParse(s, out node.Id);
                                break;
                            case "ParentProcessId":
                                int.TryParse(s, out node.ParentId);
                                break;
                            case "CommandLine":
                                node.CommandLine = s;
                                break;
                        }
                    }
                    nodes.Add(node);
                    map[node.Id] = node;
                }
            }

            // ok, now we have the nodes, make the tree.
            foreach(var n in nodes)
            {
                if (map.TryGetValue(n.ParentId, out ProcessNode parent))
                {
                    parent.AddChild(n);
                    n.Parent = parent;
                }
            }

            string match = args.Length > 0 ? args[0] : null;

            foreach (var n in nodes)
            {
                if (n.Parent == null)
                {
                    if (match == null || n.ContainsMatch(match))
                    {
                        n.PrintTree("", match);
                    }
                }
            }
        }
    }
}
