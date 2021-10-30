using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;

namespace DependencyViewer {

    public class MsbuildGraphGenerator : GraphGenerator {
        IDictionary<Uri, GraphNode> map;
        GraphType type = GraphType.Projects;
        GraphFlags flags = GraphFlags.Imports;
        MenuItem menu;
        IDictionary<Uri, XmlDocument> docs;
        IDictionary<string, string> variables;
        Stack<XmlDocument> projects;

        enum GraphType {
            Projects, Variables
        }

        enum GraphFlags {
            Imports = 1
        }

        public MsbuildGraphGenerator() {
        }


        public override void SaveState(ViewState state) {
            base.SaveState(state);
            state["GraphType"] = type;
            state["GraphFlags"] = flags;
        }

        public override void LoadState(ViewState state) {
            base.LoadState(state);
            type = (GraphType)state["GraphType"];
            flags = (GraphFlags)state["GraphFlags"];
            UpdateMenu();
        }

        public override string Label {
            get {
                return this.type == GraphType.Projects ? "Projects View" : "Variables View";
            }
        }


        public override string FileFilter {
            get {
                return "MSBuild Projects (*.*proj;*.targets)|*.*proj;*.targets";
            }
        }


        public override void CreateViewMenu(MenuItem menu) {
            this.menu = menu;
            CreateMenuItem("_Projects", type == GraphType.Projects, GraphType.Projects);
            
            //CreateMenuItem("_Variables", type == GraphType.Variables, GraphType.Variables);
            
            menu.Items.Add(new Separator());
            CreateMenuItem("_Imports", ShowImports, GraphFlags.Imports);
            
        }

        MenuItem CreateMenuItem(string label, bool check, object flag) {
            MenuItem item = new MenuItem();
            item.Header = label;
            item.IsChecked = check;
            item.Tag = flag;
            item.Click += new RoutedEventHandler(OnClick);
            menu.Items.Add(item);
            return item;
        }

        void OnClick(object sender, RoutedEventArgs e) {
            OnBeforeChange();
            MenuItem item = (MenuItem)sender;
            if (item.Tag is GraphType) {
                type = (GraphType)item.Tag;
            } else {
                ToggleFlag((GraphFlags)item.Tag);
            }
            UpdateMenu();
            OnAfterChange();
        }

        void UpdateMenu() {
            foreach (object item in menu.Items) {
                MenuItem mi = item as MenuItem;
                if (mi != null) {
                    if (mi.Tag is GraphType) {
                        GraphType t = (GraphType)mi.Tag;
                        mi.IsChecked = (this.type == t);
                    } else if (mi.Tag is GraphFlags) {
                        GraphFlags f = (GraphFlags)mi.Tag;
                        mi.IsChecked = (this.flags & f) != 0;
                    }
                }
            }
        }

        bool ToggleFlag(GraphFlags flag) {
            if ((flags & flag) != 0) {
                flags &= ~flag;
                return false;
            } else {
                flags |= flag;
                return true;
            }
        }

        public bool ShowImports {
            get { return (flags & GraphFlags.Imports) != 0; }
        }

        public override void Prepare() {
            base.Prepare();
           
            // Make sure we can resolve all the includes/imports.
            docs = new Dictionary<Uri, XmlDocument>();
            variables = new Dictionary<string, string>();
            projects = new Stack<XmlDocument>();
            map = new Dictionary<Uri, GraphNode>();
            
            foreach (string fname in this.FileNames) {
                Uri uri = new Uri(fname);
                AddProjects(uri);                
            }
        }

        // Statically load the specified assemblies and return the type graphs or dependency graph.
        public override void Create(Panel container) {

            map = new Dictionary<Uri, GraphNode>();
            base.Create(container);

            // Load all the projects and capture the project hierarchies.
            foreach (string fname in this.FileNames) {
                Uri uri = new Uri(fname);
                switch (type) {
                    case GraphType.Projects:
                        AddProjectGraph(uri);
                        break;
                    case GraphType.Variables:
                        AddVariableGraph(uri);
                        break;
                }
            }
        }

        string GetAssemblyName(XmlElement root, XmlNamespaceManager nsmgr) {
            Uri uri = new Uri(root.BaseURI);
            return uri.Segments[uri.Segments.Length - 1];

            // this would be a handy tooltip.

            //XmlNode node = root.SelectSingleNode("//x:AssemblyName", nsmgr);
            //if (node == null) {
            //    node = root.SelectSingleNode("//x:OutputName", nsmgr);
            //    if (node == null) {
                    
            //    }
            //}
            //return ResolveVariables(node.InnerText);
        }

