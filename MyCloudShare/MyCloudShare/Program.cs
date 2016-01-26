using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Sgml;
using System.Xml.Linq;
using System.Diagnostics;

namespace MyCloudShare
{
    class Program
    {
        static void Main(string[] args)
        {
            try {
                string ip = FindIpAddress("familybackup", "admin", "inamberclad");

                string share = @"\\" + ip + "\\Chris";
                string local = "z:";
                bool found = false;

                // see if a netork drive is already mapped for this remote share.
                foreach (var nr in NetworkResource.GetConnectedDrives())
                {
                    Console.WriteLine(nr.sLocalName + "=" + nr.sRemoteName);
                    if (string.Compare(nr.sRemoteName, share, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (string.Compare(nr.sLocalName, local, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            // already mapped
                            Console.WriteLine("Local drive {0} is already mapped to remote share {1}", local, share);
                            return;
                        }
                        found = true;
                    }
                }

                if (found)
                {
                    // need to remap the Z: drive to our remote share
                    NetworkResource.DisconnectLocalShare(local);
                }

                // Map Z: drive to our remote share
                NetworkResource.ConnectShare(local, share);

                Console.WriteLine("Local drive {0} is successfully mapped to remote share {1}", local, share);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        static string FindIpAddress(string machineName, string username, string password)
        {
            XDocument doc = GetHtmlAsXml("http://192.168.1.1/DEV_device2.htm", username, password);
            if (doc != null)
            {

                XElement e = (from node in doc.Descendants("span") where string.Compare(node.Value, machineName, StringComparison.OrdinalIgnoreCase) == 0 select node).FirstOrDefault();
                if (e == null)
                {
                    throw new Exception(string.Format("machine '{0}' not found in connected device table", machineName));
                }
                XElement td = e.Parent;
                XElement tr = td.Parent;
                XElement s = (from node in tr.Descendants("span") where (string)node.Attribute("name") == "rule_ip" select node).FirstOrDefault();
                if (s == null)
                {
                    throw new Exception(string.Format("rule_ip span missing for {0}'s tr element", machineName));
                }
                return s.Value;
            }
            return null;
        }

        static XDocument GetHtmlAsXml(string uri, string userid, string password)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
            req.Credentials = new NetworkCredential(userid, password);
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                using (var stream = resp.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        using (SgmlReader sgml = new SgmlReader())
                        {
                            sgml.DocType = "html";
                            sgml.InputStream = reader;
                            sgml.CaseFolding = CaseFolding.ToLower;
                            XDocument doc = XDocument.Load(sgml);
                            return doc;
                        }
                    }
                }
            }
            throw new Exception(string.Format("Request {0} failed: {1}", uri, resp.StatusDescription));
        }

        
    }
}
