using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace FoscamExplorer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CameraSettingsPage : Page
    {
        FoscamDevice device;

        class FpsItem
        {
            public string Caption;
            public byte Value;

            public FpsItem(string caption, byte value) {
                Caption = caption;
                Value = value;
            }

            public override string ToString()
            {
                return Caption;
            }
        };

        static FpsItem[] FpsItems = new FpsItem[] 
        {
            new FpsItem("Max", 0),
            new FpsItem("20 fps", 1),
            new FpsItem("15 fps", 3),
            new FpsItem("10 fps", 6),
            new FpsItem("5 fps", 11),
            new FpsItem("4 fps", 12),
            new FpsItem("3 fps", 13),
            new FpsItem("2 fps", 14),
            new FpsItem("1 fps", 15),
            new FpsItem("1/2 fps", 17),
            new FpsItem("1/3 fps", 19),
            new FpsItem("1/4 fps", 21),
            new FpsItem("1/5 fps", 23)
        };

        public CameraSettingsPage()
        {
            this.InitializeComponent();

            ErrorMessage.Text = "";

            foreach (var fps in FpsItems)
            {
                ComboBoxFps.Items.Add(fps);
            }

            for (int i = 1; i <= 6; i++)
            {
                ComboBoxContrast.Items.Add(i);
            }

            for (int i = 1; i < 16; i++)
            {
                ComboBoxBrightness.Items.Add(i);
            }

            this.Loaded += CameraSettingsPage_Loaded;
        }

        public FoscamDevice FoscamDevice
        {
            get { return device; }
            set { device = value; }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        bool updating;

        FpsItem GetFpsItem(int fps)
        {
            FpsItem found = null;
            foreach (var item in FpsItems)
            {
                found = item;
                if (item.Value >= fps)
                {
                    break;
                }
            }
            return found;
        }

        void CameraSettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (FoscamDevice != null)
            {
                updating = true;
                this.ComboBoxFps.SelectedItem = GetFpsItem(this.device.CameraInfo.Fps);
                this.ComboBoxBrightness.SelectedItem = (int)(this.device.CameraInfo.Brightness / 16);
                this.ComboBoxContrast.SelectedItem = (int)this.device.CameraInfo.Contrast;
                this.FirmwareVersion.Text = "Firmware version " + this.device.CameraInfo.SystemVersion;
                updating = false;
            }

        }

        void ShowError(string text)
        {
            var quiet = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                ErrorMessage.Text = "" + text;
            }));
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            SettingsPane.Show();
        }

        private void OnFpsChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!updating)
            {
                // this one we just update locally, this is not a "setting", instead it is the parameter to the request for
                // video stream which has to be passed to StartJpegStream.
                FpsItem fps = (FpsItem)ComboBoxFps.SelectedItem;
                this.FoscamDevice.CameraInfo.Fps = fps.Value;
            }
        }

        private async void OnBrightnessChanged(object sender, SelectionChangedEventArgs e)
        {
            var device = this.FoscamDevice;
            if (!updating && device != null)
            {
                int newValue = ((int)ComboBoxBrightness.SelectedItem * 16);
                string error = await device.SetBrightness((byte)newValue);
                if (!string.IsNullOrEmpty(error))
                {
                    ShowError(error);
                }
                else
                {
                    ShowError("");
                    device.CameraInfo.Brightness = (byte)newValue;
                }
            }
        }

        private async void OnContrastChanged(object sender, SelectionChangedEventArgs e)
        {
            var device = this.FoscamDevice;
            if (!updating && device != null)
            {
                int newValue = (int)ComboBoxContrast.SelectedItem;
                string error = await device.SetContrast((byte)newValue);
                if (!string.IsNullOrEmpty(error))
                {
                    ShowError(error);
                }
                else
                {
                    ShowError("");
                    device.CameraInfo.Contrast = (byte)newValue;
                }
            }
        }

    }
}
