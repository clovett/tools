using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Split
{
    class Program
    {
        static void Main(string[] args)
        {
            TextReader reader = null;
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: join characters [filename]");
                Console.WriteLine("Splits all lines in the given input using the characters provided in first argument");
                return;
            }
            string join = args[0];
            if (args.Length == 1)
            {
                reader = Console.In;
            }
            else
            {
                reader = new StreamReader(args[1]);
            }
            while (true)
            {
                string line = reader.ReadLine();
                if (line == null)
                {
                    break; // eof
                }
                if (!string.IsNullOrWhiteSpace(line))
                {
                    Console.Write(line);
                    Console.Write(join);
                }
            }
            Console.WriteLine();
        }
    }
}
