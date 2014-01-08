using NetFwTypeLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Security.Principal;

namespace Microsoft.Networking
{
    public class FirewallEventArgs : EventArgs
    {
        public bool FirewallEntryMissing { get; set; }
        public bool FirewallSettingsIncorrect { get; set; }
    }

    /// <summary>
    /// Since this 
    /// </summary>
    public class FirewallConfig
    {
        Uri assemblyUri;
        CancellationTokenSource cancellationSource;

        public FirewallConfig(string exePath)
        {
            assemblyUri = new Uri(exePath);
            string path = assemblyUri.LocalPath;
        }

        public event EventHandler<FirewallEventArgs> FirewallErrorDetected;

        public void StartCheckingFirewall()
        {
            StopCheckingFirewall();
            cancellationSource = new CancellationTokenSource();
            Task.Run(new Action(() =>
            {
                CheckFirewallTask();
            }), cancellationSource.Token);
        }

        public void StopCheckingFirewall()
        {
            if (cancellationSource != null)
            {
                cancellationSource.Cancel();
            }
        }

        private async void CheckFirewallTask()
        {
            while (!cancellationSource.IsCancellationRequested)
            {
                FirewallEventArgs e = await CheckSettings();
                if (e != null)
                {
                    if (FirewallErrorDetected != null)
                    {
                        FirewallErrorDetected(this, e);
                    }
                    if (!e.FirewallEntryMissing && !e.FirewallSettingsIncorrect)
                    {
                        // we're good!
                        break;
                    }
                }

                await Task.Delay(3000);
            }
        }

        public async Task<FirewallEventArgs> CheckSettings()
        {
            FirewallEventArgs e = new FirewallEventArgs();

            await Task.Run(new Action(() =>
            {
                Type NetFwMgrType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2", false);
                INetFwPolicy2 policy = (INetFwPolicy2)Activator.CreateInstance(NetFwMgrType);

                bool firewallenabled = false;

                int profileTypes = policy.CurrentProfileTypes;

                bool hasPublicProfile = (profileTypes & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC) != 0;
                bool hasPrivateProfile = (profileTypes & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE) != 0;
                bool hasDomainProfile = (profileTypes & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN) != 0;

                if (hasDomainProfile)
                {
                    firewallenabled = policy.get_FirewallEnabled(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN);
                }
                else if (hasPrivateProfile)
                {
                    firewallenabled = policy.get_FirewallEnabled(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE);
                }
                else if (hasPublicProfile)
                {
                    firewallenabled = policy.get_FirewallEnabled(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC);
                }

                if (firewallenabled)
                {
                    // check our app is configured correctly.
                    IEnumerator enumerator = policy.Rules.GetEnumerator();
                    bool found = false;
                    bool incomingUdp = false;
                    bool incomingTcp = false;

                    while (enumerator.MoveNext())
                    {
                        INetFwRule rule = (INetFwRule)enumerator.Current;
                        NET_FW_IP_PROTOCOL_ protocol = (NET_FW_IP_PROTOCOL_)rule.Protocol;
                        NET_FW_RULE_DIRECTION_ direction = (NET_FW_RULE_DIRECTION_)rule.Direction;
                        if (direction == NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN)
                        {
                            string name = rule.ApplicationName;

                            if (!string.IsNullOrEmpty(name))
                            {
                                try
                                {
                                    Uri uri = new Uri(name, UriKind.RelativeOrAbsolute);
                                    if (uri == assemblyUri)
                                    {
                                        found = true;
                                        if (rule.Enabled && 
                                            rule.InterfaceTypes == "All" && 
                                            rule.LocalPorts == "*" && 
                                            rule.RemoteAddresses == "*")
                                        {

                                            if ((rule.Profiles & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE) != 0)
                                            {
                                                // great

                                                switch (protocol)
                                                {
                                                    case NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY:
                                                        incomingUdp = true;
                                                        incomingTcp = true;
                                                        break;
                                                    case NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP:
                                                        incomingTcp = true;
                                                        break;
                                                    case NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP:
                                                        incomingUdp = true;
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                // required profile is missing.  For example, user is on a private network and the rule 
                                                // is not enabled for private networks...
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }

                    e.FirewallEntryMissing = !found;
                    e.FirewallSettingsIncorrect = !incomingUdp || !incomingTcp;
                }
                else
                {
                    // we're ok by definition!
                }

            }));

            return e;
        }

        public static bool ExtractEmbeddedResourceAsFile(string name, string path)
        {
            using (Stream s = typeof(FirewallConfig).Assembly.GetManifestResourceStream(name))
            {
                if (s == null)
                {
                    return false;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] buffer = new byte[64000];
                    int len = 0;
                    while ((len = s.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fs.Write(buffer, 0, len);
                    }
                    fs.Close();
                }
            }
            return true;
        }

        
        public void FixFirewallSettings(string exePath, bool debug)
        {
            string path = Path.Combine(Path.GetTempPath(), "OutlookSyncAdmin.exe");
            

            if (ExtractEmbeddedResourceAsFile("Microsoft.Networking.OutlookSyncAdmin.exe", path))
            {
                string args = string.Format("-fixfw \"{0}\"", exePath);
                if (debug)
                {
                    args += " -debug";
                }

                ProcessStartInfo info = new ProcessStartInfo(path, args);
                Process program = Process.Start(info);
                program.WaitForExit();
            }
        }

    }
}
