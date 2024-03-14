using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using Vanara.PInvoke;

namespace LeashMod_Server
{
    internal class FirewallAPIHelper
    {
        public static FirewallApi.INetFwPolicy2 firewallPolicy = (FirewallApi.INetFwPolicy2)Activator.CreateInstance(
            Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

        public static async void ApplyFirewallRule(string IP, bool ban)
        {
            string SanitizedIP = IP.Replace("\r", "").Replace("\n", "").Replace(" ", "");

            if ((await FileUtils.SafelyReadAllText(Environment.CurrentDirectory + "\\WhitelistedIPs.txt")).Contains(SanitizedIP))
            {
                return;
            }

            string RuleName = "Blocked IP " + SanitizedIP.Replace(".", "-");

            bool Exists = false;

            foreach (FirewallApi.INetFwRule rule in firewallPolicy.Rules)
            {
                if (rule == null)
                {
                    continue;
                }

                if (rule.Name == RuleName)
                {
                    Exists = true;
                    break;
                }
            }

            if (ban)
            {
                if (Exists)
                {
                    return;
                }

                FirewallApi.INetFwRule firewallRule = (FirewallApi.INetFwRule)Activator.CreateInstance(
                    Type.GetTypeFromProgID("HNetCfg.FWRule"));

                firewallRule.Action = FirewallApi.NET_FW_ACTION.NET_FW_ACTION_BLOCK;
                firewallRule.Direction = FirewallApi.NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_IN;
                firewallRule.InterfaceTypes = "All";

                firewallRule.Name = RuleName;
                firewallRule.Description = "Banned IP From Generic Auth Server";
                firewallRule.RemoteAddresses = SanitizedIP;

                firewallRule.Enabled = true;

                firewallPolicy.Rules.Add(firewallRule);
            }
            else
            {
                firewallPolicy.Rules.Remove(RuleName);
            }
        }
    }
}
