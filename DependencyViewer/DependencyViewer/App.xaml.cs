using System;
using System.Windows;
using System.Data;
using System.Xml;
using System.Configuration;

namespace DependencyViewer {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>

    public partial class App : System.Windows.Application {
        static string[] args;

        public static string[] Args {
            get { return args; }
            set { args = value; }
        }

        protected override void OnStartup(StartupEventArgs e) {
            args = e.Args;
            base.OnStartup(e);            
        }
    }
}