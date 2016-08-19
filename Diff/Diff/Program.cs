using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiffMatchPatch;

namespace Diff
{
    class Program
    {
        static int Main(string[] args)
        {
            // normalize newlines
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: diff file1 file2");
                return 1;
            }

            string[] left = File.ReadAllLines(args[0]);
            string[] right = File.ReadAllLines(args[1]);

            int count = 0;
            diff_match_patch diff = new diff_match_patch();
            foreach (var result in diff.diff_main(string.Join("\n", left), string.Join("\n", right)))
            {
                if (result.operation != Operation.EQUAL)
                {
                    //Console.WriteLine(result.operation + ": " + result.text);
                    count++;
                }
            }

            Console.WriteLine("Found {0} differences", count);
            return count;
        }

    }
}
