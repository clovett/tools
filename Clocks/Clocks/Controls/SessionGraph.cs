using Clocks.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Clocks.Controls
{

    /// <summary>
    /// Interaction logic for SessionGraph.xaml
    /// </summary>
    public partial class SessionGraph : BarGraph
    {
        Data data;
        bool updating = false;

        public SessionGraph()
        {
        }

        public Data Data
        {
            get
            {
                return this.data;
            }
            set
            {
                if (this.data != value)
                {
                    if (this.data != null)
                    {
                        this.data.HistoryChanged -= OnDataChanged;
                    }
                    this.data = value;
                    if (this.data != null)
                    {
                        this.data.HistoryChanged += OnDataChanged;
                    }
                }
                UpdateGraph();
            }
        }

        void OnDataChanged(object sender, EventArgs e)
        {
            UpdateGraph();
        }

        void UpdateGraph()
        {
            this.Clear();
            foreach (Session session in data.History)
            {
                BarSeries s = AddSeries(session);
                UpdateSeries(session, s);
            }
            this.InvalidateArrange();
        }

        public BarSeries AddSeries(Session session)
        {
            BarSeries bar = base.AddSeries(session.Times.Count);
            bar.UserData = session;
            bar.Changed += new EventHandler(OnBarChanged);
            return bar;
        }

        void UpdateSeries(Session data, BarSeries series)
        {
            updating = true;
            series.Clear();
            series.BeginUpdate();
            foreach (ulong time in data.Times)
            {
                series.AddDataPoint(time);
            }
            series.EndUpdate();
            updating = false;
            OnBarChanged(series, EventArgs.Empty);
        }

        void OnBarChanged(object sender, EventArgs e)
        {
            if (!updating)
            {
                BarSeries bar = (BarSeries)sender;
                Session session = (Session)bar.UserData;
                bar.Bar.Fill = this.Foreground;
            }
        }


        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Foreground.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register("Foreground", typeof(Brush), typeof(SessionGraph), new PropertyMetadata(Brushes.White));


    }

}