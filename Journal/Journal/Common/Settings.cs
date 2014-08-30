using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Phone.BatteryStretcher.Common
{
    public class Settings
    {
        const string SettingsFileName = "settings.xml";

        public Settings() { } // for serializer

        public string LogToken { get; set; }

        public string LogFileName { get; set; }

        public static async Task<Settings> LoadAsync()
        {
            IsolatedStorage<Settings> store = new IsolatedStorage<Settings>();
            Settings data = await store.LoadFromFileAsync(SettingsFileName);
            if (data == null)
            {
                data = new Settings();
            }
            return data;
        }

        public async Task SaveAsync()
        {
            try
            {
                IsolatedStorage<Settings> store = new IsolatedStorage<Settings>();
                await store.SaveToFileAsync(SettingsFileName, this);
            }
            catch
            {
            }
        }

    }
}
