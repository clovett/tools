﻿using LovettSoftware.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WebLoadTest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        Settings settings;

        public async Task<Settings> LoadSettings()
        {
            if (this.settings == null)
            {
                this.settings = await Settings.LoadAsync();
            }
            return this.settings;
        }

        public Settings Settings {  get { return this.settings; } }
    }
}
