using OutlookSync.Model;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OutlookSync.Controls
{
    /// <summary>
    /// Interaction logic for PhoneControl.xaml
    /// </summary>
    public partial class PhoneControl : UserControl
    {
        ConnectedPhone phone;

        public PhoneControl()
        {
            InitializeComponent();
            this.DataContextChanged += PhoneControl_DataContextChanged;
            this.TrustedPhoneImage.Visibility = System.Windows.Visibility.Collapsed;
            this.InSyncImage.Visibility = System.Windows.Visibility.Collapsed;
        }

        void PhoneControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (phone != null)
            {
                phone.PropertyChanged -= OnPhonePropertyChanged;
            }
            phone = (ConnectedPhone)e.NewValue;
            if (phone != null)
            {
                UpdateImages();
                phone.PropertyChanged += OnPhonePropertyChanged;
                ConnectButton.Visibility = (phone.Allowed ? Visibility.Collapsed : System.Windows.Visibility.Visible);
            }
        }

        private void OnPhonePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (phone != null)
            {
                switch (e.PropertyName)
                {
                    case "Allowed":
                        ConnectButton.Visibility = phone.Allowed ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
                        UpdateImages();
                        break;
                    case "SyncStatus":
                        UpdateTiles(phone.SyncStatus);
                        break;
                    case "InSync":
                        UpdateImages();
                        break;
                }
            }
        }

        private void UpdateImages()
        {
            if (phone != null)
            {
                if (phone.InSync)
                {
                    this.InSyncImage.Visibility = System.Windows.Visibility.Visible;
                    this.TrustedPhoneImage.Visibility = System.Windows.Visibility.Collapsed;
                }
                else if (phone.Allowed)
                {
                    this.InSyncImage.Visibility = System.Windows.Visibility.Collapsed;
                    TrustedPhoneImage.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    this.TrustedPhoneImage.Visibility = System.Windows.Visibility.Collapsed;
                    this.InSyncImage.Visibility = System.Windows.Visibility.Collapsed;
                }

            }
        }

        private void UpdateTiles(SyncResult syncResult)
        {
            if (syncResult != null)
            {
                AnimateCount(InsertIndicator, syncResult.GetTotalInserted());
                AnimateCount(UpdateIndicator, syncResult.GetTotalUpdated());
                AnimateCount(UnchangedIndicator, syncResult.Unchanged);
                AnimateCount(DeleteIndicator, syncResult.GetTotalDeleted());
            }
        }

        void AnimateCount(SyncProgressControl ctrl, List<ContactVersion> list)
        {
            DoubleAnimation animation = new DoubleAnimation();
            animation.To = list == null ? 0 : list.Count;
            animation.Duration = new Duration(TimeSpan.FromSeconds(.3));

            ctrl.Tag = list;

            Storyboard s = new Storyboard();
            Storyboard.SetTarget(animation, ctrl);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Count"));
            s.Children.Add(animation);
            s.Begin();
        }  

        private void OnConnectClick(object sender, RoutedEventArgs e)
        {
            if (phone != null)
            {
                phone.Allowed = true;
            }
        }

        private void OnIndicatorClick(object sender, MouseButtonEventArgs e)
        {
            SyncProgressControl ctrl = (SyncProgressControl)sender;
            var selectedTile = ctrl;
            var selectedList = (List<ContactVersion>)ctrl.Tag;
            if (selectedList != null && selectedList.Count > 0)
            {
                // todo: 
                // this.NavigationService.Navigate(new Uri("/Pages/ReportPage.xaml", UriKind.RelativeOrAbsolute));
            }
        }
    }
}
