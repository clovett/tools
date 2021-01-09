using LovettSoftware.Utilities;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MyFitness.Controls
{
    /// <summary>
    /// Interaction logic for AppSettings.xaml
    /// </summary>
    public partial class AppSettings : UserControl
    {
        public AppSettings()
        {
            InitializeComponent();

            string version = this.GetType().Assembly.GetName().Version.ToString();
            var pattern = VersionPrompt.Text;
            VersionPrompt.Text = string.Format(pattern, version);

            List<AppTheme> items = new List<AppTheme>();
            items.Add(AppTheme.Light);
            items.Add(AppTheme.Dark);
            ThemeSelection.ItemsSource = items;
            ThemeSelection.SelectedItem = Settings.Instance.Theme;
            ThemeSelection.SelectionChanged += ThemeSelection_SelectionChanged;
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
