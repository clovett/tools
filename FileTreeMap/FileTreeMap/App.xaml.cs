using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace FileTreeMap
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public string[] Arguments;

        protected override void OnStartup(StartupEventArgs e)
        {
            Arguments = e.Args;
            base.OnStartup(e);
        }
    }
}
