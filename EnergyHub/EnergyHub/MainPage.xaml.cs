using EnergyHub.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace EnergyHub
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        EnergyClient client;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var settings = Settings.Instance;
            base.OnNavigatedTo(e);

            client = ((App)App.Current).EnergyClient;

            Task.Run(new Action(() =>
            {
                int rc = client.InitializeAlljoynClient();
                string line = client.ReadLog();
                while (line != "EOF")
                {
                    Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
                    {
                        WriteResponse(line);
                    })).AsTask().Wait();
                    line = client.ReadLog();
                }
            }));
            
            return;
        }


        void WriteResponse(string msg)
        {
            if (ResponseText.Blocks.Count == 0)
            {
                ResponseText.Blocks.Add(new Paragraph());
            }

            Paragraph p = ResponseText.Blocks[0] as Paragraph;
            p.Inlines.Add(new Run() { Text = msg });
            p.Inlines.Add(new LineBreak());
        }
    }
}
