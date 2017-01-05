using NetgearDataUsage.Controls;
using NetgearDataUsage.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace NetgearDataUsage
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string FileAccessToken = "Settings.FileName";
        TrafficMeter model;

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += OnPageLoaded;
            Window.Current.CoreWindow.KeyDown += OnWindowKeyDown;
        }

        private async void OnWindowKeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.F5)
            {
                await GetTrafficMeter(null);
            }
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            StorageFile file = null;
            string fileName = Settings.Instance.FileName;
            if (string.IsNullOrEmpty(fileName))
            {
                file = (StorageFile)(await Windows.Storage.ApplicationData.Current.LocalFolder.TryGetItemAsync("data.xml"));
            }
            else
            {
                // get access to this file again using the saved token.
                if (Settings.Instance.FileAccessToken != null)
                {
                    file = await StorageApplicationPermissions.FutureAccessList.GetFileAsync(Settings.Instance.FileAccessToken);
                }
                else
                {
                    file = await StorageFile.GetFileFromPathAsync(fileName);
                }
                if (file == null)
                {
                    ShowStatus(string.Format("Cannot find file, or we lost permission to access it: '{0}'", fileName));
                }
            }

            await LoadModel(file);

            if (Settings.Instance.FirstLaunch)
            {
                Settings.Instance.FirstLaunch = false;
                ShowSettings();
                await Settings.Instance.SaveAsync();
            }
            else
            {
                await GetTrafficMeter(null);
            }
        }

        private async Task LoadModel(StorageFile file)
        {
            model = await TrafficMeter.LoadAsync(file);
            OnFileChanged();
            ShowGraph(model);
        }

        private async Task SaveModel(StorageFile file)
        {
            await model.SaveAsync(file);
            OnFileChanged();
        }

        public void OnFileChanged()
        {
            Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title = model.File.Path;
        }

        private void OnModelUpdated()
        {
            var today = model.GetRow(DateTime.Today);

            ShowStatus("Today: upload=" + today.Upload + ", download=" + today.Download );

            ShowGraph(model);
        }

        private void ShowGraph(TrafficMeter model)
        {
            // get the data
            List<DailyTraffic> rows = new List<Model.DailyTraffic>();
            var today = DateTime.Today;

            double max = Settings.Instance.TargetUsage * 1000; // 1024 gigabytes.
            List<DataValue> values = new List<DataValue>();

            var start = new DateTime(today.Year, today.Month, 1);
            var last = start.AddMonths(1).AddDays(-1);
            Graph.SetColumnCount(last.Day);

            double total = 0;
            for (var i = start; i <= today; i = i.AddDays(1))
            {
                DataValue dv = new Controls.DataValue()
                {
                    TipFormat = i.ToString("M") + "\nActual: {0:N0}\nMaximum: {1:N0}",
                    ShortLabel = i.Day.ToString()
                };
                var numbers = model.GetRow(i);
                if (numbers != null)
                {
                    total += numbers.Download;
                }
                dv.Value = total;
                values.Add(dv);
                rows.Add(numbers);
            }

            if (total > max)
            {
                max = total;
            }

            Graph.TargetValue = max;
            Graph.DataValues = values;

        }

        private async Task GetTrafficMeter(WebCredential credential)
        { 
            try
            {
                ShowStatus("");

                if (credential == null)
                {
                    credential = WebCredential.LoadCredential(Settings.Instance.TrafficMeterUri);
                }

                if (!await model.GetTrafficMeter(credential))
                { 
                    // prompt for userid and password.
                    PromptForCredentials(credential.Realm, credential);
                }
                else
                {
                    OnModelUpdated();
                }
            }
            catch (Exception ex)
            {
                ShowStatus(ex.Message);
            }
        }

        private void ShowStatus(string message)
        {
            UiDispatcher.RunOnUIThread(() =>
            {
                StatusText.Text = "" + message;
            });
        }

        private void PromptForCredentials(string realm, WebCredential credentials)
        {
            Flyout flyout = new Flyout();            
            var panel = new LoginPanel();
            panel.Height = this.Height;
            panel.Width = 400;

            Uri uri = new Uri(Settings.Instance.TrafficMeterUri);
            if (string.IsNullOrWhiteSpace(realm)) realm = "Unknown";
            panel.Prompt = string.Format(panel.Prompt, uri.Host, realm);
            if (credentials != null)
            {
                panel.UserName = credentials.UserName;
                panel.Password = credentials.Password;
            }
            else
            {
                panel.UserName = "admin";
            }
            panel.RememberCredentials = true;

            panel.OkCancelClick += OnPasswordProvided;
            flyout.Content = panel;
            Flyout.SetAttachedFlyout(panel, flyout);
            flyout.Placement = FlyoutPlacementMode.Right;
            flyout.ShowAt(this);
        }

        private void OnPasswordProvided(object sender, EventArgs e)
        {
            LoginPanel panel = (LoginPanel)sender;

            // close the flyout.
            var flyout = Flyout.GetAttachedFlyout(panel);
            if (flyout != null)
            {
                flyout.Hide();
            }

            if (panel.Cancelled)
            {
                ShowStatus("Cancelled");
            }
            else
            {
                WebCredential credential = new Model.WebCredential()
                {
                    Uri = Settings.Instance.TrafficMeterUri,
                    UserName = panel.UserName,
                    Password = panel.Password,
                    RememberCredentials = panel.RememberCredentials
                };

                UiDispatcher.RunOnUIThread(() => { var nowait = GetTrafficMeter(credential); });
            }
        }

        private async void OnOpenFile(object sender, RoutedEventArgs e)
        {
            FileOpenPicker fo = new FileOpenPicker();
            fo.ViewMode = PickerViewMode.Thumbnail;
            fo.FileTypeFilter.Add(".xml");
            StorageFile file = await fo.PickSingleFileAsync();
            if (file != null)
            {
                try
                {
                    await LoadModel(file);
                    await SaveFileAccess(file);
                    await GetTrafficMeter(null);
                }
                catch (Exception ex)
                {
                    // unrecoverable error... 
                    ShowStatus("Error Loading File: " + ex.Message);
                }
            }
        }

        private async Task SaveFileAccess(StorageFile file)
        {            
            // remember we have access to this file.
            StorageApplicationPermissions.FutureAccessList.AddOrReplace(FileAccessToken, file);

            if (Settings.Instance.FileAccessToken != FileAccessToken ||
                Settings.Instance.FileName != file.Path)
            {
                Settings.Instance.FileAccessToken = FileAccessToken;
                Settings.Instance.FileName = file.Path;
                await Settings.Instance.SaveAsync();
            }
        }

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            ShowSettings();
        }

        void ShowSettings()
        { 
            Flyout flyout = new Flyout();
            var panel = new SettingsPanel();
            panel.Height = this.Height;
            panel.Width = 400;
            panel.OkCancelClick += (s,argse) =>
            {
                flyout.Hide();
                OnSettingsUpdated();
            };
            flyout.Content = panel;
            Flyout.SetAttachedFlyout(panel, flyout);
            flyout.Placement = FlyoutPlacementMode.Right;
            flyout.ShowAt(this);
        }

        private async void OnSettingsUpdated()
        {
            await GetTrafficMeter(null);
        }

        private async void OnSaveFile(object sender, RoutedEventArgs e)
        {
            FileSavePicker fo = new FileSavePicker();
            fo.DefaultFileExtension = ".xml";
            if (this.model.File != null)
            {
                fo.SuggestedSaveFile = this.model.File;
            }
            fo.FileTypeChoices.Add("XML File", new List<string>() { ".xml" });
            StorageFile file = await fo.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    await SaveModel(file);
                    await SaveFileAccess(file);
                }
                catch (Exception ex)
                {
                    // unrecoverable error... 
                    ShowStatus("Error Loading File: " + ex.Message);
                }
            }
        }
    }
}
