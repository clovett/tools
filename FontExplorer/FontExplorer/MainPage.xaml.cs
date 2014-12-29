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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace FontExplorer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        FontFamily font;

        public MainPage()
        {
            this.InitializeComponent();

            List<string> fonts = new List<string>(FontEnumerator.GetFonts());
            fonts.Sort();
            FontCombo.ItemsSource = fonts;
            FontCombo.SelectedIndex = 0;

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
        }

        void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.PageDown)
            {
                int ch = GetCurrentPage();
                ch += 0x100;
                if (ch > 0xfE00)
                {
                    ch = 0xfe00;
                }
                SetCurrentPage(ch);
            }
            else if (args.VirtualKey == Windows.System.VirtualKey.PageUp)
            {
                int ch = GetCurrentPage();
                ch -= 0x100;
                if (ch < 0)
                {
                    ch = 0;
                }
                SetCurrentPage(ch);
            }
        }

        private void OnFontSelected(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                font = new FontFamily((string)FontCombo.SelectedItem);
                UpdatePage();
            }
            catch
            {
            }
        }

        private void OnTextKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                UpdatePage();
            }
        }

        int GetCurrentPage()
        {
            string text = StartTextBox.Text.Trim();
            if (text.StartsWith("0x"))
            {
                text = text.Substring(2);
            }
            int ch = 32;
            int.TryParse(text, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.CurrentCulture, out ch);

            return ch;
        }

        void SetCurrentPage(int page)
        {
            StartTextBox.Text = page.ToString("x");
            UpdatePage();
        }

        void UpdatePage()
        {
            List<FontItem> list = new List<FontItem>();
            int ch = GetCurrentPage();
            for (int i = ch; i < ch + 0x100; i++)
            {
                list.Add(new FontItem()
                {
                    Symbol = Convert.ToChar(i).ToString(),
                    Label = i.ToString("x"),
                    Font = this.font
                });
            }

            ResultList.ItemsSource = list;
            ResultList.ScrollIntoView(list[0]);
        }

        private void OnListViewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            
        }
    }
}
