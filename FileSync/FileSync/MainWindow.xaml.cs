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
using Microsoft.Win32;
using System.Diagnostics;

namespace FileSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnOpenFile(object sender, ExecutedRoutedEventArgs e)
        {
            string source = null;
            string target = null;

            System.Windows.Forms.FolderBrowserDialog fo = new System.Windows.Forms.FolderBrowserDialog();
            fo.Description = "Select Source Folder";
            if (fo.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                source = fo.SelectedPath;

                fo.Description = "Select Target Folder";
                if (fo.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    target = fo.SelectedPath;
                }

            }

        }
    }

}
