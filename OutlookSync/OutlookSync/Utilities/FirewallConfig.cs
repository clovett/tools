using NetFwTypeLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutlookSync.Utilities
{
    class FirewallConfig
    {
        public FirewallConfig()
        {

        }

        public async Task<bool> CheckSettings()
        {
            bool ok = false;

            string assembly = this.GetType().Assembly.Location;

            await Task.Run(new Action(() =>
            {
                Type NetFwMgrType = Type.GetTypeFromProgID("HNetCfg.FwMgr", false);
                INetFwMgr mgr = (INetFwMgr)Activator.CreateInstance(NetFwMgrType);
                bool firewallenabled = mgr.LocalPolicy.CurrentProfile.FirewallEnabled;
                if (firewallenabled)
                {
                    // check our app is configured correctly.
                    var applications = (INetFwAuthorizedApplications)mgr.LocalPolicy.CurrentProfile.AuthorizedApplications;
                    IEnumerator enumerator = applications.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        INetFwAuthorizedApplication app = (INetFwAuthorizedApplication)enumerator.Current;
                        if (app.Name == "Outlook Sync Server")
                        {
                            bool enabled = app.Enabled;
                            var scope = app.Scope;
                            string fileName = app.ProcessImageFileName;
                            var ip = app.IpVersion ;
                            string addresses = app.RemoteAddresses;
                            if (app.Enabled && IsSameFileName(fileName, assembly))
                            {
                                ok = true;
                            }
                        }
                    }
                }
                else
                {
                    // we're ok by definition!
                    ok = true;
                }

            }));

            return ok;
        }

        private bool IsSameFileName(string fileName1, string fileName2)
        {
            bool result = false;
            Uri uri1 = null;
            Uri uri2 = null;
            // this knows about DOS case sensitivity and normalization of file names 
            if (Uri.TryCreate(fileName1, UriKind.Absolute, out uri1) && Uri.TryCreate(fileName2, UriKind.Absolute, out uri2))
            {
                result = uri1 == uri2;
            }
            return result;
        }
    }
}
