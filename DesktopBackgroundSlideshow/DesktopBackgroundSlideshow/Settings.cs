using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DesktopBackgroundSlideshow
{
    public class Settings
    {

        public Settings() { }

        public int Index { get; set; }

        public string Path { get; set; }

        public int Seed { get; set; }

        public static Settings Load(string filename)
        {
            if (System.IO.File.Exists(filename))
            {
                try
                {
                    XmlSerializer s = new XmlSerializer(typeof(Settings));
                    using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        return (Settings)s.Deserialize(stream);
                    }
                }
                catch { }
            }
            return new Settings();
        }

        public void Save(string filename)
        {
            try
            {
                XmlSerializer s = new XmlSerializer(typeof(Settings));
                using (FileStream stream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    s.Serialize(stream, this);
                }
            }
            catch {
            }
        }
    }
}
