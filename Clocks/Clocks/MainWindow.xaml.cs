using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Clocks.Utilities;
using Clocks.Storage;
using System.ComponentModel;

namespace Clocks
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int count = 0;
        const int TotalQuestions = 25;
        Random rand = new Random();
        Data data;
        bool paused = true;

        public MainWindow()
        {
            UiDispatcher.Initialize();

            InitializeComponent();

            TimeEntry.Completed += OnTimeEntryCompleted;

            this.Loaded += MainWindow_Loaded;

            UpdateButtons();

            this.Visibility = Visibility.Hidden;
            RestorePosition();
        }

        private void UpdateButtons()
        {
            PauseButton.Visibility = (paused ? Visibility.Collapsed : Visibility.Visible);
            PlayButton.Visibility = (!paused ? Visibility.Collapsed : Visibility.Visible);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                data = Data.LoadData();
                foreach (var session in data.History.ToArray())
                {
                    if (session.Times.Count == 0)
                    {
                        data.History.Remove(session);
                    }
                }
                Graph.Data = data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

        }
        
        DateTime answer;

        private void ShowNext()
        {
            if (paused)
            {
                return;
            }
            if (count == TotalQuestions)
            {
                // done!
                Pause();
            }
            else
            {
                count++;
                var now = DateTime.Now;
                answer = new DateTime(now.Year, now.Month, now.Day, rand.Next(1, 13), rand.Next(0, 60), rand.Next(0, 60));
                Face.Time = answer;

                Progress.Content = count + " of " + TotalQuestions;
                TimeEntry.Reset();
                watch.Start();
            }
        }
        private void OnTimeEntryCompleted(object sender, EventArgs e)
        {
            watch.Stop();
            TimeSpan elapsed = watch.Elapsed;
            watch.Reset();

            bool error = false;

            Test record = new Test()
            {
                Answer = answer,
                ElapsedMilliseconds = (ulong)elapsed.TotalMilliseconds
            };

            if (TimeEntry.Time.HasValue)
            {
                DateTime entry = TimeEntry.Time.Value;
                record.Entry = entry;
                if (entry == answer)
                {
                    data.Current.Correct++;
                    TimeEntry.Correct();
                }
                else
                {
                    error = true;
                }
            }
            else
            {
                error = true;
            }

            this.data.Current.Times.Add(record);

            if (error)
            { 
                // show error.
                TimeEntry.ShowError(answer);
            }
            delayedActions.StartDelayedAction("Next", ShowNext, TimeSpan.FromSeconds(1));
        }

        DelayedActions delayedActions = new DelayedActions();
        Stopwatch watch = new Stopwatch();

        private void OnPlay(object sender, RoutedEventArgs e)
        {
            count = 0;
            if (data.Current == null)
            {
                if (data.History.Count > 0)
                {
                    Session last = data.History.Last();
                    if (last.Times.Count < TotalQuestions)
                    {
                        data.Current = last;
                        count = last.Times.Count;
                    }
                }
                if (data.Current == null)
                {
                    data.Current = data.AddNew();
                }
            }
            paused = false;
            Graph.Visibility = Visibility.Collapsed;
            ShowNext();
        }

        private void OnPause(object sender, RoutedEventArgs e)
        {
            Pause();
        }

        void Pause()
        {
            paused = true;
            TimeEntry.HidePopups();
            Graph.Visibility = Visibility.Visible;
            Graph.Data = data;
        }

        private void OnClear(object sender, RoutedEventArgs e)
        {
            data.Clear();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            data.SaveData();
            base.OnClosing(e);
        }

        private void OnWindowLocationChanged(object sender, EventArgs e)
        {
            delayedActions.StartDelayedAction("SaveWindowLocation", SavePosition, TimeSpan.FromMilliseconds(1000));
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            delayedActions.StartDelayedAction("SaveWindowLocation", SavePosition, TimeSpan.FromMilliseconds(1000));
        }

        private async void RestorePosition()
        {
            this.SizeChanged -= OnWindowSizeChanged;
            this.LocationChanged -= OnWindowLocationChanged;
            Settings settings = await ((App)App.Current).LoadSettings();
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
            this.Visibility = Visibility.Visible;
            this.SizeChanged += OnWindowSizeChanged;
            this.LocationChanged += OnWindowLocationChanged;
        }

        async void SavePosition()
        {
            var bounds = this.RestoreBounds;

            Settings settings = await ((App)App.Current).LoadSettings();
            settings.WindowLocation = bounds.TopLeft;
            settings.WindowSize = bounds.Size;
            await settings.SaveAsync();
        }

    }
}
