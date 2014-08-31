using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Utilities
{
    public class Settings
    {
        const string SettingsFileName = "settings.xml";

        public Settings() { } // for serializer

        public string LogToken { get; set; }

        public string LogFileName { get; set; }

        public static async Task<Settings> LoadAsync()
        {
            Settings data = await IsolatedStorage<Settings>.LoadFromFileAsync(SettingsFileName);
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
                await IsolatedStorage<Settings>.SaveToFileAsync(SettingsFileName, this);
            }
            catch
            {
            }
        }

    }
}
