using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using System.Windows.Threading;
using TreeMaps;

namespace FileTreeMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ToolTip _toolTip;
        DelayedActions tooltipDelay = new DelayedActions();
        TreeBuilder builder = new TreeBuilder();

        public readonly static RoutedUICommand OpenContainingFolderCommand = new RoutedUICommand("Open Containing Folder", "OpenContainingFolderCommand", typeof(MainWindow));

        public MainWindow()
        {
            UiDispatcher.Initialize(this.Dispatcher);
            InitializeComponent();
            this.TreeMap.SelectionChanged += OnTreeMapSelectionChanged;
            this.Loaded += OnMainWindowLoaded;
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            App app = (App)App.Current;
            if (app.Arguments != null && app.Arguments.Length > 0)
            {
                LocationTextBox.Text = app.Arguments[0];
                Analyze();
            }
        }

        private void OnTreeMapSelectionChanged(object sender, TreeMaps.Controls.TreeNodeSelectedEventArgs e)
        {
            if (e.Selection != null && e.Selection.Children != null)
            {
                double total = (from n in e.Selection.Children select n.Size).Sum();
                StatusMessage.Text = total + " lines";
            }
        }

        private void OnAnalyzeClick(object sender, RoutedEventArgs e)
        {
            Analyze();
        }

        bool busy;

        void Analyze()
        { 
            if (busy)
            {
                return;
            }
            busy = true;

            Help.Visibility = Visibility.Collapsed;
            this.TreeMap.TreeData = null;
            string path = LocationTextBox.Text;

            Task.Run(() => {

                SetProgress(0, 1, 0, true); // initially indeterminate
                StartProgress();

                IEnumerable<string> files = null;
                if (File.Exists(path))
                {
                    // then it is a build log file
                    files = builder.FindFilesInLogFile(path);
                }
                else if (Directory.Exists(path))
                {
                    // analyze directory
                    files = builder.FindFilesInDirectory(path);
                }
                else
                {
                    UiDispatcher.RunOnUIThread(() =>
                    {
                        ShowError(string.Format("'{0}' does not exist", path), "Folder not found");
                    });
                }
                if (files != null)
                {
                    var tree = builder.AnalyzeFiles(files, SetProgress);
                    UiDispatcher.RunOnUIThread(() => { this.TreeMap.TreeData = tree; });
                }
                StopProgress();
                busy = false;
            });

        }

        DispatcherTimer timer;
        int min;
        int max;
        int progress;
        bool indeterminate;

        void StartProgress()
        {
            Dispatcher.Invoke(() =>
            {
                StopProgress();
                timer = new DispatcherTimer();
                timer.Tick += OnTimerTick;
                timer.Interval = TimeSpan.FromMilliseconds(20);
                timer.Start();
            });
        }

        void StopProgress()
        {
            Dispatcher.Invoke(() => { 
                if (timer != null)
                {
                    timer.Stop();
                    timer.Tick -= OnTimerTick;
                    timer = null;
                }
                Progress.Visibility = Visibility.Collapsed;
            });
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            Progress.Visibility = (progress < max) ? Visibility.Visible : Visibility.Collapsed;
            if (progress < max)
            {
                Progress.Minimum = min;
                Progress.Maximum = max;
                Progress.Value = progress;
                Progress.IsIndeterminate = indeterminate;
            }
        }

        void SetProgress(int min, int max, int progress, bool indeterminate)
        {
            this.min = min;
            this.max = max;
            this.progress = progress;
            this.indeterminate = indeterminate;
        }


        private void ShowError(string msg, string caption)
        {
            MessageBox.Show(msg, caption, MessageBoxButton.OK, MessageBoxImage.Error);           
        }


        private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Analyze();
            }
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            if (TreeMap.ContextMenu.IsOpen)
            {
                return;
            }
            Point pos = e.GetPosition(TreeMap);
            if (_toolTip != null)
            {
                _toolTip.IsOpen = false;
            }
            tooltipDelay.StartDelayedAction("ShowTooltip", () =>
            {
                if (!TreeMap.ContextMenu.IsOpen)
                {
                    ShowToolTip(pos);
                }
            }, TimeSpan.FromMilliseconds(500));
            base.OnPreviewMouseMove(e);
        }

        protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(TreeMap);
            selected = HitTreeNode(pos);
            base.OnPreviewMouseRightButtonDown(e);

        }

        TreeNodeData selected;

        TreeNodeData HitTreeNode(Point pos)
        {
            HitTestResult result = VisualTreeHelper.HitTest(TreeMap, pos);
            if (result != null)
            {
                DependencyObject hit = result.VisualHit;
                if (hit != null)
                {
                    Grid g = hit.FindAncestor<Grid>();
                    if (g != null)
                    {
                        TreeNodeData data = g.DataContext as TreeNodeData;
                        if (data != null)
                        {
                            return data;
                        }
                    }
                }
            }
            return null;
        }

        private void ShowToolTip(Point pos)
        {
            TreeNodeData data = HitTreeNode(pos);
            if (data != null)
            {
                if (_toolTip == null)
                {
                    _toolTip = new ToolTip();
                    _toolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
                    _toolTip.MaxWidth = SystemParameters.PrimaryScreenWidth / 3;
                    TreeMap.ToolTip = _toolTip;
                }
                _toolTip.Content = data.Label + " (" + data.Size + ")\n" + data.Id;
                _toolTip.IsOpen = true;
            }
        }

        private void CanOpenFile(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = selected != null && File.Exists(selected.Id);
        }

        private void OnOpenFile(object sender, ExecutedRoutedEventArgs e)
        {
            if (selected != null && File.Exists(selected.Id))
            {
                OpenUrl(selected.Id);
            }

        }

        private void CanOpenFolder(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = selected != null;
        }

        private void OnOpenFolder(object sender, ExecutedRoutedEventArgs e)
        {
            if (selected != null)
            {
                string path = selected.Id;
                if (File.Exists(path))
                {
                    path = System.IO.Path.GetDirectoryName(path);
                }
                OpenUrl(path);
            }
        }


        public static void OpenUrl(string url)
        {
            const int SW_SHOWNORMAL = 1;
            int rc = ShellExecute(IntPtr.Zero, "open", url, null, "", SW_SHOWNORMAL);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1401:PInvokesShouldNotBeVisible"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "2"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "4"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "3"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "1"), DllImport("Shell32.dll", EntryPoint = "ShellExecuteA",
            SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall)]
        private static extern int ShellExecute(IntPtr handle, string verb, string file, string args, string dir, int show);

    }
}