        void AddVariables(Uri uri, XmlDocument doc, XmlNamespaceManager nsmgr) {
            // populate variables
            docs[uri] = doc;
            foreach (XmlElement e in doc.SelectNodes("//x:PropertyGroup", nsmgr)) {
                foreach (XmlNode n in e.ChildNodes) {
                    XmlElement child = n as XmlElement;
                    if (child != null) {
                        string name = child.LocalName;
                        string value = child.InnerText;
                        value = ResolveVariables(value);
                        variables[name] = value;
                    }
                }
            }            
        }

        void AddProjects(Uri uri) {
            bool isProject = uri.AbsoluteUri.EndsWith("proj", StringComparison.InvariantCultureIgnoreCase);
            try {
                XmlDocument a = new XmlDocument();
                a.Load(uri.AbsoluteUri);
                if (isProject) projects.Push(a);

                XmlElement root = a.DocumentElement;

                string nsuri = root.NamespaceURI;
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(a.NameTable);
                nsmgr.AddNamespace("x", nsuri);

                AddVariables(uri, a, nsmgr);
                AddIncludes(uri, a, nsmgr);
            } catch (Exception e) {
                Trace.WriteLine("Error loading: " + uri.AbsoluteUri);
                Trace.WriteLine(e.Message);
            } finally {
                if (isProject) projects.Pop();
            }
        }

        void AddIncludes(Uri baseUri, XmlDocument d, XmlNamespaceManager nsmgr) {
            string[] queries = new string[] { "//x:Import", "//x:ProjectFile", "//x:ProjectReference" };
            foreach (string query in queries) {
                foreach (XmlElement e in d.SelectNodes(query, nsmgr)) {
                    string rel = e.GetAttribute("Include");
                    if (string.IsNullOrEmpty(rel)) {
                        rel = e.GetAttribute("Project");
                    } 
                    
                    Uri resolved = ResolveUri(baseUri, ResolveVariables(rel));

                    if (!docs.ContainsKey(resolved)) {
                        AddProjects(resolved);                        
                    }
                }
            }
        }

        Uri ResolveUri(Uri baseUri, string path) {
            return new Uri(baseUri, path);
        }

        IDictionary<string, string> guard = new Dictionary<string, string>();

        string ResolveVariables(string s) {
            if (guard.ContainsKey(s)) {
                throw new Exception("Circular variable definition: " + s);
            }
            string key = s;
            guard[key] = key;
            // todo: resolve msbuild variable references like $(foo)   
            Regex regex = new Regex(@"\$\([^\)]*\)");
            Match m = regex.Match(s);
            while (m != null && m.Success) {
                int index = m.Index;
                int length = m.Length;
                // Strip off the $( and )
                string name = s.Substring(index+2, length-3);
                string v = GetMsbuildVariable(name);
                if (v != null) {
                    string head = s.Substring(0, index);
                    string tail = s.Substring(index + length);
                    s = head + v + tail;
                }
                m = regex.Match(s);
            }
            guard.Remove(key);
            return s;
        }

        string GetMsbuildVariable(string name) {
            switch (name) {
                case "MSBuildProjectDirectory":
                    return MSBuildProjectDirectory;
                case "MSBuildProjectFile":
                    return MSBuildProjectFile;
                case "MSBuildProjectExtension":
                    return MSBuildProjectExtension;
                case "MSBuildProjectFullPath":
                    return MSBuildProjectFullPath;
                case "MSBuildProjectName":
                    return MSBuildProjectName;
                case "MSBuildBinPath":
                    return MSBuildBinPath;
                case "MSBuildToolsPath":
                    return MSBuildToolsPath;
                case "MSBuildProjectDefaultTargets":
                    return MSBuildProjectDefaultTargets;
                case "MSBuildExtensionsPath":
                case "MSBuildExtensionsPath32":
                    return ResolveVariables(@"$(ProgramFiles)\MSBuild");
            }

            if (variables.ContainsKey(name)) {
                return ResolveVariables(variables[name]);
            }
            
            string result = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(result)) {
                return "";
            }
            return result;
        }

