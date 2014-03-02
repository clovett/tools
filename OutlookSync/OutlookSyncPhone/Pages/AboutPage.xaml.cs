using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Reflection;
using Microsoft.Phone.Tasks;
using OutlookSyncPhone.Utilities;

namespace OutlookSyncPhone.Pages
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        public SettingsPage()
        {
            InitializeComponent();

            var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
            var version = nameHelper.Version;
            VersionText.Text = string.Format(VersionText.Text, version.ToString());
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (LicenseManager.Instance.HasLicense(Settings.RemoveAdsProductId))
            {
                // no need for this button then.
                RemoveAdsButton.Visibility = System.Windows.Visibility.Collapsed;
            }

            base.OnNavigatedTo(e);
        }

        private void OnRateApp(object sender, RoutedEventArgs e)
        {
            MarketplaceDetailTask marketplaceDetailTask = new MarketplaceDetailTask();
            marketplaceDetailTask.ContentIdentifier = App.AppId;
            marketplaceDetailTask.ContentType = MarketplaceContentType.Applications;
            marketplaceDetailTask.Show();
        }

        private void OnBackPressed(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

        private async void OnRemoveAds(object sender, RoutedEventArgs e)
        {
            // ok, user wants to remove ads, so time to purchase the in-app product named "Remove Advertising"

            try
            {
                RemoveAdsButton.IsEnabled = false;

                await LicenseManager.Instance.InAppPurchaseAsync(Settings.RemoveAdsProductId);

                RemoveAdsButton.Visibility = System.Windows.Visibility.Collapsed;
            }
            catch (Exception)
            {
                RemoveAdsButton.IsEnabled = true;
                // user cancelled, or something went wrong.
                return;
            }
        }
    }
}