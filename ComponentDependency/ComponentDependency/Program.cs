using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentDependency
{
    class Program
    {
        string pattern;
        string fileName;

        static void Main(string[] args)
        {
            Program p = new Program();
            if (p.ParseCommandLine(args))
            {
                p.Run();
            }
            else
            {
                PrintUsage();
            }
        }

        private bool ParseCommandLine(string[] args)
        {
            for (int i = 0, n = args.Length; i < n; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "?":
                        case "h":
                        case "help":
                            return false;
                        default:
                            Console.WriteLine("### Error: unexpected command line argument: " + arg);
                            return false;
                    }
                }
                else if (pattern == null)
                {
                    pattern = arg;
                }
                else if (fileName == null)
                {
                    fileName = arg;
                }
                else
                {
                    Console.WriteLine("### Error: too many arguments");
                    return false;
                }
            }
            if (pattern == null)
            {
                Console.WriteLine("### Error: missing pattern argument");
                return false;
            }
            if (fileName == null)
            {
                fileName = "graph.dgml";
            }
            return true;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: ComponentDependency <pattern> [<graph.dgml>]");
            Console.WriteLine("Finds all installed components matching the given pattern and creates the dependency graph");
        }

        Dictionary<string, Product> products = new Dictionary<string, Product>();
        Dictionary<string, Feature> features = new Dictionary<string, Feature>();
        Dictionary<string, Component> components = new Dictionary<string, Component>();

        Graph graph;

        public static Graph GetEmbeddedGraphResource(string name)
        {
            using (System.IO.Stream s = typeof(Program).Assembly.GetManifestResourceStream("ComponentDependency." + name))
            {
                return Graph.Load(s, DgmlCommonSchema.Schema, InstallerSchema.Schema);
            }
        }


        private GraphNode CreateProductNode(Product p)
        {
            string id = "product:" + p.Guid;
            var pnode = graph.Nodes.Get(id);
            if (pnode == null)
            {
                pnode = graph.Nodes.GetOrCreate(id, p.Name, InstallerSchema.ProductCategory);

                if (!string.IsNullOrEmpty(p.DisplayName))
                {
                    pnode.SetValue(InstallerSchema.DisplayNameProperty, p.DisplayName);
                }
                if (!string.IsNullOrEmpty(p.LocalPackage))
                {
                    pnode.SetValue(InstallerSchema.LocalPackageProperty, p.LocalPackage);
                }
                if (!string.IsNullOrEmpty(p.Publisher))
                {
                    pnode.SetValue(InstallerSchema.PublisherProperty, p.Publisher);
                }
            }
            return pnode;
        }

        GraphNode CreateFeatureNode(Feature f)
        {
            var fnode = graph.Nodes.Get(f.Guid);
            if (fnode == null)
            {
                fnode = graph.Nodes.GetOrCreate(f.Guid, null, InstallerSchema.FeatureCategory);
                var p = f.Product;
                if (p != null)
                {
                    var pnode = CreateProductNode(p);
                    graph.Links.GetOrCreate(pnode, fnode);
                }
            }
            return fnode;
        }

        GraphNode CreateComponentNode(Component c)
        {
            GraphNode cnode = graph.Nodes.Get(c.Guid);
            if (cnode == null)
            {
                cnode = graph.Nodes.GetOrCreate(c.Guid, null, InstallerSchema.ComponentCategory);

                foreach (var f in c.Features)
                {
                    var fnode = CreateFeatureNode(f);
                    graph.Links.GetOrCreate(fnode, cnode);
                }
            }
            return cnode;
        }

        GraphNode CreateItemNode(string id)
        {
            GraphNode node = graph.Nodes.Get(id);
            if (node == null)
            {
                string label = id;
                try
                {
                    label = System.IO.Path.GetFileName(id);
                }
                catch
                {

                }
                node = graph.Nodes.GetOrCreate(id, label, InstallerSchema.ItemCategory);
                node.SetValue(InstallerSchema.FilePathProperty, id);
            }
            return node;
        }

        void Run()
        {
            LoadProducts();
            LoadFeatures();
            LoadComponents();

            this.graph = GetEmbeddedGraphResource("Template.dgml");

            foreach (var c in components.Values)
            {
                foreach (var v in c.Values)
                {
                    if (v.IndexOf(pattern) >= 0)
                    {
                        var file = CreateItemNode(v);
                        var cnode = CreateComponentNode(c);
                        graph.Links.GetOrCreate(cnode, file);
                    }
                }
            }

            graph.Save(fileName);
        }

        private void LoadProducts()
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\Installer\Products", false);
            if (key != null)
            {
                using (key)
                {
                    foreach (string productGuid in key.GetSubKeyNames())
                    {
                        Product c = new Product() { Guid = productGuid };

                        using (var subkey = key.OpenSubKey(productGuid, false))
                        {
                            c.PackageCode = (string)subkey.GetValue("PackageCode");
                            c.Name = (string)subkey.GetValue("ProductName");
                        }

                        products[productGuid] = c;
                    }
                }
            }
        }
        private void LoadFeatures()
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\Installer\Features", false);
            if (key != null)
            {
                using (key)
                {
                    foreach (string featureGuid in key.GetSubKeyNames())
                    {
                        Feature f = new Feature() { Guid = featureGuid };

                        Product p = null;
                        if (products.TryGetValue(featureGuid, out p))
                        {
                            f.Product = p;
                        }
                        else
                        {
                            Console.WriteLine("no matching product for feature " + featureGuid);
                        }

                        features[featureGuid] = f;
                    }
                }
            }
        }
        void LoadComponents()
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData", false);
            if (key != null)
            {
                using (key)
                {
                    foreach (string section in key.GetSubKeyNames())
                    {
                        using (var sectionKey = key.OpenSubKey(section, false))
                        {
                            foreach (string subSectionName in sectionKey.GetSubKeyNames())
                            {
                                // should be "Components" or "Products"
                                using (var cpKey = sectionKey.OpenSubKey(subSectionName, false))
                                {
                                    foreach (string guid in cpKey.GetSubKeyNames())
                                    {
                                        if (subSectionName == "Components")
                                        {
                                            Component c = new Component() { Guid = guid };
                                            components[guid] = c;

                                            using (var subkey = cpKey.OpenSubKey(guid, false))
                                            {
                                                foreach (var featureId in subkey.GetValueNames())
                                                {
                                                    Feature f = null;
                                                    if (features.TryGetValue(featureId, out f))
                                                    {
                                                        c.Features.Add(f);
                                                    }
                                                    else
                                                    {
                                                        f = new Feature() { Guid = featureId };
                                                        c.Features.Add(f);
                                                    }

                                                    if (subkey.GetValueKind(featureId) == RegistryValueKind.String)
                                                    {
                                                        string value = (string)subkey.GetValue(featureId);
                                                        c.Values.Add(value);
                                                    }
                                                }
                                            }
                                        }
                                        else if (subSectionName == "Products")
                                        {
                                            Product p = null;
                                            if (!products.TryGetValue(guid, out p))
                                            {
                                                p = new Product() { Guid = guid };
                                                products[guid] = p;
                                            }
                                            using (var installProperties = cpKey.OpenSubKey(guid + "\\InstallProperties", false))
                                            {
                                                if (installProperties != null)
                                                {
                                                    p.DisplayName = (string)installProperties.GetValue("DisplayName");
                                                    p.LocalPackage = (string)installProperties.GetValue("LocalPackage");
                                                    p.Publisher = (string)installProperties.GetValue("Publisher");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

    }

    public class Product
    {
        public string Guid { get; set; }

        public string PackageCode { get; set; }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string LocalPackage { get; set; }

        public string Publisher { get; set; }
    }

    public class Feature
    {
        public string Guid { get; set; }

        public Product Product { get; set;  }
    }

    public class Component
    {
        public Component()
        {
            Features = new List<Feature>();
            Values = new List<string>();
        }

        public string Guid { get; set; }

        public List<Feature> Features { get; set; }

        public List<string> Values { get; set; }

    }
}
