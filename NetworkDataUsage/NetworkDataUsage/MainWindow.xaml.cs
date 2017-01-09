using NetworkDataUsage.Controls;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using NetworkDataUsage.Utilities;
using System;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NetworkDataUsage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WindowPositionTracker tracker = new WindowPositionTracker();

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += OnWindowLoaded;
            tracker.Open(this);
            LoginFlyout.OkCancelClick += LoginFlyout_OkCancelClick;
        }

        private void LoginFlyout_OkCancelClick(object sender, EventArgs e)
        {
            LoginFlyout.Visibility = Visibility.Collapsed;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Flyout(LoginFlyout);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            var balloon = new ToastMessage();
            balloon.MaxWidth = 300;
            balloon.Message = "This app is still running, collecting data in the background.  You can access it from the system tray.";
            tb.ShowCustomBalloon(balloon, PopupAnimation.Slide, 12000);
            tb.Visibility = Visibility.Visible;
            this.Visibility = Visibility.Hidden;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            tracker.Close();
        }

        private void Flyout(FrameworkElement e)
        {
            e.Visibility = Visibility.Visible;
            TranslateTransform transform = new TranslateTransform(e.Width, 0);
            e.RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(0, new Duration(TimeSpan.FromSeconds(0.2)))
                {
                    EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut }
                });
        }
    }
}