        Uri CurrentProject {
            get {
                XmlDocument p = projects.Peek();
                return new Uri(p.BaseURI);
            }
        }
        string MSBuildProjectDirectory {
            get {
                return Path.GetDirectoryName(MSBuildProjectFullPath);
            }
        }

        string MSBuildProjectFile {
            get {
                Uri uri = this.CurrentProject;
                string filename = uri.Segments[uri.Segments.Length - 1];
                return filename;
            }
        }

        string MSBuildProjectExtension {
            get {
                Uri uri = this.CurrentProject;
                string filename = uri.Segments[uri.Segments.Length - 1];
                return Path.GetExtension(filename);
            }
        }

        string MSBuildProjectFullPath {
            get {
                Uri uri = this.CurrentProject;
                if (uri.IsFile) return uri.LocalPath;
                return uri.AbsoluteUri;
            }
        }

        string MSBuildProjectName {
            get {
                Uri uri = this.CurrentProject;
                string filename = uri.Segments[uri.Segments.Length - 1];
                return Path.GetFileNameWithoutExtension(filename);
            }
        }
        string MSBuildBinPath {
            get {
                return GetBinPath("2.0");
            }
        }

        string MSBuildToolsPath {
            get {
                XmlDocument p = projects.Peek();
                string ver = p.DocumentElement.GetAttribute("ToolsVersion");
                if (string.IsNullOrEmpty(ver)) ver = "2.0";
                return GetBinPath(ver);
            }
        }

        string GetBinPath(string version) {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\MSBuild\ToolsVersions\" + version, false)) {
                if (key != null) {
                    string path = key.GetValue("MSBuildToolsPath").ToString();
                    return path;
                }
            }
            return "";
        }

        string MSBuildProjectDefaultTargets {
            get {
                XmlDocument p = projects.Peek();
                return p.DocumentElement.GetAttribute("DefaultTargets");
            }
        }

        void AddVariableGraph(Uri uri) {
        }

        void AddProjectGraph(Uri path) {
            if (!docs.ContainsKey(path))
                return;

            XmlDocument a = docs[path];
            projects.Push(a);
            XmlElement root = a.DocumentElement;

            string nsuri = root.NamespaceURI;
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(a.NameTable);
            nsmgr.AddNamespace("x", nsuri);

            string name = GetAssemblyName(root, nsmgr);
            GraphNode node = AddNode(path, name);
            AddIncludeGraph(node, path, a, nsmgr);
            projects.Pop();
        }

        void AddIncludeGraph(GraphNode node, Uri key, XmlDocument doc, XmlNamespaceManager nsmgr) {
            string[] queries = new string[] { "//x:Import", "//x:ProjectFile", "//x:ProjectReference" };
            foreach (string query in queries) {
                foreach (XmlElement e in doc.SelectNodes(query, nsmgr)) {
                    string rel = e.GetAttribute("Include");
                    if (string.IsNullOrEmpty(rel) && ShowImports) {
                        rel = e.GetAttribute("Project");
                    } 
                    Uri resolved = ResolveUri(key, ResolveVariables(rel));

                    if (this.docs.ContainsKey(resolved)) {
                        XmlDocument include = this.docs[resolved];

                        string nsuri = doc.DocumentElement.NamespaceURI;
                        XmlNamespaceManager childmgr = new XmlNamespaceManager(include.NameTable);
                        childmgr.AddNamespace("x", nsuri);

                        string name = GetAssemblyName(include.DocumentElement, childmgr);
                        if (map.ContainsKey(resolved)) {
                            GraphNode child = AddNode(resolved, name);
                            AddEdge(node, child, "Includes");
                            // do not recurrse any further!
                        } else {
                            GraphNode child = AddNode(resolved, name);
                            AddEdge(node, child, "Includes");
                            AddIncludeGraph(child, resolved, include, childmgr);
                        }
                    }
                }
            }
        }

        internal GraphNode AddNode(Uri key, string label) {
            if (map.ContainsKey(key)) {
                return map[key];
            }
            string path = key.AbsoluteUri;
            GraphNode n = new GraphNode();
            n.Id = path;
            n.Label = label;
            n.NodeType = GetNodeType(path);
            map[key] = n;
            AddNode(n);
            return n;
        }

        string GetNodeType(string path) {
            int i = path.LastIndexOf(".");
            string ext = i >= 0 ? path.Substring(i) : "";
            return ext.ToLowerInvariant(); // .csproj, .vbproj, etc.            
        }
    }
}
