using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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

namespace HtmlSnapshotMaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool stopped;
        long index = 0;
        Settings settings;

        public MainWindow()
        {
            InitializeComponent();
            settings = Settings.Instance;
            if (this.settings.IntervalSeconds == 0)
            {
                this.settings.IntervalSeconds = 60;
            }

            if (string.IsNullOrEmpty(settings.Directory))
            {
                settings.Directory = System.IO.Directory.GetCurrentDirectory();
            }
            if (!string.IsNullOrEmpty(settings.Url))
            {
                Browser.Address = settings.Url;
            } 
            else
            {
                settings.Url = Browser.Address;
            }
            settings.Save();

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GetNextIndex();
            Task.Run(TakeSnapshots);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.stopped = true;
            base.OnClosing(e);
        }

        private void GetNextIndex()
        {
            foreach (string file in System.IO.Directory.GetFiles(settings.Directory))
            {
                string filename = System.IO.Path.GetFileName(file);
                if (filename.StartsWith("index") && filename.EndsWith(".png"))
                {
                    string s = file.Substring(5, filename.Length - 4);
                    if (long.TryParse(s, out long i) && i > index)
                    {
                        index = i + 1;
                    }
                }
            }
        }

        private async Task TakeSnapshots()
        {
            try
            {
                while (!this.stopped)
                {
                    if (this.settings.IntervalSeconds> 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(this.settings.IntervalSeconds));
                    }

                    if (this.stopped)
                    {
                        return;
                    }

                    index++;

                    // this part has to happen back on UI thread.
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        RenderWindowToFile(System.IO.Path.Combine(settings.Directory, "image" + index + ".png"));
                    }));

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Creating Screenshot", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return;
        }
        
        void RenderWindowToFile(string filename)
        {
            int width = (int)this.Browser.ActualWidth;
            int height = (int)this.Browser.ActualHeight;
            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(this.Browser);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                encoder.Save(fs);
            }
        }
    }
}
