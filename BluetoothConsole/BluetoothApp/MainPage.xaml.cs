using BluetoothConsole;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BluetoothApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        RichTextLog log;
        SensorTag tag;
        public MainPage()
        {
            this.InitializeComponent();
            log = new BluetoothApp.MainPage.RichTextLog(this.TextOutput, this.TextScroller);
            tag = new SensorTag(log);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            tag.FindPairedDevices();
        }

        class RichTextLog : ILog
        {
            RichTextBlock block;
            ScrollViewer scroller;

            public RichTextLog(RichTextBlock block, ScrollViewer scroller)
            {
                this.block = block;
                this.scroller = scroller;
            }

            public void WriteLine(string line)
            {
                var nowait = block.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
                {
                    if (this.block.Blocks.Count == 0)
                    {
                        this.block.Blocks.Add(new Paragraph());
                    }
                    Paragraph p = (Paragraph)this.block.Blocks[0];
                    p.Inlines.Add(new Run() { Text = line });
                    p.Inlines.Add(new LineBreak());
                }));
            }
        }
    }
}
