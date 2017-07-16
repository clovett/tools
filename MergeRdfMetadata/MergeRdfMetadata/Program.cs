using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MergeRdfMetadata
{
    class Program
    {
        string source;
        string target;

        bool ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg[0] == '/' || arg[0] == '-')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "?":
                        case "h":
                        case "help":
                            return false;
                        default:
                            Console.WriteLine("Unexpected argument: " + arg); ;
                            return false;
                    }
                }
                else if (source == null)
                {
                    source = arg;
                }
                else if (target == null)
                {
                    target = arg;
                }
                else
                {
                    Console.WriteLine("too many arguments");
                    return false;
                }
            }
            if (source == null || target == null)
            {
                return false;
            }
            return true;
        }

        static int Main(string[] args)
        {
            Program p = new MergeRdfMetadata.Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return 1;
            }
            return p.Run();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: MergeRdfMetadata sourceDir targetDir");
        }

        TreeMap src;
        TreeMap dest;

        private int Run()
        {
            string sourceDir = Path.GetFullPath(source);
            string targetDir = Path.GetFullPath(target);

            if (!Directory.Exists(sourceDir))
            {
                Console.WriteLine("Cannot find source directory: " + sourceDir);
                return 1;
            }
            if (!Directory.Exists(targetDir))
            {
                Console.WriteLine("Cannot find source directory: " + sourceDir);
                return 1;
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();
            src = TreeMap.LoadSource(sourceDir);
            watch.Stop();
            Console.WriteLine("Loaded {0} source files in {1} seconds", src.Size(), (double)watch.ElapsedMilliseconds / 1000.0);

            watch.Reset();
            watch.Start();
            dest = TreeMap.LoadSource(targetDir);
            watch.Stop();
            Console.WriteLine("Loaded {0} target files in {1} seconds", dest.Size(), (double)watch.ElapsedMilliseconds / 1000.0);


            src.Merge(dest, (sourceNode, destNode, action) =>
            {
                switch (action)
                {
                    case MergeAction.Exists:
                        OnExists(sourceNode, destNode);
                        break;
                    case MergeAction.Added:
                        OnAdded(destNode);
                        break;
                    case MergeAction.Removed:
                        OnRemoved(sourceNode);
                        break;
                    default:
                        break;
                }
            });

            return 0;
        }

        private void OnExists(TreeMap sourceNode, TreeMap destNode)
        {
            string ext = Path.GetExtension(sourceNode.Path);
            if (ext == ".xmp")
            {
                // check if the two xmp files need to be merged...
                string srcText = File.ReadAllText(sourceNode.Path);
                string destText = File.ReadAllText(destNode.Path);
                if (srcText != destText)
                {
                    Console.WriteLine("windiff \"{0}\" \"{1}\"", sourceNode.Path, destNode.Path);
                }
            }
        }

        private void OnRemoved(TreeMap sourceNode)
        {
            string ext = Path.GetExtension(sourceNode.Path);
            if (ext == ".xmp")
            {
                string rel = sourceNode.GetRelativePath();
                string dest = System.IO.Path.Combine(this.dest.Path, rel);
                foreach (var match in System.IO.Directory.GetFiles(Path.GetDirectoryName(dest), Path.GetFileNameWithoutExtension(dest) + ".*"))
                {
                    if (Path.GetExtension(match).ToLowerInvariant() != ".xmp") {
                        // there is a photo matching this name, so copy the XMP over
                        Console.WriteLine("Copying missing mestadata: " + dest);
                        File.Copy(sourceNode.Path, dest);
                        break;
                    }
                }
                return;
            }
        }

        private void OnAdded(TreeMap destNode)
        {
            // new files in destination are fine, nothing to do here.
        }

    }
}
