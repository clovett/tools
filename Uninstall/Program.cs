using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;

namespace Uninstall
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length != 1)
            {
                PrintUsage();
                return;
            }
            string filename = args[0];
            XDocument doc = null;
            try
            {
                doc = XDocument.Load(filename, LoadOptions.SetLineInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading XML document '" + filename + "'\n" + ex.Message);
            }
            try
            {
                new Program().Uninstall(doc);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing XML document '" + filename + "'\n" + ex.Message);
            }
        }

        Dictionary<int, List<ProductInfo>> groups = new Dictionary<int, List<ProductInfo>>();
        List<ProductInfo> all = new List<ProductInfo>();
               
        void Uninstall(XDocument doc)
        {
            if (doc.Root.Name != "uninstall")
            {
                throw new InvalidOperationException("Expecting root element 'uninstall'");
            }

            LoadAndMatchProducts(doc);
            UninstallMatchedProducts();
        }

        void LoadAndMatchProducts(XDocument doc)
        {
            int i = 0;
            foreach (XElement group in doc.Root.Elements("group"))
            {
                List<ProductInfo> list = new List<ProductInfo>();
                groups[i] = list;
                foreach (XElement item in group.Elements("item"))
                {
                    ProductInfo info = new ProductInfo();
                    info.Group = i;
                    info.Match = item.Value;
                    if (string.IsNullOrEmpty(info.Match))
                    {
                        int line = ((IXmlLineInfo)item).LineNumber;
                        throw new InvalidOperationException("'item' element must contain product name to match on line " + line);
                    }
                    all.Add(info);
                    list.Add(info);
                }
                if (list.Count == 0)
                {
                    int line = ((IXmlLineInfo)group).LineNumber;
                    throw new InvalidOperationException("Expecting child 'item' elements inside the 'group' on line " + line);
                }
                i++;
            }
            if (i == 0)
            {
                throw new InvalidOperationException("Expecting child 'group' elements");
            }

            // Now match installed products...
            foreach (ProductInstallation productInfo in ProductInstallation.AllProducts)
            {
                string name = productInfo.ProductName;
                if (!string.IsNullOrEmpty(name))
                {
                    foreach (ProductInfo pi in all)
                    {
                        if (name.IndexOf(pi.Match, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (pi.Products == null)
                            {
                                pi.Products = new List<ProductInstallation>();
                            }
                            //Console.WriteLine("Found matching product: " + name + " in group " + pi.Group);
                            pi.Products.Add(productInfo);
                        }
                    }
                }
            }
        }

        void UninstallMatchedProducts()
        {
            // now do the uninstall.
            for (int j = 0; j < groups.Count; j++)
            {
                List<ProductInfo> list = groups[j];
                foreach (ProductInfo info in list) 
                {
                    if (info.Products != null)
                    {
                        foreach (ProductInstallation pi in info.Products)
                        {
                            try
                            {
                                Console.Write("Uninstalling '" + pi.ProductName + "'...");
                                Installer.ConfigureProduct(pi.ProductCode, 0, InstallState.Absent, null);
                                Console.WriteLine("ok");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("\n### Error:" + e.Message);
                            }
                        }
                    }
                }
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: Uninstall <info.xml>");
            Console.WriteLine("Uninstalls all products in the given xml file in the order specified");
        }
    }

    class ProductInfo
    {
        int _group;
        string _match;
        List<ProductInstallation> _products;

        public int Group { get { return _group;} set { _group = value;} }
        public string Match { get {return _match;} set {_match = value;} }
        public List<ProductInstallation> Products { get { return _products;} set {_products = value;} }
    }
}
