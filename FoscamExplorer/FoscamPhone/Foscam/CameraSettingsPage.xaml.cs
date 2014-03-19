using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using FoscamExplorer.Pages;
using System.Threading.Tasks;

namespace FoscamExplorer.Foscam
{
    public partial class CameraSettingsPage : PhoneApplicationPage
    {
        FoscamDevice device;
        PropertyBag deviceParams;

        public CameraSettingsPage()
        {
            this.InitializeComponent();

            ErrorMessage.Text = "";

            updating = true;
            foreach (var fps in FoscamDevice.FpsItems)
            {
                ComboBoxFps.Items.Add(fps.Caption);
            }

            for (int i = 1; i <= 6; i++)
            {
                ComboBoxContrast.Items.Add(i.ToString());
            }

            for (int i = 1; i < 16; i++)
            {
                ComboBoxBrightness.Items.Add(i.ToString());
            }
            this.Loaded += CameraSettingsPage_Loaded;

            PasswordButton.IsEnabled = false;
        }

        public FoscamDevice FoscamDevice
        {
            get { return device; }
            set { device = value; }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // see if we launched the change password dialog
            LogonPage page = e.Content as LogonPage;
            if (page != null)
            {
                // setup the logon page for change password.
                string user = deviceParams.GetValue<string>("user1_name");
                string pswd = deviceParams.GetValue<string>("user1_pwd");

                page.Title = "Camera Account";
                page.SignOnButtonCaption = "Update";
                page.UserName = user;
                page.Password = pswd;                

                if (DataStore.Instance.Cameras.Count > 1)
                {
                    page.CheckboxVisibility = Visibility.Visible;
                    page.CheckBoxAllCaption = "Update all cameras";
                }

                page.LoginClicked += OnLoginClicked;
            }

        }

        bool updating;

        FpsInfo GetFpsItem(byte fps)
        {
            FpsInfo found = null;
            foreach (var item in FoscamDevice.FpsItems)
            {
                found = item;
                if ((byte)item.Value >= fps)
                {
                    break;
                }
            }
            return found;
        }

        private FpsInfo FindFpsItem(string caption)
        {
            FpsInfo found = null;
            foreach (var item in FoscamDevice.FpsItems)
            {
                found = item;
                if (item.Caption == caption)
                {
                    break;
                }
            }
            return found;
        }


        async void CameraSettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (FoscamDevice != null)
            {
                this.deviceParams = await device.GetParams();
                PasswordButton.IsEnabled = true;

                updating = true;
                this.ComboBoxFps.SelectedItem = GetFpsItem(this.device.CameraInfo.Fps).Caption;
                this.ComboBoxBrightness.SelectedItem = (this.device.CameraInfo.Brightness / 16).ToString();
                this.ComboBoxContrast.SelectedItem = this.device.CameraInfo.Contrast.ToString();
            }

            updating = false;
        }

        void ShowError(string text)
        {
            var quiet = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
            {
                ErrorMessage.Text = "" + text;
            }));
        }

        private void OnFpsChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!updating)
            {
                // this one we just update locally, this is not a "setting", instead it is the parameter to the request for
                // video stream which has to be passed to StartJpegStream.
                FpsInfo fps = FindFpsItem((string)ComboBoxFps.SelectedItem);
                this.FoscamDevice.CameraInfo.Fps = (byte)fps.Value;
            }
        }

        private async void OnBrightnessChanged(object sender, SelectionChangedEventArgs e)
        {
            var device = this.FoscamDevice;
            if (!updating && device != null)
            {
                int newValue = 0;
                int.TryParse((string)ComboBoxBrightness.SelectedItem, out newValue);
                newValue *= 16;
                if (newValue != device.CameraInfo.Brightness)
                {
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
        }

        private async void OnContrastChanged(object sender, SelectionChangedEventArgs e)
        {
            var device = this.FoscamDevice;
            if (!updating && device != null)
            {
                int newValue = 0;
                int.TryParse((string)ComboBoxContrast.SelectedItem, out newValue);
                if (newValue != device.CameraInfo.Contrast)
                {
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

        private void OnChangeUserPassword(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/pages/logonpage.xaml", UriKind.RelativeOrAbsolute));            
        }

        async void OnLoginClicked(object sender, EventArgs e)
        {
            string user = deviceParams.GetValue<string>("user1_name");
            string pswd = deviceParams.GetValue<string>("user1_pwd");

            LogonPage page = (LogonPage)sender;
            if (page.UserName != user || pswd != page.Password)
            {
                await ChangeUserNamePassword(page.UserName, page.Password, page.CheckBoxAllIsChecked);
            }
        }

        private async Task ChangeUserNamePassword(string userName, string password, bool updateAll)
        {
            if (await UpdateAccountInfo(device, userName, password, true))
            {
                if (updateAll)
                {
                    foreach (var camera in DataStore.Instance.Cameras)
                    {
                        if (camera != this.device.CameraInfo)
                        {
                            FoscamDevice temp = new FoscamDevice() { CameraInfo = camera };
                            await UpdateAccountInfo(device, userName, password, false);
                        }
                    }
                }
            }
        }

        private async Task<bool> UpdateAccountInfo(FoscamDevice device, string userName, string password, bool showError)
        {
            var result = await device.ChangeUserPassword(userName, password);
            if (!string.IsNullOrEmpty(result))
            {
                if (showError)
                {
                    ShowError(result);
                }
                return false;
            }
            else
            {
                if (showError)
                {
                    ShowError("updated");
                }
                device.CameraInfo.UserName = userName;
                device.CameraInfo.Password = password;
                return true;
            }
        }

    }
}