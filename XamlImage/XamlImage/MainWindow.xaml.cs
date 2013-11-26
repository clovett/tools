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
using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Markup;
using System.Xml;
using System.Xml.Linq;

namespace XamlImage
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


        private void OnExit(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        static XNamespace winfx = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml");
        static XNamespace wpf = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml/presentation");

        private void OnPaste(object sender, ExecutedRoutedEventArgs args)
        {
            try
            {
                string text = Clipboard.GetText();

                // make sure the namespaces are defined...
                text = "<Grid xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " + 
                              "xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>"
                        + text + "</Grid>";

                XDocument doc = XDocument.Parse(text);
                XElement root = doc.Root;

                foreach (XElement e in doc.Descendants())
                {
                    
                    XAttribute cls = e.Attribute(winfx + "Class");
                    if (cls != null)
                    {
                        cls.Remove(); 
                    }
                }

                using (XmlReader reader = XmlReader.Create(new StringReader(doc.ToString())))
                {
                    UIElement element = XamlReader.Load(reader) as UIElement;
                    RootContent.Children.Clear();
                    RootContent.Children.Add(element);
                }

            }
            catch (Exception ex)
            {
                TextBlock message = new TextBlock();
                message.Text = ex.Message;
                message.TextWrapping = TextWrapping.Wrap;
                message.Margin = new Thickness(20);
                RootContent.Children.Clear();
                RootContent.Children.Add(message);
            }
        }

        private void OnSave(object sender, ExecutedRoutedEventArgs e)
        {

            SaveFileDialog sd = new SaveFileDialog();
            sd.Filter = "PNG Files (*.png)|*.png";
            if (sd.ShowDialog() == true)
            {
                SaveFile(sd.FileName);
            }
        }

        private void SaveFile(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();

                double w = RootContent.ActualWidth;
                double h = RootContent.ActualHeight;

                RenderTargetBitmap bitmap = new RenderTargetBitmap((int)w, (int)h, 96, 95, PixelFormats.Pbgra32);
                bitmap.Render(RootContent);
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                encoder.Save(stream);
            }

            ShowFile(filename);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "rc")]
        public static void ShowFile(string path)
        {
            string dir = System.IO.Path.GetDirectoryName(path);            
            // todo: support showing embedded pack:// resources in a popup page (could be useful for help content).
            const int SW_SHOWNORMAL = 1;
            int rc = ShellExecute(IntPtr.Zero, "open", path, null, dir, SW_SHOWNORMAL);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "1"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "2"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "4"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "3"), 
        DllImport("Shell32.dll", EntryPoint = "ShellExecuteA",
            SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall)]
        private static extern int ShellExecute(IntPtr handle, string verb, string file,
            string args, string dir, int show);

        private void OnBlackBackground(object sender, RoutedEventArgs e)
        {
            this.Background = Brushes.Black;
            this.Foreground = Brushes.White;
        }

        private void OnWhiteBackground(object sender, RoutedEventArgs e)
        {
            this.Background = Brushes.White;
            this.Foreground = Brushes.Black;
        }
        
    }
}
