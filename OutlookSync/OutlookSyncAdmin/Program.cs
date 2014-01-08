using NetFwTypeLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Collections;

namespace OutlookSyncAdmin
{
    class Program
    {
        string exePath;
        bool debug;

        static void Main(string[] args)
        {

            Program p = new Program();
            if (p.ParseCommandLine(args))
            {
                p.Run();
            }
            else
            {
                PrintUsage();
            }
            
        }

        private void Run()
        {
            if (debug)
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadLine();
            }
            if (!string.IsNullOrEmpty(exePath))
            {
                FixSettings(exePath);
            }
        }

        bool ParseCommandLine(string[] args)
        {
            for (int i = 0, n = args.Length; i < n; i++)
            {
                string arg = args[i];
                if (arg[0] == '/' || arg[0] == '-')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "?":
                        case "h":
                        case "help":
                            return false;
                        case "debug":
                            this.debug = true;
                            break;
                        case "fixfw":
                            if (i + 1 < n)
                            {
                                exePath = args[++i];
                            }
                            break;
                        default:
                            Console.WriteLine("Unrecognized command line argument " + arg);
                            return false;
                    }
                }
            }
            if (exePath == null)
            {
                Console.WriteLine("Missing exepath argument");
                return false;
            }
            if (!File.Exists(exePath))
            {
                Console.WriteLine("Specified executable not found");
                return false;
            }
            return true;
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: OutlookSyncAdmin [-fixfw exepath]");
        }

        public void FixSettings(string appPath)
        {
            Uri assemblyUri = new Uri(appPath);
            string name = Path.GetFileName(appPath);

            string desc = "Allow incoming {0} traffic to {1}";

            Type NetFwMgrType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2", false);
            INetFwPolicy2 policy = (INetFwPolicy2)Activator.CreateInstance(NetFwMgrType);

            // check if there are any existing rules, and fix them.
            IEnumerator enumerator = policy.Rules.GetEnumerator();
            bool fixedUdp = false;
            bool fixedTcp = false;
            int expectedProfiles = (int)(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE | NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC | NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN);

            while (enumerator.MoveNext())
            {
                INetFwRule rule = (INetFwRule)enumerator.Current;
                NET_FW_IP_PROTOCOL_ protocol = (NET_FW_IP_PROTOCOL_)rule.Protocol;
                string protocolString = (protocol == NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP ? "TCP" : "UDP");
                NET_FW_RULE_DIRECTION_ direction = (NET_FW_RULE_DIRECTION_)rule.Direction;
                if (direction == NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN)
                {
                    string appName = rule.ApplicationName;

                    if (!string.IsNullOrEmpty(appName))
                    {
                        try
                        {
                            Uri uri = new Uri(appName, UriKind.RelativeOrAbsolute);
                            if (uri == assemblyUri)
                            {
                                rule.Name = name;

                                rule.Description = string.Format(desc, protocolString, name);
                                rule.Grouping = name;

                                if (!rule.Enabled)
                                {
                                    rule.Enabled = true;
                                }
                                if (rule.Action == NET_FW_ACTION_.NET_FW_ACTION_BLOCK)
                                {
                                    rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                                }
                                if (rule.InterfaceTypes != "All")
                                {
                                    rule.InterfaceTypes = "All";
                                }
                                if (rule.RemoteAddresses != "*")
                                {
                                    rule.RemoteAddresses = "*";
                                }

                                if (rule.Profiles != expectedProfiles)
                                {
                                    rule.Profiles = expectedProfiles;
                                }
                                
                                if (protocol == NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP)
                                {
                                    fixedTcp = true;
                                }

                                if (protocol == NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP)
                                {
                                    fixedUdp = true;
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }

            if (!fixedUdp)
            {

                Type ruleType = Type.GetTypeFromProgID("HNetCfg.FwRule", false);
                INetFwRule newRule = (INetFwRule)Activator.CreateInstance(ruleType);


                newRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                newRule.ApplicationName = appPath;
                newRule.Name = name;
                newRule.Grouping = name;
                newRule.Description = string.Format(desc, "UDP", name);
                newRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
                newRule.Enabled = true;
                newRule.Protocol = (int)(NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP);
                newRule.InterfaceTypes = "All";
                newRule.LocalPorts = "*";
                newRule.Profiles = expectedProfiles;

                policy.Rules.Add(newRule);
            }

            if (!fixedTcp)
            {

                Type ruleType = Type.GetTypeFromProgID("HNetCfg.FwRule", false);
                INetFwRule newRule = (INetFwRule)Activator.CreateInstance(ruleType);


                newRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                newRule.ApplicationName = appPath;
                newRule.Name = name;
                newRule.Grouping = name;
                newRule.Description = string.Format(desc, "TCP", name);
                newRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
                newRule.Enabled = true;
                newRule.Protocol = (int)(NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP);
                newRule.InterfaceTypes = "All";
                newRule.LocalPorts = "*";
                newRule.Profiles = expectedProfiles;

                policy.Rules.Add(newRule);
            }
        }
    }
}
