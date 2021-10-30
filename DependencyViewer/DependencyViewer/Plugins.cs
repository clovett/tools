using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace DependencyViewer {
    internal static class Plugins {

        static List<GraphGenerator> registered = new List<GraphGenerator>();

        public static void Register(GraphGenerator g) {
            registered.Add(g);
        }

        public static GraphGenerator Create(string[] fileNames) {
            if (fileNames == null || fileNames.Length == 0) {
                throw new ArgumentException("Must provide at least one file name", "fileNames");
            }
            string fname = Path.GetFileName(fileNames[0]);
            foreach (GraphGenerator g in registered) {
                if (g.FilterMatches(fname)) {
                    g.FileNames = fileNames;
                    return g;
                }
            }
            throw new ArgumentException(string.Format("Could not find graph generator for this file type: '{0}'", fname));
        }

        public static string Filters {
            get {
                StringBuilder sb = new StringBuilder();
                foreach (GraphGenerator g in registered) {
                    if (sb.Length > 0) sb.Append("|");
                    sb.Append(g.FileFilter);
                }
                return sb.ToString();
            }
        }

        public static void LoadGenerators() {
            Plugins.Register(new ImageGenerator()); // built in.
            Plugins.Register(new DgmlGenerator()); // built in.
            Plugins.Register(new DotGenerator()); // built in.

            System.Diagnostics.Process proc = System.Diagnostics.Process.GetCurrentProcess();
            string mainExe = proc.MainModule.FileName;
            if (!string.IsNullOrEmpty(mainExe)) {
                string path = Path.GetDirectoryName(mainExe);
                IList<string> files = new List<string>();
                GetAllAssemblies(path, files);
                foreach (string fname in files) {
                    // check and see if it's another GraphGenerator or not.
                    try {
                        Type gt = typeof(GraphGenerator);
                        Assembly a = Assembly.LoadFrom(fname);
                        foreach (Type t in a.GetExportedTypes()) {
                            if (gt.IsAssignableFrom(t) && !t.IsAbstract) {
                                GraphGenerator plugin = Activator.CreateInstance(t) as GraphGenerator;
                                Debug.Assert(plugin != null);
                                Plugins.Register(plugin);
                            }
                        }
                    } catch (Exception e) {
                        Trace.WriteLine("Cannot load plugin assembly: " + fname);
                        Trace.WriteLine(e.Message);
                    }
                }
            }
        }

        private static void GetAllAssemblies(string path, IList<string> files) {
            foreach (string dll in System.IO.Directory.GetFiles(path, "*.dll")) {
                files.Add(dll);
            }
            //foreach (string dir in System.IO.Directory.GetDirectories(path)) {
            //    GetAllAssemblies(dir, files);
            //}
        }
    }
}

