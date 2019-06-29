using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Security;
using System.Threading.Tasks;
using System.Windows;

// see https://docs.microsoft.com/en-us/azure/active-directory/develop/tutorial-v2-windows-desktop

namespace MeetingBuzzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AccessManager accessManager = new AccessManager();

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += OnMainWindowLoaded;
        }

        private async void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var token = accessManager.GetTokenAsync(ConfigurationManager.AppSettings["clientId"].ToString());

                GraphServiceClient graphClient = new GraphServiceClient("https://graph.microsoft.com/v1.0", new DelegateAuthenticationProvider(async (requestMessage) => {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
                }));

                var currentUser = await graphClient.Me.Request().GetAsync();
                Console.WriteLine(currentUser.DisplayName);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }

    static class SecureStringExtensions
    {
        public static SecureString ToSecureString(this string s)
        {
            SecureString result = new SecureString();
            foreach (var ch in s)
            {
                result.AppendChar(ch);
            }
            return result;
        }
    }
}
