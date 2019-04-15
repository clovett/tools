using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RssLibrary
{
    public class FeedMonitor
    {
        Settings _settings;

        public FeedMonitor()
        {
            _settings = Settings.LoadSettings();
        }

        public Settings Settings { get { return _settings;  } }

        /// <summary>
        /// Return the feeds that have changed since the last time we looked at them.
        /// </summary>
        /// <returns></returns>
        public List<FeedInfo> GetUpdatedFeeds()
        {
            List<FeedInfo> changed = new List<FeedInfo>();
            foreach (var info in _settings.FeedInfo)
            {
                try
                {
                    XDocument doc = XDocument.Load(info.Url);
                    XNamespace ns = doc.Root.Name.Namespace;
                    var channel = doc.Root.Element("channel" + ns);
                    foreach (var item in channel.Elements("item" + ns))
                    {
                        var pubDate = (string)item.Element("pubDate" + ns);
                        DateTime dt = DateTime.Parse(pubDate);
                        if (dt > info.LastUpdated)
                        {
                            info.LastUpdated = dt;
                            changed.Add(info);
                        }
                        break;
                    }
                }
                catch (Exception e)
                {
                    info.Error = e.Message;
                }
            }

            return changed;
        }

        public void SendEmails(List<FeedInfo> updated)
        {
            foreach (var info in updated)
            {
                try
                {
                    SendMailHelper.SendEmail(_settings.SmtpHost, _settings.SmtpPort, _settings.UserName, _settings.Password,
                        _settings.FromEmailAddress, _settings.ToEmailAddress, "RSS feed updated on " + info.LastUpdated.ToShortDateString(),
                        info.Url);
                }
                catch (Exception e)
                {
                    info.Error = e.Message;
                }
            }
        }
    }
}
