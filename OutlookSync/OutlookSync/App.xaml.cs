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

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledException);            
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ShowUnhandledException(e.ExceptionObject);            
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            ShowUnhandledException(e.Exception);
        }

        void ShowUnhandledException(object e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                UnhandledExceptionWindow uew = new UnhandledExceptionWindow();
                uew.ErrorMessage = (e != null ? e.ToString() : "No Exception object provided");
                uew.ShowDialog();
            }));
        }
    }
}
