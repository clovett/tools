using RssLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Walkabout.Utilities;

namespace RssMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FeedMonitor monitor;
        DelayedActions actions = new DelayedActions();

        public MainWindow()
        {
            UiDispatcher.Initialize(this.Dispatcher);
            monitor = new FeedMonitor();
            InitializeComponent();
            this.DataContext = monitor.Settings;

            StringBuilder sb = new StringBuilder();
            foreach (var i in monitor.Settings.FeedInfo)
            {
                sb.AppendLine(i.Url);
            }
            RSSFeeds.Text = sb.ToString();
            RSSFeeds.LostFocus += RSSFeeds_LostFocus;
            this.Loaded += MainWindow_Loaded;

            var task = Microsoft.Win32.TaskScheduler.TaskService.Instance.FindTask("RssMonitor");
            if (task == null)
            {
                var program = typeof(RssMonitorConsole.Program).Assembly.Location;
                Microsoft.Win32.TaskScheduler.TaskService.Instance.AddTask("RssMonitor",
                    Microsoft.Win32.TaskScheduler.QuickTriggerType.Daily, program);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                var updated = monitor.GetUpdatedFeeds();
                UiDispatcher.RunOnUIThread(() =>
                {
                    DisplayUpdatedFeeds(updated);
                });
            });
        }

        private void DisplayUpdatedFeeds(List<FeedInfo> updated)
        {
            foreach (var info in updated)
            {
                if (!string.IsNullOrEmpty(info.Error))
                {
                    StatusText.Text = info.Error;
                    return;
                }
            }
            StatusText.Text = string.Format("Found {0} updated feeds", updated.Count);
            Task.Run(() =>
            {
                monitor.SendEmails(updated);
            });
        }

        private void OnFieldChanged(object sender, TextChangedEventArgs e)
        {
            actions.StartDelayedAction("save", new Action(monitor.Settings.SaveSettings), TimeSpan.FromSeconds(0.5));
        }

        private void RSSFeeds_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            var lines = box.Text.Split('\n');
            var feeds = new List<string>();
            for (int i = 0, n = lines.Length; i < n; i++)
            {
                feeds.Add(lines[i].Trim());
            }
            MergeNewFeeds(feeds);
        }

        private void MergeNewFeeds(List<string> feeds)
        { 

            List<FeedInfo> used = new List<FeedInfo>();
            foreach(var line in feeds)
            {
                bool found = false;
                foreach (var item in monitor.Settings.FeedInfo)
                {
                    if (item.Url == line)
                    {
                        found = true;
                        used.Add(item);
                    }
                }
                if (!found)
                {
                    FeedInfo item = new FeedInfo() { Url = line };
                    monitor.Settings.FeedInfo.Add(item);
                    used.Add(item);
                }
            }

            // remove unused
            foreach (FeedInfo item in monitor.Settings.FeedInfo.ToArray())
            {
                if (!used.Contains(item))
                {
                    monitor.Settings.FeedInfo.Remove(item);
                }
            }
            
            actions.StartDelayedAction("save", new Action(monitor.Settings.SaveSettings), TimeSpan.FromSeconds(0.5));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            actions.CancelDelayedAction("save");
            monitor.Settings.SaveSettings();
            base.OnClosing(e);
        }
    }
}
