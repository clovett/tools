using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using VideoThumbnailMaker.Utilities;

namespace VideoThumbnailMaker.Controls
{
    /// <summary>
    /// Interaction logic for AppSettings.xaml
    /// </summary>
    public partial class AppSettings : UserControl
    {
        public AppSettings()
        {
            InitializeComponent();

            List<AppTheme> items = new List<AppTheme>();
            items.Add(AppTheme.Light);
            items.Add(AppTheme.Dark);
            ThemeSelection.ItemsSource = items;
            ThemeSelection.SelectionChanged += ThemeSelection_SelectionChanged;
            var settings = Settings.Instance;
            if (settings == null)
            {
                Settings.Loaded += OnSettingsLoaded;
            }
            else
            {
                InitializeSettings();
            }
        }

        private void OnSettingsLoaded(object sender, System.EventArgs e)
        {
            InitializeSettings();
            Settings.Loaded -= OnSettingsLoaded;
        }

        void InitializeSettings()
        {
            var settings = Settings.Instance;
            ThemeSelection.SelectedItem = settings.Theme;
            this.DataContext = settings;
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
        }

        private void ThemeSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                Settings.Instance.Theme = (AppTheme)e.AddedItems[0];
            }
        }

    }
}
