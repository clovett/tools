using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Walkabout.Utilities;

namespace NetworkDataUsage.Controls
{
    /// <summary>
    /// Interaction logic for TrafficGraphControl.xaml
    /// </summary>
    public partial class TrafficGraphControl : UserControl
    {
        NetworkTraffic traffic = new NetworkTraffic();
        double x = 0;

        public TrafficGraphControl()
        {
            InitializeComponent();
            this.Loaded += OnControlLoaded;
            this.Unloaded += OnControlUnloaded;
            this.traffic.Updated += OnTrafficUpdated;
            this.Chart.LiveScrolling = true;
        }

        private void OnTrafficUpdated(object sender, EventArgs e)
        {
            this.Chart.SetCurrentValue(new NetgearDataUsage.Model.DataValue()
            {
                X = x,                
                Y = this.traffic.GetCurrentBytesReceived()
            });
            x += 5;
        }

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            traffic.Start();
        }

        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            traffic.Stop();
        }

    }
}
