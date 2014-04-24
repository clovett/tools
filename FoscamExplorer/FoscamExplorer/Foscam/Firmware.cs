using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.ApplicationModel;

namespace FoscamExplorer.Foscam
{
    public class Firmware
    {
        const string DataFile = "firmware.xml";

        public Firmware() { }

        public List<Update> Updates { get; set; }

        public static async Task<Firmware> LoadAsync()
        {
            var folder = await Package.Current.InstalledLocation.GetFolderAsync("Assets");
            var file = await folder.GetFileAsync(DataFile);
            Firmware data = await IsolatedStorage<Firmware>.LoadFromFile(file);
            if (data == null)
            {
                data = new Firmware();
            }
            return data;
        }


        internal Update FindUpdate(string currentVersion)
        {
            if (Updates == null)
            {
                return null;
            }
            foreach (Update u in Updates)
            {
                if (u.Matches(currentVersion))
                {
                    return u;
                }
            }
            return null;
        }
    }

    public class Update
    {
        public Update() { }

        [XmlAttribute("devices")]
        public string Devices { get; set; }

        [XmlAttribute("latest")]
        public string Latest { get; set; }

        [XmlAttribute("from")]
        public string From { get; set; }

        [XmlAttribute("download")]
        public string Download { get; set; }

        internal bool Matches(string currentVersion)
        {
            if (currentVersion == Latest) return true;
            if (Latest == null || currentVersion == null) return false;

            string[] parts = this.Latest.Split('.');

            string[] parts2 = currentVersion.Split('.');

            if (parts.Length != parts2.Length) return false;

            bool match = true;

            for (int i = 0, n = parts.Length; i < n; i++)
            {
                string a = parts[i];
                string b = parts2[i];

                // 'xx' is the magic string for matching any number in that field.
                if (a != "xx")
                {
                    int v = 0;
                    int w = 0;
                    if (int.TryParse(a, out v) && int.TryParse(b, out w) && v != w)
                    {
                        match = false;
                        break;
                    }
                }
            }

            return match;
        }
    }

}
