using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;

namespace NewBrowseFolderDialog
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

        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            // Display a CommonOpenFileDialog to select only folders 
            CommonOpenFileDialog cfd = new CommonOpenFileDialog();
            cfd.EnsureReadOnly = true;
            cfd.IsFolderPicker = true;
            cfd.AllowNonFileSystemItems = true;

            if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                TextOutput.Text = "result=" + cfd.FileName;
            }
        }
    }
}
