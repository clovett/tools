using Avalonia.Controls;
using BarChart.Utilities;

namespace BarChart.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            UiDispatcher.Initialize();
            InitializeComponent();
        }
    }
}