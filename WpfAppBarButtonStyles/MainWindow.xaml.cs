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

namespace WpfAppBarButtonStyles
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            List<ButtonItem> items = new List<ButtonItem>();
            for (int i = 0xe000; i < 0xe500; i++)
            {
                items.Add(new ButtonItem() { Name = i.ToString("x"), Symbol = Convert.ToChar(i).ToString() });
            }
            ButtonGrid.ItemsSource = items;
        }

        private void OnCopySymbol(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                ButtonItem item = button.DataContext as ButtonItem;
                var hex = Convert.ToInt32(item.Symbol[0]).ToString("x");
                Clipboard.SetText(item.Symbol + ": 0x" + hex);
            }
        }
    }

    public class ButtonItem
    {
        public string Name { get; set; }
        public string Symbol { get; set; }
    }

}
