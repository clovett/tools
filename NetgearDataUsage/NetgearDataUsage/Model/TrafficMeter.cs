using Microsoft.Storage;
using Sgml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace NetgearDataUsage.Model
{
    public class DailyTraffic
    {
        public DailyTraffic() { }

        [XmlAttribute]
        public DateTime Date { get; set; }


        [XmlAttribute]
        public double Upload { get; set; }

        [XmlAttribute]
        public double Download { get; set; }
    }

    public class TrafficMeter
    {
        StorageFile file;
        static string Heading = "Internet Traffic Statistics";
        static string CurrentDatePrefix= "Current Date/Time:";
        static string TodayHeader = "Today";
        static string YesterdayHeader = "Yesterday";

        public TrafficMeter()
        {
            Data = new List<DailyTraffic>();
        }

        public List<DailyTraffic> Data { get; set; }

        public StorageFile File { get { return file; } }

        private void AddRecord(DailyTraffic numbers)
        {
            DateTime date = numbers.Date;

            DailyTraffic dt = GetRow(date);
            if (dt == null)
            {
                bool added = false;
                // insert new row in order.
                for (int i = Data.Count - 1; i >= 0; i--)
                {
                    dt = Data[i];
                    if (date < dt.Date)
                    {
                        Data.Insert(i, numbers);
                        added = true;
                        break;
                    }
                }
                if (!added)
                {
                    Data.Add(numbers);
                }
            }
            else
            {
                // the numbers can't go down, unfortunately, the router has a bug and sometimes forgets
                // yesterday's numbers...
                dt.Upload = Math.Max(dt.Upload, numbers.Upload);
                dt.Download = Math.Max(dt.Download, numbers.Download);
            }
        }

        public DailyTraffic GetRow(DateTime date)
        {
            DateTime dt = date.Date;
            return (from e in Data where e.Date == dt select e).FirstOrDefault();
        }

        public static async Task<TrafficMeter> LoadAsync(StorageFile file)
        {
            var store = new IsolatedStorage<TrafficMeter>();
            TrafficMeter result = await store.LoadFromFileAsync(file);
            if (result == null)
            {
                result = new TrafficMeter();
                await result.SaveAsync(file);
            }
            result.file = file;
            return result;
        }

        public async Task SaveAsync(StorageFile file)
        {
            var store = new IsolatedStorage<TrafficMeter>();
            await store.SaveToFileAsync(file, this);
            this.file = file;
        }

        /// <summary>
        /// Get current traffic data
        /// </summary>
        /// <param name="credential"></param>
        /// <returns>Returns false if web credential didn't work and re-authentication is required.</returns>
        public async Task<bool> GetTrafficMeter(WebCredential credential)
        {
            try
            { 
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

                HttpResponseMessage response = await client.GetAsync(Settings.Instance.TrafficMeterUri);
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
                    string realm = GetBasicRealm(response);
                    if (realm != null)
                    {
                        // prompt for userid and password.
                        credential.Realm = realm;
                        return false;
                    }
                    else
                    {
                        throw new Exception("Access denied and basic authentication is not supported by this router");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Exception from {0}: ", Settings.Instance.TrafficMeterUri, ex.Message));
            }
            return true;
        }

        private string DecodeHtml(byte[] data, string mediaType, string charSet)
        {
            if (string.Compare(mediaType, "text/html", StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new Exception("Unexpected media type returned, expecting 'text/html' but received: " + mediaType);
            }
            charSet = ("" + charSet).Trim('"');
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

        private async void UpdateModel(string html)
        {
            this.ScrapeDataFromHtmlPage(html);
            await this.SaveAsync(this.file);
        }

        public void ScrapeDataFromHtmlPage(string html)
        {
            XDocument doc = ParseHtml(html);

            XElement heading = (from e in doc.Descendants() where e.Value == Heading select e).FirstOrDefault();
            if (heading == null)
            {
                throw new Exception(string.Format("Could not find expected heading: '{0}'", Heading));
            }

            XElement currentDate = (from e in doc.Descendants() where e.Value.Trim().StartsWith(CurrentDatePrefix) select e).FirstOrDefault();
            if (currentDate == null)
            {
                throw new Exception(string.Format("Could not find element starting wtih: '{0}'", CurrentDatePrefix));
            }

            DateTime date = DateTime.Parse(currentDate.Value.Trim().Substring(CurrentDatePrefix.Length));
            
            XElement today = (from e in doc.Descendants() where e.Value.StartsWith(TodayHeader) select e).FirstOrDefault();
            if (today == null)
            {
                throw new Exception(string.Format("Could not find element containing: '{0}'", TodayHeader));
            }

            // these are partial (but still interesting).
            DailyTraffic todaysNumbers  = GetNumbers(today);
            todaysNumbers.Date = date.Date;

            XElement yesterday = (from e in doc.Descendants() where e.Value.StartsWith(YesterdayHeader) select e).FirstOrDefault();
            if (yesterday == null)
            {
                throw new Exception(string.Format("Could not find element containing: '{0}'", YesterdayHeader));
            }

            // these are complete
            DailyTraffic yesterdaysNumbers = GetNumbers(yesterday);
            yesterdaysNumbers.Date = date.AddDays(-1).Date;

            AddRecord(todaysNumbers);
            AddRecord(yesterdaysNumbers);
        }

        private XElement GetAncestorByName(XElement e, string name)
        {
            foreach (var p in e.Ancestors())
            {
                if (p.Name.LocalName == name)
                {
                    return p;
                }
            }
            return null;
        }

        private DailyTraffic GetNumbers(XElement today)
        {
            DailyTraffic result = new DailyTraffic();

            XElement td = GetAncestorByName(today, "td");
            int count = 0;
            foreach (XElement cell in td.ElementsAfterSelf().Skip(1))
            {
                string value = cell.Value;
                double v = 0;
                double.TryParse(value, out v);
                if (count == 0)
                {
                    result.Upload = v;
                }
                else
                {
                    result.Download = v;
                }
                count++;
                if (count == 2)
                {
                    break;
                }
            }
            return result;
        }

        private XDocument ParseHtml(string html)
        {
            try
            {
                Sgml.SgmlReader reader = new Sgml.SgmlReader()
                {
                    DocType = "html",
                    InputStream = new StringReader(html)
                };

                XDocument doc = XDocument.Load(reader);
                return doc;
            }
            catch (Exception)
            {
                Debug.WriteLine("Error converting HTML to XML");
            }
            return null;
        }
    }
}
