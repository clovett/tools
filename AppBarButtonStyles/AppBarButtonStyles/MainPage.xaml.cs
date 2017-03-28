using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AppBarButtonStyles
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            List<ButtonItem> items = new List<AppBarButtonStyles.ButtonItem>();
            for (int i = 0xe000; i < 0xe500; i++)
            {
                items.Add(new ButtonItem() { Name = i.ToString("x"), Symbol = Convert.ToChar(i).ToString() });
            }
            ButtonGrid.ItemsSource = items;
        }
    }

    public class ButtonItem
    {
        public string Name { get; set; }
        public string Symbol { get; set; }
    }

}
