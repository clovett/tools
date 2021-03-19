using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ColorContrast
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // See https://www.w3.org/WAI/WCAG21/Understanding/contrast-minimum.html
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Rectangle swatch = null;
            TextBox box = (TextBox)sender;
            if (box.Name == "Color1")
            {
                swatch = Swatch1;
            }
            else
            {
                swatch = Swatch2;
            }
            try
            {
                Color c = (Color)ColorConverter.ConvertFromString(box.Text);
                swatch.Fill = new SolidColorBrush(c);
                ComputeContrast();
            } 
            catch
            {
                string[] parts = box.Text.Split(",");
                int r = 0, g = 0, b = 0;
                if (parts.Length > 0)
                {
                    int.TryParse(parts[0], out r);
                }
                if (parts.Length > 1)
                {
                    int.TryParse(parts[1], out g);
                }
                if (parts.Length > 2)
                {
                    int.TryParse(parts[2], out b);
                }
                swatch.Fill = new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
                ComputeContrast();
            }
        }

        private void ComputeContrast()
        {
            // (L1 + 0.05) / (L2 + 0.05) where
            // L1 is the relative luminance of the lighter of the colors, and
            // L2 is the relative luminance of the darker of the colors.
            HlsColor c1 = new HlsColor(GetColor(Swatch1));
            HlsColor c2 = new HlsColor(GetColor(Swatch2));
            float l1 = c1.Luminance;
            float l2 = c2.Luminance;
            if (l1 < l2)
            {
                float t = l1;
                l1 = l2;
                l2 = t;
            }

            float contrast = (l1 + 0.05f) / (l2 + 0.05f);

            Contrast.Text = contrast.ToString();
        }

        Color GetColor(Rectangle r)
        {
            var s = r.Fill as SolidColorBrush;
            if (s != null)
            {
                return s.Color;
            }
            return Colors.Transparent;
        }

        private void OnLinkClick(object sender, RoutedEventArgs e)
        {
            Hyperlink link = (Hyperlink)sender;
            var uri = link.NavigateUri;
            OpenUrl(uri);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "rc")]
        public static void OpenUrl(Uri url)
        {
            var cwd = System.IO.Directory.GetCurrentDirectory();
            // todo: support showing embedded pack:// resources in a popup page (could be useful for help content).
            const int SW_SHOWNORMAL = 1;
            int rc = ShellExecute(IntPtr.Zero, "open", url.AbsoluteUri, null, cwd, SW_SHOWNORMAL);
        }

        [DllImport("Shell32.dll", EntryPoint = "ShellExecuteA",
            SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int ShellExecute(IntPtr handle, string verb, string file,
            string args, string dir, int show);
    }
}
