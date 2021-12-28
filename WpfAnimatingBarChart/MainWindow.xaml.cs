using LovettSoftware.Controls;
using LovettSoftware.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;

namespace WpfAppTemplate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DelayedActions delayedActions = new DelayedActions();

        class Dataset
        {
            public Color Color;
            public List<BarChartDataValue> Data;
        }

        List<Dataset> datasets = new List<Dataset>();
        int dataset = 0;
        Random rand = new Random(Environment.TickCount);
        const double PlaySpeedSeconds = 2;

        public MainWindow()
        {
            UiDispatcher.Initialize();

            InitializeComponent();
            RestoreSettings();

            this.SizeChanged += OnWindowSizeChanged;
            this.LocationChanged += OnWindowLocationChanged;

            Settings.Instance.PropertyChanged += OnSettingsChanged;

            PauseButton.Visibility = Visibility.Collapsed;
            this.Chart.Orientation = Orientation.Horizontal;
            this.Chart.ColumnHover += OnColumnHover;
            this.Chart.ColumnClicked += OnColumnClicked;
            this.Chart.ToolTipGenerator = OnGenerateTip;
            this.Chart.Foreground = Brushes.SlateGray;

            LoadSamples();
            if (!string.IsNullOrEmpty(Settings.Instance.LastFile) && File.Exists(Settings.Instance.LastFile))
            {
                Open(Settings.Instance.LastFile);
            }
            else
            {
                Toggle();
            }
        }

        void Toggle()
        {
            var ds = datasets[dataset];
            Chart.FillColor = ds.Color;
            Chart.Series = ds.Data;

            dataset++;
            if (dataset >= datasets.Count)
            {
                dataset = 0;
            }
        }

        void LoadSamples()
        {
            datasets.Add(new Dataset() {
                Color = Colors.Purple,
                Data = new List<BarChartDataValue>()
                {
                    new BarChartDataValue() { Label = "2002", Value = 14813.67 },
                    new BarChartDataValue() { Label = "2003", Value = 13260.92 },
                    new BarChartDataValue() { Label = "2004", Value = 18412.61 },
                    new BarChartDataValue() { Label = "2005", Value = 16824.06 },
                    new BarChartDataValue() { Label = "2006", Value = 18653.40 },
                    new BarChartDataValue() { Label = "2007", Value = 20828.9 },
                    new BarChartDataValue() { Label = "2008", Value = 20534.97 },
                    new BarChartDataValue() { Label = "2009", Value = 19595.84 },
                    new BarChartDataValue() { Label = "2010", Value = 17665.99 },
                    new BarChartDataValue() { Label = "2011", Value = 19748.83 },
                    new BarChartDataValue() { Label = "2012", Value = 18100.11 },
                    new BarChartDataValue() { Label = "2013", Value = 19053.027 },
                    new BarChartDataValue() { Label = "2014", Value = 19971.89 },
                    new BarChartDataValue() { Label = "2015", Value = 17086.59 },
                    new BarChartDataValue() { Label = "2016", Value = 18769.17 },
                    new BarChartDataValue() { Label = "2017", Value = 21448.2 },
                    new BarChartDataValue() { Label = "2018", Value = 22470.60 },
                    new BarChartDataValue() { Label = "2019", Value = 20611.92 },
                    new BarChartDataValue() { Label = "2020", Value = 25070.87 },
                    new BarChartDataValue() { Label = "2021", Value = 25200.55 }
                }
            });

            datasets.Add(new Dataset() {
                Color = Colors.Orange,
                Data = new List<BarChartDataValue>()
                {
                    new BarChartDataValue() { Label = "2002",    Value = 2678.25  },
                    new BarChartDataValue() { Label = "2003",    Value = 3461.95  },
                    new BarChartDataValue() { Label = "2004",    Value = 3034.71  },
                    new BarChartDataValue() { Label = "2005",    Value = 2540.69  },
                    new BarChartDataValue() { Label = "2006",    Value = 2587.59  },
                    new BarChartDataValue() { Label = "2007",    Value = 2729.91  },
                    new BarChartDataValue() { Label = "2008",    Value = 2660.03  },
                    new BarChartDataValue() { Label = "2009",    Value = 1678.28  },
                    new BarChartDataValue() { Label = "2010",    Value = 1811.17  },
                    new BarChartDataValue() { Label = "2011",    Value = 2332.09  },
                    new BarChartDataValue() { Label = "2012",    Value = 2109.82  },
                    new BarChartDataValue() { Label = "2013",    Value = 1227.98  },
                    new BarChartDataValue() { Label = "2014",    Value = 1880.62  },
                    new BarChartDataValue() { Label = "2015",    Value = 2053.86  },
                    new BarChartDataValue() { Label = "2016",    Value = 2019.01  },
                    new BarChartDataValue() { Label = "2017",    Value = 2702.83  },
                    new BarChartDataValue() { Label = "2018",    Value = 2717.44  },
                    new BarChartDataValue() { Label = "2019",    Value = 1638.22  },
                    new BarChartDataValue() { Label = "2020",    Value = 1903.7   },
                    new BarChartDataValue() { Label = "2021",    Value = -836.56   }
                }
            });

            datasets.Add(new Dataset()
            {
                Color = Color.FromRgb(0xA3, 0x00, 0x27),
                Data = new List<BarChartDataValue>()
                {
                    new BarChartDataValue() { Label = "14 January", Value = 158.22  },
                    new BarChartDataValue() { Label = "14 February", Value = 226.5   },
                    new BarChartDataValue() { Label = "14 March", Value = 291.19  },
                    new BarChartDataValue() { Label = "14 April", Value = 430.28  },
                    new BarChartDataValue() { Label = "14 May", Value = 140.75  },
                    new BarChartDataValue() { Label = "14 June", Value = 139.82  },
                    new BarChartDataValue() { Label = "14 July", Value = 448.72  },
                    new BarChartDataValue() { Label = "14 August", Value = 149.81  },
                    new BarChartDataValue() { Label = "14 September", Value = 75.84   },
                    new BarChartDataValue() { Label = "14 October", Value = 40.97   },
                    new BarChartDataValue() { Label = "14 November", Value = 140.52  },
                    new BarChartDataValue() { Label = "14 December", Value = 705.66  }
                }
            });
        }


        private UIElement OnGenerateTip(BarChartDataValue value)
        {
            var tip = new StackPanel() { Orientation = Orientation.Vertical };
            tip.Children.Add(new TextBlock() { Text = value.Label, FontWeight = FontWeights.Bold });
            tip.Children.Add(new TextBlock() { Text = value.Value.ToString("C0") });
            return tip;
        }

        private void OnColumnHover(object sender, BarChartDataValue e)
        {

        }

        private void OnColumnClicked(object sender, BarChartDataValue e)
        {
            Toggle();
        }


        private void OnOpenFile(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog od = new Microsoft.Win32.OpenFileDialog();
            od.Filter = "CSV files (*.csv)|*.csv";
            if (od.ShowDialog() == true)
            {
                Open(od.FileName);
            }
        }

        void Open(string fileName)
        {
            try
            {
                using (var reader = new XmlCsvReader(new Uri(fileName), Encoding.UTF8, new NameTable()))
                {
                    reader.ColumnsAsAttributes = true;
                    XDocument doc = XDocument.Load(reader);
                    Dataset ds = ReadDataset(doc);
                    dataset = this.datasets.Count;
                    this.datasets.Add(ds);
                    Toggle();
                }

                Settings.Instance.LastFile = fileName;
            } 
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Loading File", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Dataset ReadDataset(XDocument doc)
        {
            List<BarChartDataValue> data = new List<BarChartDataValue>();
            foreach(var row in doc.Root.Elements())
            {
                var attrs = row.Attributes();
                if (attrs != null)
                {
                    var list = new List<XAttribute>(attrs);
                    if (list.Count > 1)
                    {
                        var label = list[0].Value;
                        var value = list[1].Value;
                        if (double.TryParse(value, out double v))
                        {
                            data.Add(new BarChartDataValue() { Label = label, Value = v });
                        }
                    }
                }
            }
            return new Dataset()
            {
                Color = GetRandomColor(),
                Data = data
            };
        }

        private void OnSettings(object sender, RoutedEventArgs e)
        {
            XamlExtensions.Flyout(AppSettingsPanel);
        }

        private void OnWindowLocationChanged(object sender, EventArgs e)
        {
            var bounds = this.RestoreBounds;
            Settings.Instance.WindowLocation = bounds.TopLeft;
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var bounds = this.RestoreBounds;
            Settings.Instance.WindowSize = bounds.Size;
        }

        private void RestoreSettings()
        {
            Settings settings = Settings.Instance;
            if (settings.WindowLocation.X != 0 && settings.WindowSize.Width != 0 && settings.WindowSize.Height != 0)
            {
                // make sure it is visible on the user's current screen configuration.
                var bounds = new System.Drawing.Rectangle(
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowLocation.X),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowLocation.Y),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowSize.Width),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowSize.Height));
                var screen = System.Windows.Forms.Screen.FromRectangle(bounds);
                bounds.Intersect(screen.WorkingArea);

                this.Left = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.X);
                this.Top = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.Y);
                this.Width = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.Width);
                this.Height = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.Height);
            }

            UpdateTheme();

            this.Visibility = Visibility.Visible;
        }

        void UpdateTheme()
        {
            var theme = ModernWpf.ApplicationTheme.Light;
            switch (Settings.Instance.Theme)
            {
                case AppTheme.Dark:
                    theme = ModernWpf.ApplicationTheme.Dark;
                    break;
                default:
                    break;
            }
            ModernWpf.ThemeManager.Current.ApplicationTheme = theme;
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Theme")
            {
                UpdateTheme();
            }
            saveSettingsPending = true;
            delayedActions.StartDelayedAction("SaveSettings", OnSaveSettings, TimeSpan.FromSeconds(2));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (saveSettingsPending)
            {
                // then we need to do synchronous save and cancel any delayed action.
                OnSaveSettings();
            }

            base.OnClosing(e);
        }

        void OnSaveSettings()
        {
            if (saveSettingsPending)
            {
                saveSettingsPending = false;
                delayedActions.CancelDelayedAction("SaveSettings");
                if (Settings.Instance != null)
                {
                    try
                    {
                        Settings.Instance.Save();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private bool saveSettingsPending;

        private void OnPlay(object sender, RoutedEventArgs e)
        {
            PauseButton.Visibility = Visibility.Visible;
            PlayButton.Visibility = Visibility.Collapsed;
            Toggle();
            delayedActions.StartDelayedAction("play", MoveNext, TimeSpan.FromSeconds(PlaySpeedSeconds));
        }

        private void MoveNext()
        {
            Toggle();
            if (dataset == 0)
            {
                OnRotate(this, null);
            }
            delayedActions.StartDelayedAction("play", MoveNext, TimeSpan.FromSeconds(PlaySpeedSeconds));
        }

        private void OnPause(object sender, RoutedEventArgs e)
        {
            delayedActions.CancelDelayedAction("play");
            PauseButton.Visibility = Visibility.Collapsed;
            PlayButton.Visibility = Visibility.Visible;
        }

        private void OnRefresh(object sender, RoutedEventArgs e)
        {
            var data = Chart.Series;
            Chart.Series = null;
            Chart.Series = data;
        }

        private void OnRotate(object sender, RoutedEventArgs e)
        {
            Chart.Orientation = Chart.Orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
        }

        private Color GetRandomColor()
        {
            return Color.FromRgb((byte)rand.Next(80, 200), (byte)rand.Next(80, 200), (byte)rand.Next(80, 200));
        }
    }
}
