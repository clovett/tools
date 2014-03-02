using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Store;

namespace OutlookSyncPhone.Utilities
{
    /// <summary>
    /// This class encapsulates the calls to lookup ProductLicense information.
    /// It also takes care of delaying these calls until there is a network available.
    /// It also skips the calls altogether if we already know the license is granted from our local app settings
    /// thereby making the app run faster when you purchase the product feature.
    /// </summary>
    public class LicenseManager
    {
        List<string> productIds = new List<string>();

        private LicenseManager()
        {
            // if network changes from offline to online then we can check for license and add advertising.
            Microsoft.Phone.Net.NetworkInformation.DeviceNetworkInformation.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        }

        static Settings _settings;

        static LicenseManager _instance;

        public static LicenseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LicenseManager();
                    _settings = OutlookSyncPhone.App.Settings;
                }
                return _instance;
            }
        }

        /// <summary>
        /// Register the product ids that you would like to check for.
        /// </summary>
        /// <param name="id"></param>
        public void RegisterProductId(string id)
        {
            productIds.Add(id);
        }

        public event EventHandler LicensesChecked;

        void OnNetworkAvailabilityChanged(object sender, Microsoft.Phone.Net.NetworkInformation.NetworkNotificationEventArgs e)
        {
            BeginCheckLicenses();
        }

        public void BeginCheckLicenses()
        {
            bool foundLicenses = true;

            foreach (string product in this.productIds)
            {
                if (!HasLicense(product))
                {
                    foundLicenses = false;
                    break;
                }
            }

            if (foundLicenses)
            {
                // already have them, so no need for all the other BS, let the app run baby!
                return;
            }

            if (!Microsoft.Phone.Net.NetworkInformation.DeviceNetworkInformation.IsNetworkAvailable)
            {
                // can't do it right now.
                return;
            }

            Task.Run(new Action(CheckLicensesBackgroundTask));
        }

        private void CheckLicensesBackgroundTask()
        {            
            try
            {
                var licenseInfo = Windows.ApplicationModel.Store.CurrentApp.LicenseInformation;

                foreach (string productId in this.productIds)
                {
                    ProductLicense productLicense = null;
                    if (licenseInfo.ProductLicenses.TryGetValue(productId, out productLicense) && productLicense != null)
                    {
                        if (productLicense.IsActive)
                        {
                            this.AddLicense(productId);
                        }
                    }
                }
                if (LicensesChecked != null)
                {
                    LicensesChecked(this, EventArgs.Empty);
                }
            }
            catch (Exception)
            {
                // no network right now?
                return; // try again later.
            }            
        }

        /// <summary>
        /// Try and purchase the given productId, throws exception if user cancels.
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public async Task InAppPurchaseAsync(string productId)
        {
            await Windows.ApplicationModel.Store.CurrentApp.RequestProductPurchaseAsync(productId, false);

            // if purchase succeeds then our ad state needs updating.
            this.BeginCheckLicenses();
        }

        /// <summary>
        /// Add a license for a given in app product id.
        /// </summary>
        /// <param name="license">The product id that the user has a license to</param>
        public void AddLicense(string productId)
        {
            List<string> list = _settings.Licenses;
            if (list == null) {
                list = new List<string>();
            }
            if (list.Contains(productId))
            {
                // already there
                return;
            }

            list.Add(productId);

            // make sure we persist this list.
            _settings.Licenses = list;
        }

        /// <summary>
        /// Return true if we already know that the user has purchased a license to the given in app product Id.
        /// This does not do a real license check, it is only checking the cached value.
        /// </summary>
        /// <param name="productId"></param>
        /// <returns>True if user owns this product already.</returns>
        internal bool HasLicense(string productId)
        {
            List<string> list = _settings.Licenses;
            if (list == null)
            {
                return false;
            }

            return list.Contains(productId);
        }

    }
}
