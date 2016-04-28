using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Xml;
using System.Xml;
using System.Xml.Linq;
using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace LogViewer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        XDocument data;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void OnOpenFile(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            button.IsEnabled = false;
                
            FileOpenPicker fo = new FileOpenPicker();
            fo.ViewMode = PickerViewMode.Thumbnail;
            fo.FileTypeFilter.Add(".csv");
            StorageFile file = await fo.PickSingleFileAsync();
            if (file != null)
            {
                await LoadCsvFile(file);
            }

            button.IsEnabled = true;
        }

        
        private async Task LoadCsvFile(StorageFile file)
        {
            try
            {
                ShowStatus("Loading " + file.Path);
                using (Stream s = await file.OpenStreamForReadAsync())
                {
                    XmlNameTable nametable = new NameTable();
                    using (XmlCsvReader reader = new XmlCsvReader(s, System.Text.Encoding.UTF8, new Uri(file.Path), nametable))
                    {
                        reader.FirstRowHasColumnNames = true;
                        data = XDocument.Load(reader);

                        string[] names = reader.ColumnNames;
                        if (names != null)
                        {
                            CategoryList.ItemsSource = names;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatus(ex.Message);
            }
            ShowStatus("");
        }

        private void ShowStatus(string message)
        {
            var nowait = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                StatusText.Text = message;
            }));
        }

        private void OnListItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                string name = (string)e.AddedItems[0];
                string xname = XmlConvert.EncodeLocalName(name);

                double x = 0;
                List<DataValue> list = new List<DataValue>();
                foreach (var node in data.Descendants(xname))
                {
                    string value = (string)node;
                    double y = 0;
                    if (double.TryParse(value, out y))
                    {
                        list.Add(new DataValue() { X = x, Y = y });
                        x++;
                    }
                }
                Chart.SetData(list);
            }
        }
    }
}
