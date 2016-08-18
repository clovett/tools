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

namespace Prompt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ParseCommandLine();
        }

        bool ParseCommandLine()
        {
            string[] args = ((App)App.Current).Arguments;
            for(int i = 0, n = args.Length; i<n; i++)
            {
                string arg = args[i];
                if (arg[0] == '/' || arg[1] == '-')
                {
                    switch (arg.Substring(1))
                    {
                        case "h":
                        case "help":
                        case "?":
                            return false;
                        case "message":
                            if (i + 1 < n)
                            {
                                Message.Text = args[++i];
                            }
                            break;
                        case "ok":
                            if (i + 1 < n)
                            {
                                ButtonConfirm.Content = args[++i];
                            }
                            break;
                        case "cancel":
                            if (i + 1 < n)
                            {
                                ButtonConfirm.Content = args[++i];
                            }
                            break;
                        default:
                            Message.Text = "Unexpected argument: " + arg + "\n" + Message.Text;
                            return false;
                    }
                }
                else
                {
                    Message.Text = "Unexpected argument: " + arg + "\n" + Message.Text;
                    return false;
                }
            }
            return true;
        }

        private void OnConfirm(object sender, RoutedEventArgs e)
        {
            ((App)App.Current).ExitCode = 0;
            this.Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            ((App)App.Current).ExitCode = 1;
            this.Close();
        }
    }
}
