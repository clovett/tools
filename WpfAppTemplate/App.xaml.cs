using LovettSoftware.Utilities;
using System.Threading.Tasks;
using System.Windows;

namespace WpfAppTemplate
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
    }
}
