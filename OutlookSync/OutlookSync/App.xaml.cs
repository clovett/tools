using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace OutlookSync
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            UnhandledExceptionWindow uew = new UnhandledExceptionWindow();
            uew.ErrorMessage = (e.Exception != null ? e.Exception.ToString() : "No Exception object provided");
            e.Handled = true;
            uew.ShowDialog();
        }
    }
}
