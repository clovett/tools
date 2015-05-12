using EnergyHub.Data;
using EnergyHub.Filters;
using EnergyHub.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            Graph.ValueGetter = OnGetNextValue;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var settings = Settings.Instance;
            base.OnNavigatedTo(e);


            var path = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "energy.sqlite");
            SqlDatabase database = SqlDatabase.OpenDatabase(path);
            

            client = ((App)App.Current).EnergyClient;

            Task.Run(new Action(GetEnergyData));

            Graph.Start();
            
            return;
        }

        private void GetEnergyData()
        {
            int rc = client.InitializeAlljoynClient();
            string line = client.ReadLog();
            while (line != "EOF")
            {
                ParseEnergyData(line);

                line = client.ReadLog();
            }
        }

        private void ParseEnergyData(string line)
        {
            string[] parts = line.Split(',');
            if (parts.Length > 2)
            {
                DateTime dt = DateTime.Now;
                double e = 0;
                if (DateTime.TryParse(parts[0] + " " + parts[1], out dt) && 
                    double.TryParse(parts[2], out e) && 
                    e < 10000) // strip out crazy big numbers (noise from radio)
                {
                    lock (data){
                        data.Add(new EnergyData() {
                             Time  = dt,
                              Energy = e
                        });
                    }
                }
            }
        }

        List<EnergyData> data = new List<EnergyData>();
        int pos = 0;

        private double OnGetNextValue()
        {
            lock (data)
            {
                int i = pos;
                if (i < data.Count)
                {
                    pos++;
                }
                else if (pos == data.Count && data.Count > 0)
                {
                    i = data.Count - 1;
                }
                if (i < data.Count)
                {
                    return data[i].Energy;
                }
            }
            return 0;
        }

    }
}
