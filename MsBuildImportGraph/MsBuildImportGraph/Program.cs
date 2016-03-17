using Microsoft.VisualStudio.GraphModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MsBuildImportGraph
{
    class Program
    {
        string msBuildFile;
        Graph graph = new Graph();

        Program()
        {
            graph.AddSchema(MsBuildGraphSchema.Schema);
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
            }
            else
            {
                p.Run();
            }
        }

        private void Run()
        {
            //TraverseImports(null, msBuildFile);
            string fullPath = Path.GetFullPath(msBuildFile);

            importStack = new Stack<GraphNode>();
            GraphNode root = graph.Nodes.GetOrCreate(fullPath, Path.GetFileName(fullPath), MsBuildGraphSchema.FileCategory);
            importStack.Push(root);
            currentNode = root;

            XDocument doc = XDocument.Load(fullPath);
            foreach (XNode node in doc.Root.Nodes())
            {
                if (node is XComment)
                {
                    ProcessComment((XComment)node);
                }
            }
            if (!foundImportComments)
            {
                Console.WriteLine("Please provide the output of the msbuild /pp:out.xml command line as the input to this tool");
            }

            string dgmlFile = Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath) + ".dgml");

            graph.Save(dgmlFile);

            Console.WriteLine(dgmlFile);
        }

        Stack<GraphNode> importStack;
        GraphNode currentNode;
        bool foundImportComments;

        private void ProcessComment(XComment comment)
        {
            bool push = false;
            string text = comment.Value;
            foreach (string line in text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("<Import"))
                {
                    foundImportComments = true;
                    push = true;
                }
                else if (trimmed.StartsWith("</Import>"))
                {
                    foundImportComments = true;
                    currentNode = importStack.Pop();
                    return;
                }
                else if (trimmed.Contains("======================="))
                {

                }
                else if (!string.IsNullOrEmpty(trimmed))
                {
                    string fileName = trimmed;
                    if (push)
                    {
                        GraphNode importNode = graph.Nodes.GetOrCreate(fileName, Path.GetFileName(fileName), MsBuildGraphSchema.FileCategory);
                        importStack.Push(importNode);
                        if (currentNode != null)
                        {
                            graph.Links.GetOrCreate(currentNode, importNode);
                        }
                        currentNode = importNode;
                    }
                }
            }
        }

        private void TraverseImports(GraphNode sourceNode, string fileName)
        {
            GraphNode importNode = graph.Nodes.GetOrCreate(fileName, Path.GetFileName(fileName), MsBuildGraphSchema.FileCategory);
            if (sourceNode != null)
            {
                graph.Links.GetOrCreate(sourceNode, importNode);
            }
            if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName))
            {
                importNode.SetValue<string>(MsBuildGraphSchema.ErrorProperty, "File not found: " + fileName);
            }
            else
            {
                XDocument doc = XDocument.Load(fileName);
                XNamespace ns = doc.Root.Name.Namespace;
                foreach (var import in doc.Descendants(ns + "Import"))
                {
                    string project = (string)import.Attribute("Project");
                    string path = ResolveProject(project);
                }
            }
        }

        private string ResolveProject(string project)
        {
            return null;
        }


        private static void PrintUsage()
        {
            Console.WriteLine("Usage: MsBuildImportGraph MSBuildFileName");
            Console.WriteLine("Creates a DGML graph of the imports found in the given MSBUild file");
        }

        bool ParseCommandLine(string[] args)
        {
            for (int i = 0, n = args.Length; i < n; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "h":
                        case "help":
                        case "?":
                            return false;
                        default:
                            Console.WriteLine("Unexpected argument: " + arg);
                            return false;
                    }
                }
                else if (msBuildFile == null)
                {
                    msBuildFile = arg;
                    if (!File.Exists(arg))
                    {
                        Console.WriteLine("### File not found '{0}'", arg);
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("### Too many arguments");
                    return false;
                }
            }
            if (msBuildFile == null)
            {
                Console.WriteLine("### Missing MSBuild file name");
                return false;
            }
            return true;
        }

    }
}
