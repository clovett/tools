using System;
using Walkabout.Utilities;

namespace AzureRmFolder
{
    internal class Program
    {
        static void PrintUsage()
        {
            Console.WriteLine("Usage: AzureRmFolder [--preview] path connectionString");
            Console.WriteLine("Removes all files and directories from the specified path");
            Console.WriteLine("in the Azure Storage account specified by the given connection string.");
            Console.WriteLine("Use the '--preview' option to see what would be deleted but not actually do it.");
            Console.WriteLine();
        }

        static int Main(string[] args)
        {
            string path = null;
            string connectionString = null;
            bool preview = false;

            foreach (string arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    switch (arg.Trim('-').ToLowerInvariant())
                    {
                        case "preview":
                            preview = true;
                            break;
                        case "help":
                            PrintUsage();
                            return 1;
                        default:
                            Console.WriteLine("Invalid argumenty '{0}'", arg);
                            PrintUsage();
                            return 1;
                    }
                }
                else if (path == null)
                {
                    path = arg;
                }
                else if (connectionString == null)
                {
                    connectionString = arg;
                }
                else
                {
                    Console.WriteLine("Too many arguments");
                    PrintUsage();
                    return 1;
                }
            }

            if (string.IsNullOrEmpty(path))
            {
                Console.WriteLine("Missing 'folder' argument");
                PrintUsage();
                return 1;
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Missing 'connectionString' argument");
                PrintUsage();
                return 1;
            }

            var folder = new Folder(path, connectionString);
            folder.DeleteSubtree(preview);

            return 0;
        }
    }
}
