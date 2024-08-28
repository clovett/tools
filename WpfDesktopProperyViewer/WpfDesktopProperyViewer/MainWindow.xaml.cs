using LovettSoftware.Utilities;
using System.Net.Sockets;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfDesktopProperyViewer.Utilities;
using System.ComponentModel;
using System.Windows.Interop;

namespace WpfDesktopProperyViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WindowMoveGesture gesture;
        private Model model = new Model();

        public MainWindow()
        {
            UiDispatcher.Initialize();
            InitializeComponent();
            this.Top = 200;
            this.Left = 300;
            this.Loaded += OnMainWindowLoaded;
            this.gesture = new WindowMoveGesture(this);
            NativeMethods.SetBottomMost(new WindowInteropHelper(this).Handle);
        }

        string GetIpAddress()
        {
            IPAddress host = IPAddress.None;
            foreach (IPAddress ip in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "No IP Address Found";
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.model = Model.Load();
            if (model.Entities.Count == 0)
            {
                model.Entities.Add(new Entity() { Name = "COMPUTERNAME", Value = Environment.GetEnvironmentVariable("COMPUTERNAME") });
                model.Entities.Add(new Entity() { Name = "IP", Value = GetIpAddress() });
            }
            model.Entities = this.model.Entities;            
            if (this.model.Left != 0){
                this.Left = this.model.Left;
            }
            if (this.model.Top != 0){
                this.Top = this.model.Top;
            }
            PropertyView.ItemsSource = model.Entities;
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            this.Background = new SolidColorBrush(Color.FromArgb(80, 20, 20, 50));
            this.CloseBox.BeginAnimation(Button.OpacityProperty, new DoubleAnimation()
            {
                To = 1,
                Duration = new Duration(TimeSpan.FromSeconds(0.5))
            });
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            this.Background = new SolidColorBrush(Colors.Transparent);
            this.CloseBox.BeginAnimation(Button.OpacityProperty, new DoubleAnimation()
            {
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(0.5))
            });
            base.OnMouseLeave(e);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            this.PropertyView.UnselectAll();
            base.OnMouseLeftButtonDown(e);
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.model.Left = this.Left;
            this.model.Top = this.Top;
            this.model.Save();
        }
    }
}