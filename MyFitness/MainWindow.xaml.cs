using LovettSoftware.Utilities;
using Microsoft.Win32;
using MyFitness.Model;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace MyFitness
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DelayedActions delayedActions = new DelayedActions();
        CalendarModel model = new CalendarModel();

        public static RoutedUICommand ClearCommand = new RoutedUICommand("Clear", "ClearCommand", typeof(MainWindow));
        public static RoutedUICommand SettingsCommand = new RoutedUICommand("Settings", "SettingsCommand", typeof(MainWindow));
        public static RoutedUICommand NextCommand = new RoutedUICommand("Next", "NextCommand", typeof(MainWindow));
        public static RoutedUICommand PreviousCommand = new RoutedUICommand("Previous", "PreviousCommand", typeof(MainWindow));

        public MainWindow()
        {
            InitializeComponent();
            RestoreSettings();

            UiDispatcher.Initialize();

            this.SizeChanged += OnWindowSizeChanged;
            this.LocationChanged += OnWindowLocationChanged;
            
            Settings.Instance.PropertyChanged += OnSettingsChanged;
            this.Loaded += OnWindowLoaded;
            this.model.PropertyChanged += OnModelPropertyChanged;            
        }

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsDirty" && model.IsDirty)
            {
                saveModelPending = true;
                delayedActions.StartDelayedAction("SaveModel", OnSaveModel, TimeSpan.FromSeconds(2));
            }
        }

        private void OnSaveModel()
        {
            if (!string.IsNullOrEmpty(Settings.Instance.FileName))
            {
                delayedActions.CancelDelayedAction("SaveModel");
                saveModelPending = false;
                model.Save(Settings.Instance.FileName);
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            OnLoadModel();
        }

        private async void OnLoadModel() 
        {
            if (!string.IsNullOrEmpty(Settings.Instance.FileName) && System.IO.File.Exists(Settings.Instance.FileName))
            {
                await model.LoadAsync(Settings.Instance.FileName);
            }

            MyCalendar.DataContext = model.GetOrCreateCurrentMonth();
            InvalidateCommandState();
        }

        private void OnOpenFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.CheckFileExists = true;
            if (fd.ShowDialog() == true)
            {
                Settings.Instance.FileName = fd.FileName;
            }
        }

        private void OnSaveFile(object sender, RoutedEventArgs e)
        {
            var filename = Settings.Instance.FileName;
            if (string.IsNullOrEmpty(filename))
            {
                OpenFileDialog fd = new OpenFileDialog();
                fd.CheckFileExists = false;
                if (fd.ShowDialog() == true)
                {
                    filename = fd.FileName;
                }
            }

            try
            {
                model.Save(filename);
                Settings.Instance.FileName = filename;
            } 
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnClear(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to start a new file from scratch?", "Confirm New File", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                FlushModel();
                Settings.Instance.FileName = null;
                this.model.PropertyChanged -= OnModelPropertyChanged;
                this.model = new CalendarModel();
                this.model.PropertyChanged += OnModelPropertyChanged;
                MyCalendar.DataContext = this.model.GetOrCreateCurrentMonth();
                InvalidateCommandState();
            }
        }

        void InvalidateCommandState()
        {
            CalendarMonth current = MyCalendar.DataContext as CalendarMonth;
            this.Title = "My Fitness - " + current.Start.ToString("MMMM");
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
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
            this.Visibility = Visibility.Visible;
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FileName")
            {
                OnLoadModel();
            }

            saveSettingsPending = true;
            delayedActions.StartDelayedAction("SaveSettings", OnSaveSettings, TimeSpan.FromSeconds(2));
        }

        private void FlushModel()
        {
            if (saveSettingsPending)
            {
                // then we need to do synchronous save and cancel any delayed action.
                OnSaveSettings();
            }
            if (saveModelPending)
            {
                OnSaveModel();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            FlushModel();
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

        private bool saveModelPending;
        private bool saveSettingsPending;

        private void OnNextMonth(object sender, ExecutedRoutedEventArgs e)
        {
            CalendarMonth current = MyCalendar.DataContext as CalendarMonth;
            int index = this.model.Months.IndexOf(current);
            if (index < this.model.Months.Count - 1)
            {
                MyCalendar.DataContext = this.model.Months[index + 1];
                InvalidateCommandState();
            }
        }

        private void OnPreviousMonth(object sender, ExecutedRoutedEventArgs e)
        {
            CalendarMonth current = MyCalendar.DataContext as CalendarMonth;
            int index = this.model.Months.IndexOf(current);
            if (index > 0)
            {
                MyCalendar.DataContext = this.model.Months[index - 1];
                InvalidateCommandState();
            }
        }

        private void HasNextMonth(object sender, CanExecuteRoutedEventArgs e)
        {
            if (MyCalendar != null)
            {
                CalendarMonth current = MyCalendar.DataContext as CalendarMonth;
                int index = this.model.Months.IndexOf(current);
                e.CanExecute = index < this.model.Months.Count - 1;
            }
        }

        private void HasPreviousMonth(object sender, CanExecuteRoutedEventArgs e)
        {
            if (MyCalendar != null)
            {
                CalendarMonth current = MyCalendar.DataContext as CalendarMonth;
                int index = this.model.Months.IndexOf(current);
                e.CanExecute = index > 0;
            }
        }
    }
}
