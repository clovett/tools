using LovettSoftware.Utilities;
using System.Threading.Tasks;
using System.Windows;

namespace WpfGifBuilder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        Settings settings;

        protected override void OnStartup(StartupEventArgs e)
        {
            settings = Settings.Load();
            base.OnStartup(e);
        }
    }
}
