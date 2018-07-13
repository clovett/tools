using System.Windows;

namespace FidelityShowDetails
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnExpandAll(object sender, RoutedEventArgs args)
        {
            AutomationExpander.ExpandAll();
        }

    }
}
