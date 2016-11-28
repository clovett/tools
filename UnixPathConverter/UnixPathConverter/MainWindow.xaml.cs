using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UnixPathConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool programaticChange;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnUnixPathChanged(object sender, TextChangedEventArgs e)
        {
            if (!programaticChange)
            {
                UpdateWindowsPath(UnixPath.Text);
            }
        }

        private void OnWindowsPathChanged(object sender, TextChangedEventArgs e)
        {
            if (!programaticChange)
            {
                UpdateUnixPath(WindowsPath.Text);
            }
        }

        private void UpdateWindowsPath(string unixPath)
        {
            string[] parts = unixPath.Split('/');
            string windowsPath = "";
            int offset = 0;
            if (parts.Length > 2 && parts[0] == "" && parts[1] == "mnt")
            {
                offset = 3;
                windowsPath = parts[2] + ":\\";
            }
            windowsPath += string.Join("\\", parts, offset, parts.Length - offset);
            programaticChange = true;
            WindowsPath.Text = windowsPath;
            programaticChange = false;
        }

        private void UpdateUnixPath(string windowsPath)
        {
            string unixpath = windowsPath.Replace('\\', '/');
            if (unixpath.Length > 1 && unixpath[1] == ':')
            {
                unixpath = "/mnt/" + unixpath[0] + "/" + unixpath.Substring(3);
            }
            programaticChange = true;
            UnixPath.Text = unixpath;
            programaticChange = false;
        }
    }
}
