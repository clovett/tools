using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diff
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] left = File.ReadAllLines(args[0]);
            string[] right = File.ReadAllLines(args[1]);

            HashSet<string> leftSet = new HashSet<string>();
            HashSet<string> rightSet = new HashSet<string>();


            foreach (string line in left)
            {
                string s = GetProcessName(line);
                leftSet.Add(s);
            }

            foreach (string line in right)
            {
                string s = GetProcessName(line);
                rightSet.Add(s);
            }

            HashSet<string> leftOnly = new HashSet<string>(leftSet);
            leftOnly.ExceptWith(rightSet);

            HashSet<string> rightOnly = new HashSet<string>(rightSet);
            rightOnly.ExceptWith(leftSet);

            // ok, now we need to print out these results with colors showing
            // red for leftOnly items, green for rightOnly items and normal for
            // "both" items.

            List<ChangeLine> result = new List<ChangeLine>();


            foreach (string line in left)
            {
                string s = GetProcessName(line);

                ChangeLine cl = new ChangeLine() { Text = s };

                if (leftOnly.Contains(s))
                {
                    cl.deleted = true;
                }
                result.Add(cl);
            }

            int pos = 0;
            foreach (string line in right)
            {
                string s = GetProcessName(line);

                if (rightOnly.Contains(s))
                {
                    ChangeLine cl = new ChangeLine() { Text = s };
                    cl.inserted = true;
                    result.Insert(pos, cl);
                }
                pos++;
            }

            WriteHeader();

            foreach (var change in result)
            {
                string className = "normal";
                if (change.deleted)
                {
                    className = "deleted";
                }
                else if (change.inserted) 
                {
                    className = "inserted";
                }
                WriteLine(change.Text, className);
            }

            WriteFooter();

        }

        static char[] whitespace = new char[] { ' ', '\t' };

        private static string GetProcessName(string line)
        {
            string trimmed = line.Trim();
            string[] parts = trimmed.Split(whitespace, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                return parts[1];
            }
            return trimmed;
        }

        private static void WriteHeader()
        {
            Console.WriteLine("<html>");
            Console.WriteLine("<style type='text/css'>");
            Console.WriteLine(".deleted { background: red; }");
            Console.WriteLine(".inserted { background: yellow; }");
            Console.WriteLine("</style>");
            Console.WriteLine("<body>");

        }
        private static void WriteLine(string text, string className)
        {
            Console.WriteLine("<span class='{0}'>{1}</span><br/>", className, text);
        }

        private static void WriteFooter()
        {
            Console.WriteLine("</body>");
            Console.WriteLine("</html>");
        }


        class ChangeLine
        {
            public string Text;
            public bool deleted;
            public bool inserted;
        }
    }
}
