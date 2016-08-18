using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Prompt
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public string[] Arguments { get; set; }
        public int ExitCode { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            Arguments = e.Args;
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            e.ApplicationExitCode = ExitCode;
            base.OnExit(e);
        }
    }
   
}
