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

        static void Main(string[] args)
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

            foreach (String fileName in args)
            {
                String fullPath = Path.GetFullPath(fileName);
                try
                {
                    Console.WriteLine("Processing: " + fullPath);
                    IAssembly assembly = host.LoadUnitFrom(args[0]) as IAssembly;
                    GraphNode root = graph.Nodes.GetOrCreate(GetNodeID(assembly.AssemblyIdentity), assembly.AssemblyIdentity.Name.Value, AssemblyCategory);
                    WalkDependencies(graph, root, assembly, new HashSet<IAssembly>());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("#Error: " + ex.Message);
                }
            }

            graph.Save("out.dgml");
        }

        static GraphNodeIdName AssemblyName = GraphNodeIdName.Get("Assembly", "Assembly", typeof(Uri));


        static Dictionary<string, Uri> assemblyMap = new Dictionary<string, Uri>();

        static Uri FindAssembly(AssemblyIdentity id)
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

        private static void FindAssemblies(string path)
        {
            foreach (string file in Directory.GetFiles(path, "*.dll"))
            {
                assemblyMap.Add(Path.GetFileNameWithoutExtension(file).ToLowerInvariant(), new Uri(file));
            }
            foreach (string dir in Directory.GetDirectories(path))
            {
                FindAssemblies(dir);
            }
        }

        static GraphNodeId GetNodeID(AssemblyIdentity id)
        {
            return GraphNodeId.GetNested(new GraphNodeId[] {
                GraphNodeId.GetPartial(AssemblyName, FindAssembly(id)) });
        }

        private static void WalkDependencies(Graph graph, GraphNode root, IAssembly assembly, HashSet<IAssembly> visited)
        {
            visited.Add(assembly);
            foreach (IAssemblyReference r in assembly.AssemblyReferences)
            {
                GraphNode node = graph.Nodes.GetOrCreate(GetNodeID(r.AssemblyIdentity), r.AssemblyIdentity.Name.Value, AssemblyCategory);
                graph.Links.GetOrCreate(root, node);

                IAssembly referenced = r.ResolvedAssembly;
                if (referenced != null)
                {
                    if (!visited.Contains(referenced))
                    {
                        WalkDependencies(graph, node, referenced, visited);
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
