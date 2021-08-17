using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Ink;
using System.Windows.Markup;
using ToonBuilder.ColorPicker;
using Microsoft.Win32;
using ToonBuilder.Model;
using System.Xml.Linq;
using System.IO;

namespace ToonBuilder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public readonly static RoutedUICommand CommandAddShape;

        static MainWindow()
        {
            CommandAddShape = new RoutedUICommand("AddShape", "AddShape", typeof(MainWindow));
        }

        public MainWindow()
        {
            InitializeComponent();
            ButtonArrange.ContextMenu.PlacementTarget = ButtonArrange;
        }

        private void OnNew(object sender, ExecutedRoutedEventArgs e)
        {
            this.SceneEditor.Clear();
        }

        private void OnDelete(object sender, ExecutedRoutedEventArgs e)
        {
            this.SceneEditor.DeleteSelection();
        }

        private void ButtonArrange_Checked(object sender, RoutedEventArgs e)
        {
            ButtonArrange.ContextMenu.IsOpen = true;
        }

        private void ButtonArrange_Unchecked(object sender, RoutedEventArgs e)
        {
            ButtonArrange.ContextMenu.IsOpen = false;
        }


        private string fileName;

        private void OnFileOpen(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog od = new OpenFileDialog();
            od.Filter = "Toon Files (*.toon)|*.toon|Images (.png,*.jpg)|*.png,*.jpg";
            if (od.ShowDialog() == true)
            {
                OpenFile(od.FileName);
            }
        }
        private void OnCanSave(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrEmpty(fileName);
        }

        private void OnFileSaveAs(object sender, ExecutedRoutedEventArgs e)
        {
            SaveFileDialog sd = new SaveFileDialog();
            sd.Filter = "Toon Files (*.toon)|*.toon|XAML Files (*.xaml)|*.saml";
            if (sd.ShowDialog() == true)
            {
                SaveFile(sd.FileName);
            }
        }

        private void OnFileSave(object sender, ExecutedRoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.fileName))
            {
                OnFileSaveAs(sender, e);
            }
            else
            {
                SaveFile(this.fileName);
            }
        }

        private void OnFileClose(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void OpenFile(string fileName)
        {
            string ext = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            if (ext == ".png" || ext == ".jpg" || ext == ".bmp" || ext == ".gif" || ext == ".jpeg")
            {
                BackgroundImage.Source = new BitmapImage(new Uri(fileName));
            }
            else if (ext == ".toon")
            {
                // todo: load a .toon file.
            }
        }

        private void SaveFile(string fileName)
        {
            try
            {
                string ext = System.IO.Path.GetExtension(fileName);

                if (string.Compare(ext, ".xaml", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    XDocument doc = SceneEditor.ToXaml();
                    doc.Save(fileName);
                }
                else
                {
                    ToonSerializer.Save(SceneEditor, fileName);
                }
                this.fileName = fileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnPaste(object sender, ExecutedRoutedEventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                try
                {
                    var bitmap = Clipboard.GetImage();
                    if (bitmap != null)
                    {
                        BackgroundImage.Source = bitmap;
                        SceneEditor.Background = Brushes.Transparent;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Paste Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if (Clipboard.ContainsText())
            {
                try
                {
                    var xaml = Clipboard.GetText();
                    if (!string.IsNullOrEmpty(xaml))
                    {
                        SceneEditor.PasteXaml(xaml);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Paste Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnCopy(object sender, ExecutedRoutedEventArgs e)
        {
            this.SceneEditor.CopySelection();
        }
    }
}
