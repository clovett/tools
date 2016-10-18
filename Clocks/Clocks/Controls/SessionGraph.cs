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
                StackedBar s = AddSeries(session);
                UpdateSeries(session, s);
            }
            this.InvalidateArrange();
        }

        public StackedBar AddSeries(Session session)
        {
            StackedBar bar = base.AddSeries(2);
            bar.UserData = session;
            return bar;
        }

        void UpdateSeries(Session data, StackedBar series)
        {
            series.Clear();
            series.BeginUpdate();

            List<Test> correct = new List<Test>(from t in data.Times
                                                where t.Answer == t.Entry
                                                select t);
            double totalCorrectMs = (from t in correct select (double)t.ElapsedMilliseconds).Sum();

            List<Test> errors = new List<Test>(from t in data.Times
                                               where t.Answer != t.Entry
                                               select t);
            double totalIncorrectMs = (from t in errors select (double)t.ElapsedMilliseconds).Sum();

            series.AddDataValue(new Controls.DataValue()
            {
                Value = totalCorrectMs,
                Color = ColorFromBrush(GoodBrush),
                Tooltip = correct.Count + " good answers in " + TimeSpan.FromSeconds((long)(totalCorrectMs / 1000)).ToString()
                 + " for a total time of " + TimeSpan.FromSeconds((long)((totalCorrectMs + totalIncorrectMs )/ 1000)).ToString()
            });
            series.AddDataValue(new Controls.DataValue()
            {
                Value = totalIncorrectMs,
                Color = ColorFromBrush(ErrorBrush),
                Tooltip = errors.Count + " wrong answers in " + TimeSpan.FromSeconds((long)(totalIncorrectMs / 1000)).ToString()
            });
            series.EndUpdate();
        }

        public Color ColorFromBrush(Brush brush)
        {
            SolidColorBrush solidBrush = brush as SolidColorBrush;
            if (solidBrush != null)
            {
                return solidBrush.Color;
            }
            return Colors.White;
        }


        public Brush GoodBrush
        {
            get { return (Brush)GetValue(GoodBrushProperty); }
            set { SetValue(GoodBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Foreground.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GoodBrushProperty =
            DependencyProperty.Register("GoodBrush", typeof(Brush), typeof(SessionGraph), new PropertyMetadata(Brushes.White));



        public Brush ErrorBrush
        {
            get { return (Brush)GetValue(ErrorBrushProperty); }
            set { SetValue(ErrorBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Foreground.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ErrorBrushProperty =
            DependencyProperty.Register("ErrorBrush", typeof(Brush), typeof(SessionGraph), new PropertyMetadata(Brushes.Red));


    }

}