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
                Console.WriteLine("Usage: split character [filename]");
                Console.WriteLine("Splits all lines in the given input using the characters provided in first argument");
                return;
            }
            char[] splitChars = args[0].ToCharArray();
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
                foreach (var part in line.Split(splitChars))
                {
                    Console.WriteLine(part);
                }
            }
        }
    }
}
