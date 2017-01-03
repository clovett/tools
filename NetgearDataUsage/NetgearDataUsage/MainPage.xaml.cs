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
        static string RouterTrafficMeterUri = "http://192.168.1.1/traffic_meter.htm";
        TrafficMeter model;

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            model = await TrafficMeter.LoadAsync();

            await GetTrafficMeter(null);
        }

        private async void UpdateModel(string html)
        {
            model.ScrapeDataFromHtmlPage(html);
            await model.SaveAsync();

            var today = model.GetRow(DateTime.Today);
            ShowStatus("Today: upload=" + today.Upload + ", download=" + today.Download );

            ShowGraph(model);
        }

        private void ShowGraph(TrafficMeter model)
        {
            // get the data
            List<DailyTraffic> rows = new List<Model.DailyTraffic>();
            var today = DateTime.Today;

            double max = 1024000; // 1024 gigabytes.
            List<double> values = new List<double>();

            var start = new DateTime(today.Year, today.Month, 1);
            var last = start.AddMonths(1).AddDays(-1);
            Graph.SetColumnCount(last.Day);

            double total = 0;
            for (var i = start; i <= today; i = i.AddDays(1))
            {
                var numbers = model.GetRow(i);
                total += numbers.Download;
                values.Add(total);
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
                    credential = WebCredential.LoadCredential(RouterTrafficMeterUri);
                }
                
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "text/html, application/xhtml+xml, image/jxr, */*");
                client.DefaultRequestHeaders.Add("Accept-Language", "en-US");
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36 Edge/15.14986");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                if (credential != null)
                {
                    // "Basic YWRtaW46aW5hbWJlcmNsYWQ="
                    var byteArray = System.Text.Encoding.UTF8.GetBytes(credential.UserName + ":" + credential.Password);
                    var base64String = Convert.ToBase64String(byteArray);
                    client.DefaultRequestHeaders.Add("Authorization", "Basic " + base64String);
                }

                HttpResponseMessage response = await client.GetAsync(RouterTrafficMeterUri);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    byte[] data = await response.Content.ReadAsByteArrayAsync();
                    var contentType = response.Content.Headers.ContentType;
                    string html = DecodeHtml(data, contentType.MediaType, contentType.CharSet);

                    UpdateModel(html);

                    if (credential.RememberCredentials)
                    {
                        WebCredential.SavePassword(credential);
                    }
                }
                else
                {
                    byte[] data = await response.Content.ReadAsByteArrayAsync();
                    var contentType = response.Content.Headers.ContentType;
                    string html = DecodeHtml(data, contentType.MediaType, contentType.CharSet);

                    ShowStatus(response.StatusCode.ToString());
                    string realm = GetBasicRealm(response);
                    if (realm != null)
                    {
                        // prompt for userid and password.
                        PromptForCredentials(realm, credential);
                    }
                    else
                    {
                        ShowStatus("Access denied and basic authentication is not supported by this router");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatus(ex.Message);
            }
        }

        private string GetBasicRealm(HttpResponseMessage response)
        {
            foreach (var authType in response.Headers.WwwAuthenticate)
            {
                if (authType.Scheme == "Basic")
                {
                    string realm = authType.Parameter;
                    if (realm.StartsWith("realm="))
                    {
                        return realm.Substring(6).Trim('"');
                    }
                }
                Debug.WriteLine(authType.Scheme + ": " + authType.Parameter);
            }
            return null;
        }

        private void ShowStatus(string message)
        {
            UiDispatcher.RunOnUIThread(() =>
            {
                StatusText.Text = "" + message;
            });
        }

        private string DecodeHtml(byte[] data, string mediaType, string charSet)
        {
            if (string.Compare(mediaType, "text/html", StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new Exception("Unexpected media type returned, expecting 'text/html' but received: " + mediaType);
            }
            charSet = (""+charSet).Trim('"');
            if (string.Compare(charSet, "utf-8", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return Encoding.UTF8.GetString(data);
            }
            else if (string.IsNullOrWhiteSpace(charSet))
            {
                return Encoding.ASCII.GetString(data);
            }
            else
            { 
                throw new Exception("Unexpected charset returned, expecting 'utf-8' but received: " + mediaType);
            }

        }

        private void PromptForCredentials(string realm, WebCredential credentials)
        {
            Flyout flyout = new Flyout();
            var panel = new LoginPanel();
            panel.Height = this.Height;
            panel.Width = 400;
            panel.Prompt = string.Format(panel.Prompt, "192.168.1.1", realm);
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
                    Uri = RouterTrafficMeterUri,
                    UserName = panel.UserName,
                    Password = panel.Password,
                    RememberCredentials = panel.RememberCredentials
                };

                UiDispatcher.RunOnUIThread(() => GetTrafficMeter(credential));
            }
        }
    }
}
