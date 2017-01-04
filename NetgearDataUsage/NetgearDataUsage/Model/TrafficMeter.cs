using Microsoft.Storage;
using Sgml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        static string Heading = "Internet Traffic Statistics";
        static string CurrentDatePrefix= "Current Date/Time:";
        static string TodayHeader = "Today";
        static string YesterdayHeader = "Yesterday";

        public TrafficMeter()
        {
            Data = new List<DailyTraffic>();
        }

        public List<DailyTraffic> Data { get; set; }

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
                dt.Upload = numbers.Upload;
                dt.Download = numbers.Download;
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
            return result;
        }

        public async Task SaveAsync(StorageFile file)
        {
            var store = new IsolatedStorage<TrafficMeter>();
            await store.SaveToFileAsync(file, this);
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
