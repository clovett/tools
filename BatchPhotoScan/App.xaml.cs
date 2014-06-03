using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BatchPhotoScan
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static Settings _settings;

        public static Settings Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = Settings.LoadSettings();
                }
                return _settings;
            }
        }
    }
}
