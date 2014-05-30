using System;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Walkabout.Controls;
using Walkabout.Utilities;

namespace BatchPhotoScan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, System.Windows.Forms.IWin32Window
    {
        public readonly static RoutedUICommand CommandRotateRight = new RoutedUICommand("Rotate Right", "CommandRotateRight", typeof(MainWindow));
        public readonly static RoutedUICommand CommandRotateLeft = new RoutedUICommand("Rotate Left", "CommandRotateLeft", typeof(MainWindow));
        public readonly static RoutedUICommand CommandCropImage = new RoutedUICommand("Crop", "CommandCropImage", typeof(MainWindow));
        public readonly static RoutedUICommand CommandScan = new RoutedUICommand("Scan ", "CommandScan", typeof(MainWindow));

        private Resizer resizer;
        private bool dirty;
        private AttachmentDialogItem selected;
        private string directory;
        private int nextImageIndex;
        private Brush resizerBrush;
        const double ResizerThumbSize = 12;

        public MainWindow()
        {
            InitializeComponent();

            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            Directory = path;

            CanvasGrid.PreviewMouseDown += new MouseButtonEventHandler(CanvasGrid_MouseDown);

            SelectPageSize("PaperSize4x6");

            resizerBrush = (Brush)Resources["ResizerThumbBrush"];
        }

        void CanvasGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(CanvasGrid);
            HitTestResult result = VisualTreeHelper.HitTest(CanvasGrid, pos);

            DependencyObject hit = result.VisualHit;
            if (hit != null)
            {
                AttachmentDialogItem item = WpfHelper.FindAncestor<AttachmentDialogItem>(hit);
                if (item != null && this.selected != item)
                {
                    SelectItem(item);
                    return;
                }
                if (hit == resizer)
                {
                    return;
                }
            }
        }

        private void OnCanvasMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Delta < 0)
                {
                    ZoomOut(this, e);
                }
                else if (e.Delta > 0)
                {
                    ZoomIn(this, e);
                }
                e.Handled = true;
            }
        }

        private int ItemCount
        {
            get
            {
                int i = 0;
                foreach (UIElement e in Canvas.Children)
                {
                    AttachmentDialogItem item = e as AttachmentDialogItem;
                    if (item != null)
                    {
                        i++;
                    }
                }
                return i;
            }
        }

        void LayoutContent()
        {
            // Let the wrap panel do it's thing.
            Canvas.UpdateLayout();

            if (resizer != null)
            {
                MoveResizer(this.selected, GetUnscaledBounds(resizer.Bounds));
            }
        }

        void SelectItem(AttachmentDialogItem item)
        {
            this.selected = item;
            AddResizer(item, GetItemContentBounds(item));
        }

        private void ClearSelection()
        {
            selected = null;
            RemoveResizer();
        }

        public string Directory
        {
            get { return directory; }
            set { directory = value; OnDirectoryChanged(); }
        }

        private void OnDirectoryChanged()
        {
            int max = 0;

            // scan the directory and find the largest file index.
            foreach (string name in System.IO.Directory.GetFiles(this.directory))
            {
                string fname = Path.GetFileNameWithoutExtension(name);
                for (int i = fname.Length - 1; i >= 0; i--)
                {
                    if (!Char.IsDigit(fname[i]))
                    {
                        string number = fname.Substring(i + 1);
                        int n;
                        if (int.TryParse(number, out n))
                        {
                            max = Math.Max(n, max);
                        }
                    }
                }
            }

            this.nextImageIndex = max + 1;
        }

        WIA.ICommonDialog cdc = new WIA.CommonDialog();
        WIA.Device scanner;
        bool scanning;

        private void CanScanImage(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !scanning;
        }

        private void OnScanImage(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Scan();
            }
            catch (Exception ex)
            {
                string message = GetWiaErrorMessage(ex);
                MessageBox.Show(this, message, "Scan Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Scan()
        {
            scanning = true;
            try
            {
                CommandManager.InvalidateRequerySuggested();
                if (scanner == null)
                {
                    scanner = cdc.ShowSelectDevice(DeviceType: WIA.WiaDeviceType.ScannerDeviceType, AlwaysSelectDevice: true);
                }
                WIA.ImageFile imageFile = null;
                if (scanner != null)
                {
                    WIA.Item scannerItem = scanner.Items[1];

                    SetDPI(scannerItem, 600);
                    SetScannerBounds(scannerItem, 0, 0, (int)(600 * selectedPageSize.Width), (int)(600 * selectedPageSize.Height));
                    const string wiaFormatPNG = "{B96B3CAF-0728-11D3-9D7B-0000F81EF32E}";
                    object scanResult = cdc.ShowTransfer(scannerItem, wiaFormatPNG, false);
                    imageFile = (WIA.ImageFile)scanResult;
                }

                if (imageFile != null)
                {
                    string temp = System.IO.Path.GetTempFileName();
                    if (File.Exists(temp))
                    {
                        File.Delete(temp);
                    }
                    imageFile.SaveFile(temp);
                    TempFilesManager.AddTempFile(temp);

                    AttachmentDialogImageItem image = new AttachmentDialogImageItem(temp, true);
                    AddItem(image);
                    LayoutContent();
                    SelectItem(image);
                    //AutoCrop(image);
                    ZoomToFit(this, new RoutedEventArgs());
                    SetDirty();
                }
            }
            catch
            {
                scanner = null;
            }
            finally
            {
                scanning = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private static void SetDPI(WIA.IItem scannnerItem, int scanResolutionDPI)
        {
            const string WIA_HORIZONTAL_SCAN_RESOLUTION_DPI = "6147";
            const string WIA_VERTICAL_SCAN_RESOLUTION_DPI = "6148";

            SetWIAProperty(scannnerItem.Properties, WIA_HORIZONTAL_SCAN_RESOLUTION_DPI, scanResolutionDPI);
            SetWIAProperty(scannnerItem.Properties, WIA_VERTICAL_SCAN_RESOLUTION_DPI, scanResolutionDPI);
        }

        private static void SetScannerBounds(WIA.IItem scannnerItem, int scanStartLeftPixel, int scanStartTopPixel,
                                                  int scanWidthPixels, int scanHeightPixels)
        {
            const string WIA_HORIZONTAL_SCAN_START_PIXEL = "6149";
            const string WIA_VERTICAL_SCAN_START_PIXEL = "6150";
            const string WIA_HORIZONTAL_SCAN_SIZE_PIXELS = "6151";
            const string WIA_VERTICAL_SCAN_SIZE_PIXELS = "6152";

            SetWIAProperty(scannnerItem.Properties, WIA_HORIZONTAL_SCAN_START_PIXEL, scanStartLeftPixel);
            SetWIAProperty(scannnerItem.Properties, WIA_VERTICAL_SCAN_START_PIXEL, scanStartTopPixel);
            SetWIAProperty(scannnerItem.Properties, WIA_HORIZONTAL_SCAN_SIZE_PIXELS, scanWidthPixels);
            SetWIAProperty(scannnerItem.Properties, WIA_VERTICAL_SCAN_SIZE_PIXELS, scanHeightPixels);
        }

        private static void SetScannerBrightness(WIA.IItem scannnerItem, int brightnessPercents)
        {
            const string WIA_SCAN_BRIGHTNESS_PERCENTS = "6154";

            SetWIAProperty(scannnerItem.Properties, WIA_SCAN_BRIGHTNESS_PERCENTS, brightnessPercents);
        }

        private static void SetScannerContrast(WIA.IItem scannnerItem, int contrastPercents)
        {
            const string WIA_SCAN_CONTRAST_PERCENTS = "6155";

            SetWIAProperty(scannnerItem.Properties, WIA_SCAN_CONTRAST_PERCENTS, contrastPercents);
        }

        private static void SetWIAProperty(WIA.IProperties properties, object propName, object propValue)
        {
            WIA.Property prop = properties.get_Item(ref propName);
            prop.set_Value(ref propValue);
        }


        private void SetDirty()
        {
            dirty = true;
            CommandManager.InvalidateRequerySuggested();
        }

        private string GetWiaErrorMessage(Exception ex)
        {
            COMException ce = ex as COMException;
            if (ce != null)
            {
                int hresult = ce.ErrorCode;
                int wiaRange = (1 << 31) | (33 << 16);

                if ((int)(hresult & 0xffff0000) == wiaRange)
                {
                    switch ((hresult) & 0xffff)
                    {
                        case 1: // WIA_ERROR_GENERAL_ERROR
                            return "An unknown error has occurred with the scanning device.";
                        case 2: // WIA_ERROR_PAPER_JAM
                            return "Paper is jammed in the scanner's document feeder.";
                        case 3: // WIA_ERROR_PAPER_EMPTY
                            return "The user requested a scan and there are no documents left in the document feeder.";
                        case 4: // WIA_ERROR_PAPER_PROBLEM
                            return "An unspecified problem occurred with the scanner's document feeder";
                        case 5: // WIA_ERROR_OFFLINE
                            return "The scanning device is not online.";
                        case 6: // WIA_ERROR_BUSY
                            return "The scanning device is busy.";
                        case 7: // WIA_ERROR_WARMING_UP
                            return "The scanner is warming up.";
                        case 8: // WIA_ERROR_USER_INTERVENTION
                            return "An unspecified error has occurred with the scanner that requires user intervention. The user should ensure that the device is turned on, online, and any cables are properly connected.";
                        case 9: // WIA_ERROR_ITEM_DELETED
                            return "The scanner device was deleted. It can no longer be accessed";
                        case 10: // WIA_ERROR_DEVICE_COMMUNICATION
                            return "An unspecified error occurred during an attempted communication with the scanner.";
                        case 11: // WIA_ERROR_INVALID_COMMAND
                            return "The device does not support this command";
                        case 12: // WIA_ERROR_INCORRECT_HARDWARE_SETTING
                            return "There is an incorrect setting on the scanner.";
                        case 13: // WIA_ERROR_DEVICE_LOCKED
                            return "The scanner head is locked";
                        case 14: // WIA_ERROR_EXCEPTION_IN_DRIVER
                            return "The device driver threw an exception.";
                        case 15: // WIA_ERROR_INVALID_DRIVER_RESPONSE
                            return "The response from the driver is invalid.";
                        case 16: // WIA_ERROR_COVER_OPEN
                            return "The cover is open";
                        case 17: // WIA_ERROR_LAMP_OFF
                            return "The lamp is off";
                        case 18: // WIA_ERROR_DESTINATION
                            return "Error in scanner destination";
                        case 19: // WIA_ERROR_NETWORK_RESERVATION_FAILED
                            return "Network reservation failed";
                    }
                }
            }
            return "Unexpected error with scanner: " + ex.Message;
        }

        private void AddItem(AttachmentDialogItem item)
        {
            item.Margin = new Thickness(10);
            item.ContentChanged += OnContentChanged;
            Canvas.Children.Add(item);
        }

        void OnContentChanged(object sender, EventArgs e)
        {
            var item = (AttachmentDialogItem)sender;
            OnItemChanged(item);
        }

        private void OnItemChanged(AttachmentDialogItem item)
        {
            if (item != null)
            {
                this.selected.InvalidateArrange();
                LayoutContent();
                SelectItem(this.selected); // fix up the resizer.
                this.SetDirty();
            }
        }

        private void OnItemRemoved(AttachmentDialogItem item)
        {
            Canvas.Children.Remove(item);
            if (item == selected)
            {
                RemoveResizer();
                selected = null;
            }
        }

        /// <summary>
        /// Add or more the resizer so it is aligned with the given image.
        /// </summary>
        private void AddResizer(AttachmentDialogItem item, Rect cropBounds)
        {
            if (resizer == null)
            {
                resizer = new Resizer();
                resizer.BorderBrush = resizer.ThumbBrush = this.resizerBrush;
                resizer.ThumbSize = ResizerThumbSize;
                resizer.Resized += OnResized;
                resizer.Resizing += OnResizing;
                this.Adorners.Children.Add(resizer);
            }

            MoveResizer(item, cropBounds);
        }

        private void MoveResizer(AttachmentDialogItem item, Rect resizerBounds)
        {
            // Transform resizer so it is anchored around the selected image.
            if (resizer != null && item != null)
            {
                resizer.LimitBounds = GetScaledBounds(item.ResizeLimit);
                resizer.Bounds = GetScaledBounds(resizerBounds);
                resizer.InvalidateArrange();
            }
        }

        void OnResized(object sender, EventArgs e)
        {
            SetDirty();
        }

        void OnResizing(object sender, EventArgs e)
        {
            if (this.selected != null && this.selected.LiveResizable)
            {
                this.selected.Resize(resizer.Bounds);
            }
        }

        private void RemoveResizer()
        {
            if (resizer != null)
            {
                this.Adorners.Children.Remove(resizer);
                resizer = null;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            if (dirty)
            {
                Save(this, null);
            }

            TempFilesManager.Cleanup();
        }

        private void ZoomToFit(object sender, RoutedEventArgs e)
        {
            ScaleTransform scale = Canvas.LayoutTransform as ScaleTransform;
            if (scale == null)
            {
                Canvas.LayoutTransform = scale = new ScaleTransform(1, 1);
            }
            if (resizer != null)
            {
                Rect bounds = GetUnscaledBounds(resizer.Bounds);
                Rect gridBounds = new Rect(0, 0, Scroller.ActualWidth - 10, Scroller.ActualHeight - 10);
                double xscale = gridBounds.Width / bounds.Width;
                double yscale = gridBounds.Height / bounds.Height;
                double minScale = Math.Min(xscale, yscale);
                scale.ScaleX = scale.ScaleY = minScale;
                CanvasGrid.UpdateLayout();
                MoveResizer(selected, bounds);
            }
        }


        private void ZoomIn(object sender, RoutedEventArgs e)
        {
            ScaleTransform scale = Canvas.LayoutTransform as ScaleTransform;
            if (scale == null)
            {
                Canvas.LayoutTransform = scale = new ScaleTransform(1, 1);
            }
            if (resizer != null)
            {
                Rect bounds = GetUnscaledBounds(resizer.Bounds);
                scale.ScaleX = scale.ScaleY = (scale.ScaleY * 1.1);
                CanvasGrid.UpdateLayout();
                MoveResizer(selected, bounds);
            }
        }

        private void ZoomOut(object sender, RoutedEventArgs e)
        {
            ScaleTransform scale = Canvas.LayoutTransform as ScaleTransform;
            if (scale == null)
            {
                Canvas.LayoutTransform = scale = new ScaleTransform(1, 1);
            }
            if (resizer != null)
            {
                Rect bounds = GetUnscaledBounds(resizer.Bounds);
                scale.ScaleX = scale.ScaleY = (scale.ScaleY / 1.1);
                CanvasGrid.UpdateLayout();
                MoveResizer(selected, bounds);
            }
        }

        private string GetNextImageFileName()
        {
            return Path.Combine(this.directory, "Scan" + this.nextImageIndex++);
        }

        private void Save(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                foreach (UIElement element in Canvas.Children)
                {
                    AttachmentDialogItem item = element as AttachmentDialogItem;
                    if (item != null)
                    {
                        AttachmentDialogImageItem img = item as AttachmentDialogImageItem;
                        if (img != null)
                        {
                            if (item == selected && resizer != null)
                            {
                                // crop it.
                                Rect bounds = GetUnscaledBounds(resizer.Bounds);
                                img.Resize(bounds);
                            }
                        }

                        if (string.IsNullOrEmpty(item.FileName))
                        {
                            item.Save(GetNextImageFileName());
                        }
                        else
                        {
                            item.Save(item.FileName);
                        }
                    }
                }

                ClearSelection();
                Canvas.Children.Clear();
                dirty = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Unexpected error saving new image: " + ex.Message, "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Get the bounds of the content of this item in unscaled coordinates, relative
        /// to the top left of the item.
        /// </summary>
        /// <param name="item">The item whose content bounds we want</param>
        /// <returns>The bounds</returns>
        private Rect GetItemContentBounds(AttachmentDialogItem item)
        {
            Size size = item.Content.DesiredSize;
            return new Rect(0, 0, size.Width, size.Height);
        }

        private Rect GetScaledBounds(Rect unscaledBounds)
        {
            Rect bounds = unscaledBounds;
            return selected.Content.TransformToAncestor(CanvasGrid).TransformBounds(unscaledBounds);
        }

        private Rect GetUnscaledBounds(Rect scaledBounds)
        {
            Rect bounds = CanvasGrid.TransformToDescendant(selected.Content).TransformBounds(scaledBounds);
            if (bounds.Left < 0)
            {
                bounds.Width += bounds.Left;
                bounds.X = 0;
            }
            if (bounds.Top < 0)
            {
                bounds.Height += bounds.Top;
                bounds.Y = 0;
            }

            return bounds;
        }

        private void Delete(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                AttachmentDialogItem item = this.selected;
                if (item != null)
                {
                    string filePath = item.FileName;
                    DeleteFile(filePath);
                    OnItemRemoved(item);
                    SetDirty();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error deleting file: " + ex.Message, "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    if (File.GetAttributes(filePath) == FileAttributes.ReadOnly)
                    {
                        File.SetAttributes(filePath, FileAttributes.Normal);
                    }

                    File.Delete(filePath);
                }
                catch
                {
                    // perhaps file is locked?
                    TempFilesManager.AddTempFile(filePath);
                }
            }
        }

        private void Cut(object sender, ExecutedRoutedEventArgs e)
        {
            Copy(sender, e);
            Delete(sender, e);
        }

        private void Copy(object sender, ExecutedRoutedEventArgs e)
        {
            if (selected != null)
            {
                selected.Copy();
            }
        }

        private void Paste(object sender, ExecutedRoutedEventArgs e)
        {
            AttachmentDialogItem newItem = null;

            if (Clipboard.ContainsImage())
            {
                var image = Clipboard.GetImage();
                if (image != null)
                {
                    // for some reason this bitmap doesn't paint unless I save and reload it
                    // the in-memory copy from the clipboard is a bit touchy, probably comes from
                    // another process and so on, so persistence is better strategy here...
                    string path = System.IO.Path.GetTempFileName();

                    AttachmentDialogImageItem item = new AttachmentDialogImageItem(image);
                    item.Save(path);

                    TempFilesManager.AddTempFile(path);

                    newItem = new AttachmentDialogImageItem(path, true);
                }
            }

            if (newItem != null)
            {
                AddItem(newItem);
                LayoutContent();
                SelectItem(newItem);
                SetDirty();
            }
        }

        private void HasSelectedItem(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (selected != null);
        }

        private void ClipboardHasData(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Clipboard.ContainsImage() ||
                Clipboard.ContainsData(DataFormats.Xaml) ||
                Clipboard.ContainsData(DataFormats.Rtf) ||
                Clipboard.ContainsData(DataFormats.Text);
        }

        private void HasSelectedImage(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (selected is AttachmentDialogImageItem);
        }

        private void CanSave(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = dirty && this.ItemCount > 0;
        }


        private void RotateRight(object sender, ExecutedRoutedEventArgs e)
        {
            AttachmentDialogImageItem img = this.selected as AttachmentDialogImageItem;
            if (img == null)
            {
                return;
            }
            img.RotateImage(90);
            OnItemChanged(img);
        }

        private void RotateLeft(object sender, ExecutedRoutedEventArgs e)
        {
            AttachmentDialogImageItem img = this.selected as AttachmentDialogImageItem;
            if (img == null)
            {
                return;
            }
            img.RotateImage(-90.0);
            OnItemChanged(img);
        }

        const double PrintMargin = 10;

        private void Print(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.selected == null)
            {
                return;
            }

            Rect bounds = GetItemContentBounds(this.selected);
            double w = bounds.Width;
            double h = bounds.Height;

            FrameworkElement visual = this.selected.CloneContent();
            visual.Margin = new Thickness(PrintMargin);
            visual.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            visual.Arrange(new Rect(0, 0, w + (2 * PrintMargin), h + (2 * PrintMargin)));

            PrintDialog pd = new PrintDialog();
            // pd.Owner = this; // bugbug, print dialog is missing this?
            if (pd.ShowDialog() == true)
            {
                pd.PrintVisual(visual, "Attachment");
            }
        }

        private void OnCropImage(object sender, ExecutedRoutedEventArgs e)
        {
            AttachmentDialogImageItem img = this.selected as AttachmentDialogImageItem;
            if (img == null)
            {
                return;
            }
            AutoCrop(img);
        }

        private void AutoCrop(AttachmentDialogImageItem img)
        {
            CannyEdgeDetector edgeDetector = new CannyEdgeDetector(img.Bitmap, 20, 80, 30);
            edgeDetector.DetectEdges();
            Rect bounds = edgeDetector.EdgeBounds;
            AddResizer(img, bounds);
            SetDirty();
        }

        private void OnFolderLinkClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fd = new System.Windows.Forms.FolderBrowserDialog();
            fd.SelectedPath = this.directory;
            if (fd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                this.Directory = fd.SelectedPath;
                FolderLink.Inlines.Clear();
                FolderLink.Inlines.Add(new Run(this.directory));
            }
        }


        public IntPtr Handle
        {
            get
            {
                IntPtr hwnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                return hwnd;
            }
        }

        class PageSizeEntry
        {
            public string Name;
            public double Width;
            public double Height;
        }

        PageSizeEntry[] pageSizes = new PageSizeEntry[] {
            new PageSizeEntry() { Name = "PaperSizeAll", Width = 0, Height = 0 },
            new PageSizeEntry() { Name = "PaperSizeLetter", Width = 8.5, Height = 11 },
            new PageSizeEntry() { Name = "PaperSizeA4", Width = 8.2677165228, Height = 11.69291336796 },
            new PageSizeEntry() { Name = "PaperSizeLegal", Width = 8.5, Height = 14 },
            new PageSizeEntry() { Name = "PaperSize5x7", Width = 5, Height = 7 },
            new PageSizeEntry() { Name = "PaperSize4x6", Width = 4, Height = 6 },
            new PageSizeEntry() { Name = "PaperSize3x7", Width = 3, Height = 7 },
        };


        private void OnPageSizeChanged(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            SelectPageSize((string)item.Tag);
        }

        PageSizeEntry selectedPageSize;

        private void SelectPageSize(string name)
        {
            PageSizeEntry entry = FindEntry(name);
            if (entry != null)
            {
                selectedPageSize = entry;

                foreach (MenuItem item in PageSizeButton.DropDownMenu.Items)
                {                    
                    if ((string)item.Tag== name)
                    {
                        item.IsChecked = true;
                    }
                    else
                    {
                        item.IsChecked = false;
                    }
                }
            }
        }

        private PageSizeEntry FindEntry(string name)
        {
            return (from e in pageSizes where e.Name == name select e).FirstOrDefault();
        }
    }

}
