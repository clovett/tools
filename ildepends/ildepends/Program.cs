using Microsoft.Cci;
using Microsoft.Cci.MutableContracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Styles;

namespace ildepends
{
    class Program
    {
        static GraphCategory UnresolvedCategory;
        static GraphCategory AssemblyCategory;
        static ConsoleHostEnvironment host;

        List<string> assemblies = new List<string>();
        uint levels = int.MaxValue;
        string outputFileName = "out.dgml";
        bool verbose = false;

        static void Main(string[] args)
        {
            Program p = new Program();
            if (p.ParseCommandLine(args))
            {
                try
                {
                    p.Run();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("### Unexpected exception: {0}", ex.Message);
                    Console.WriteLine(ex.ToString());
                }
            }
            else
            {
                PrintUsage();
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("ildepends [options] assemblies");
            Console.WriteLine("Loads all the given .NET assemblies, walks their referenced assemblies and dumps out DGML graph of the result");
            Console.WriteLine("Options:");
            Console.WriteLine("    /out:filename.dgml       (default out.dgml)");
            Console.WriteLine("    /levels:n                (how many levels of references to traverse, default int.MaxValue)");
        }

        bool ParseCommandLine(string[] args)
        {
            for (int i = 0, n = args.Length; i < n; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    int colon = arg.IndexOf(':');
                    string value = null;
                    if (colon > 0 && colon < arg.Length - 1)
                    {
                        value = arg.Substring(colon + 1);
                        arg = arg.Substring(0, colon);
                    }

                    switch (arg.Substring(1).ToLowerInvariant().Trim())
                    {
                        case "?":
                        case "help":
                            return false;
                        case "out":
                            outputFileName = value;
                            break;
                        case "v":
                        case "verbose":
                            verbose = true;
                            break;
                        case "levels":
                            {
                                uint x = 0;
                                if (uint.TryParse(value, out x))
                                {
                                    levels = x;
                                }
                                else
                                {
                                    Console.WriteLine("Levels should provide a value integer value : {0}", value);
                                    return false;
                                }
                            }
                            break;
                        default:
                            Console.WriteLine("Unrecognized command line argument: {0}", args[i]);
                            return false;
                    }
                }
                else
                {
                    try
                    {
                        String fullPath = Path.GetFullPath(arg);
                        if (!File.Exists(fullPath))
                        {
                            Console.WriteLine("File not found: {0}", fullPath);
                            return false;
                        }
                        else
                        {
                            assemblies.Add(fullPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Invalid file name: {0}", arg);
                        Console.WriteLine(ex.Message);
                        return false;
                    }
                }
            }
            if (assemblies.Count == 0)
            {
                Console.WriteLine("Missing assemblies to process");
                return false;
            }
            return true;
        }

        void Run()
        {
            GraphSchema assemblySchema = new GraphSchema("ildependsSchema");
            UnresolvedCategory = assemblySchema.Categories.AddNewCategory("Unresolved");
            AssemblyCategory = assemblySchema.Categories.AddNewCategory("CodeSchema_Assembly");

            List<string> paths = new List<string>();

            string windir = Environment.GetEnvironmentVariable("WINDIR");
            paths.Add(windir + @"\Microsoft.NET\Framework\v4.0.30319");
            paths.Add(Directory.GetCurrentDirectory());

            host = new ConsoleHostEnvironment(paths.ToArray(), true);

            Graph graph = new Graph();
            graph.AddSchema(assemblySchema);

            GraphConditionalStyle errorStyle = new GraphConditionalStyle(graph);
            GraphCondition condition = new GraphCondition(errorStyle);
            condition.Expression = "HasCategory('Unresolved')";
            GraphSetter setter = new GraphSetter(errorStyle, "Icon");
            setter.Value = "pack://application:,,,/Microsoft.VisualStudio.Progression.GraphControl;component/Icons/kpi_red_sym2_large.png";
            graph.Styles.Add(errorStyle);

            foreach (String fullPath in this.assemblies)
            {
                try
                {
                    Console.WriteLine("Processing: " + fullPath);
                    IAssembly assembly = host.LoadUnitFrom(fullPath) as IAssembly;
                    GraphNode root = graph.Nodes.GetOrCreate(GetNodeID(assembly.AssemblyIdentity), assembly.AssemblyIdentity.Name.Value, AssemblyCategory);
                    WalkDependencies(graph, root, assembly, 0, new HashSet<IAssembly>());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("### Error processing assembly: " + ex.Message);
                    Console.WriteLine(ex.ToString());
                }
            }

            try
            {
                graph.Save(this.outputFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving output file: {0}\n{1}", this.outputFileName, ex.Message);
            }
        }

        static GraphNodeIdName AssemblyName = GraphNodeIdName.Get("Assembly", "Assembly", typeof(Uri));


        static Dictionary<string, Uri> assemblyMap = new Dictionary<string, Uri>();

        Uri FindAssembly(AssemblyIdentity id)
        {
            if (!string.IsNullOrEmpty(id.Location))
            {
                return new Uri(id.Location);
            }

            if (assemblyMap.Count == 0)
            {
                // populate it.
                foreach (string path in host.SearchPaths)
                {
                    FindAssemblies(path);
                }
            }

            string name = id.Name.Value.ToLowerInvariant(); ;

            Uri result = null;
            if (assemblyMap.TryGetValue(name, out result)) 
            {
                return result;
            }
            else
            {
                /// hmmm... make one up then...
                result = new Uri(Directory.GetCurrentDirectory() + "\\" + name + ".dll");
            }

            return result;
        }

        private void FindAssemblies(string path)
        {
            foreach (string file in Directory.GetFiles(path, "*.dll"))
            {
                string key = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                if (!assemblyMap.ContainsKey(key))
                {
                    assemblyMap.Add(key, new Uri(file));
                }
                else if (verbose)
                {
                    Console.WriteLine("### skipping dll because it is already found: " + key);
                }
            }
            foreach (string dir in Directory.GetDirectories(path))
            {
                FindAssemblies(dir);
            }
        }

        GraphNodeId GetNodeID(AssemblyIdentity id)
        {
            return GraphNodeId.GetNested(new GraphNodeId[] {
                GraphNodeId.GetPartial(AssemblyName, FindAssembly(id)) });
        }

        private void WalkDependencies(Graph graph, GraphNode root, IAssembly assembly, uint level, HashSet<IAssembly> visited)
        {
            if (verbose)
            {
                Console.Write(new string(' ', (int)(level * 2)));
                Console.WriteLine(assembly.Location);
            }
            visited.Add(assembly);
            foreach (IAssemblyReference r in assembly.AssemblyReferences)
            {
                GraphNode node = graph.Nodes.GetOrCreate(GetNodeID(r.AssemblyIdentity), r.AssemblyIdentity.Name.Value, AssemblyCategory);
                graph.Links.GetOrCreate(root, node);

                IAssembly referenced = r.ResolvedAssembly;
                if (referenced != null)
                {
                    if (!visited.Contains(referenced) && level <= levels)
                    {
                        WalkDependencies(graph, node, referenced, level + 1, visited);
                    }
                }
                else
                {
                    node.AddCategory(UnresolvedCategory);
                }
            }
        }
    }


    internal class ConsoleHostEnvironment : FullyResolvedPathHost
    {

        internal readonly Microsoft.Cci.Immutable.PlatformType platformType;

        internal ConsoleHostEnvironment(string[] libPaths, bool searchGAC)
            : base()
        {
            foreach (var p in libPaths)
            {
                this.AddLibPath(p);
            }
            this.SearchInGAC = searchGAC;
            this.platformType = new Microsoft.Cci.Immutable.PlatformType(this);
        }

        public override IUnit LoadUnitFrom(string location)
        {
            IUnit result = this.peReader.OpenModule(BinaryDocument.GetBinaryDocumentForFile(location, this));
            this.RegisterAsLatest(result);
            return result;
        }

        public List<string> SearchPaths
        {
            get { return this.LibPaths; }
        }

        protected override IPlatformType GetPlatformType()
        {
            return this.platformType;
        }

        /// <summary>
        /// override this here to not use memory mapped files since we want to use asmmeta in msbuild and it is sticky
        /// </summary>
        public override IBinaryDocumentMemoryBlock/*?*/ OpenBinaryDocument(IBinaryDocument sourceDocument)
        {
            try
            {
                IBinaryDocumentMemoryBlock binDocMemoryBlock = UnmanagedBinaryMemoryBlock.CreateUnmanagedBinaryMemoryBlock(sourceDocument.Location, sourceDocument);
                this.disposableObjectAllocatedByThisHost.Add((IDisposable)binDocMemoryBlock);
                return binDocMemoryBlock;
            }
            catch (IOException)
            {
                return null;
            }
        }
    }
}
