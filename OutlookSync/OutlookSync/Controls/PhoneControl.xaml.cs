using OutlookSync.Model;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OutlookSync.Controls
{
    /// <summary>
    /// Interaction logic for PhoneControl.xaml
    /// </summary>
    public partial class PhoneControl : UserControl
    {
        ConnectedPhone phone;

        public PhoneControl()
        {
            InitializeComponent();
            this.DataContextChanged += PhoneControl_DataContextChanged;
            this.TrustedPhoneImage.Visibility = System.Windows.Visibility.Collapsed;
            this.InSyncImage.Visibility = System.Windows.Visibility.Collapsed;
        }

        void PhoneControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (phone != null)
            {
                phone.PropertyChanged -= OnPhonePropertyChanged;
            }
            phone = (ConnectedPhone)e.NewValue;
            if (phone != null)
            {
                UpdateImages();
                phone.PropertyChanged += OnPhonePropertyChanged;
                UpdateButtonState();
            }
        }

        void UpdateButtonState()
        {
            ConnectButton.Visibility = (phone.Allowed || phone.SyncError != null) ? Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        void UpdateSyncError()
        {
            string message = phone.SyncError;
            if (!string.IsNullOrEmpty(message))
            {
                ErrorBorder.Visibility = System.Windows.Visibility.Visible;
                ErrorMessage.Text = message;
                UpdateButtonState();
            }
            else
            {
                ErrorBorder.Visibility = System.Windows.Visibility.Collapsed;
                UpdateButtonState();
            }
        }

        private void OnPhonePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (phone != null)
            {
                switch (e.PropertyName)
                {
                    case "Allowed":
                        UpdateButtonState();
                        UpdateImages();
                        break;
                    case "SyncStatus":
                        UpdateTiles(phone.SyncStatus);
                        break;
                    case "InSync":
                        UpdateImages();
                        break;
                    case "SyncError":
                        UpdateSyncError();
                        break;
                }
            }
        }

        private void UpdateImages()
        {
            if (phone != null)
            {
                if (phone.InSync)
                {
                    this.InSyncImage.Visibility = System.Windows.Visibility.Visible;
                    this.TrustedPhoneImage.Visibility = System.Windows.Visibility.Collapsed;
                }
                else if (phone.Allowed)
                {
                    this.InSyncImage.Visibility = System.Windows.Visibility.Collapsed;
                    TrustedPhoneImage.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    this.TrustedPhoneImage.Visibility = System.Windows.Visibility.Collapsed;
                    this.InSyncImage.Visibility = System.Windows.Visibility.Collapsed;
                }

            }
        }

        private void UpdateTiles(SyncResult syncResult)
        {
            if (syncResult != null)
            {
                AnimateCount(InsertIndicator, syncResult.GetTotalInserted());
                AnimateCount(UpdateIndicator, syncResult.GetTotalUpdated());
                AnimateCount(UnchangedIndicator, syncResult.Unchanged);
                AnimateCount(DeleteIndicator, syncResult.GetTotalDeleted());
            }
        }

        void AnimateCount(SyncProgressControl ctrl, List<SyncItem> list)
        {
            DoubleAnimation animation = new DoubleAnimation();
            animation.To = list == null ? 0 : list.Count;
            animation.Duration = new Duration(TimeSpan.FromSeconds(.3));

            ctrl.Tag = list;

            Storyboard s = new Storyboard();
            Storyboard.SetTarget(animation, ctrl);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Count"));
            s.Children.Add(animation);
            s.Begin();
        }  

        private void OnConnectClick(object sender, RoutedEventArgs e)
        {
            if (phone != null)
            {
                phone.Allowed = true;
            }
        }

        SyncProgressControl selected;

        private void OnIndicatorClick(object sender, MouseButtonEventArgs e)
        {
            SyncProgressControl ctrl = (SyncProgressControl)sender;
            var selectedTile = ctrl;
            var selectedList = (List<SyncItem>)ctrl.Tag;
            if (selectedList != null)
            {
                if (ctrl == selected)
                {
                    selected = null;
                    SyncDetailsBorder.Visibility = System.Windows.Visibility.Collapsed;
                    SyncDetails.Visibility = System.Windows.Visibility.Collapsed;                    
                }
                else
                {
                    ShowSyncDetails((SyncProgressControl)sender, selectedList, selected == null);
                    selected = ctrl;
                }
            }
        }

        private void ShowSyncDetails(SyncProgressControl syncProgressControl, List<SyncItem> selectedList, bool animate)
        {
            // Get the starting point for our pretty SyncDetailsBorder            
            Point progressLocation = syncProgressControl.TransformToAncestor(WhiteBorder).Transform(new Point(0, 0));

            const double margin = 20;
            const double cornerRadius = 15;
            double width = WhiteBorder.ActualWidth;
            double textBoxMarginX = SyncDetails.Margin.Left + SyncDetails.Margin.Right;
            double textBoxMarginY = SyncDetails.Margin.Top + SyncDetails.Margin.Bottom;
            double textBoxWidth = width - (2 * margin) - textBoxMarginX;
            double borderThickness = WhiteBorder.BorderThickness.Left;
            SyncDetails.Width = textBoxWidth;
            double height = SyncDetails.Height + textBoxMarginY;

            SyncDetailsBorder.Data = CreateDetailsBorderPath(new Point(progressLocation.X - borderThickness, -syncProgressControl.Margin.Bottom),
                syncProgressControl.ActualWidth - borderThickness,
                width - (2 * margin), height, margin, cornerRadius);


            SyncDetails.Foreground = syncProgressControl.TileForeground;
            SyncDetails.Background = syncProgressControl.TileBackground;
            SyncDetailsBorder.Fill = syncProgressControl.TileBackground;
            SyncDetailsBorder.Visibility = System.Windows.Visibility.Visible;
            SyncDetailsBorder.Margin = new Thickness(0, 0, 0, margin);
            SyncDetailsBorder.StrokeThickness = borderThickness;
            SyncDetailsBorder.Stroke = syncProgressControl.TileBackground;
            SyncDetails.Text = FormatSyncList(selectedList);

            // animate the height of the border using clip rect.
            if (animate)
            {
                SyncDetails.Visibility = System.Windows.Visibility.Collapsed;
                topDiff = syncProgressControl.Margin.Bottom + margin + SyncDetails.Margin.Bottom;
                DoubleAnimation a = new DoubleAnimation(0, height + margin + margin + 2, new Duration(TimeSpan.FromMilliseconds(200)));
                Storyboard sb = new Storyboard();
                Storyboard.SetTarget(a, this);
                Storyboard.SetTargetProperty(a, new PropertyPath("BorderClipHeight"));
                sb.Children.Add(a);
                sb.Begin();
            }
            else
            {
                SyncDetails.Visibility = System.Windows.Visibility.Visible;
            }
        }

        double topDiff;


        public double BorderClipHeight
        {
            get { return (double)GetValue(BorderClipHeightProperty); }
            set { SetValue(BorderClipHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BorderClipHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BorderClipHeightProperty =
            DependencyProperty.Register("BorderClipHeight", typeof(double), typeof(PhoneControl), new PropertyMetadata(0.0, new PropertyChangedCallback(OnClipHeightChanged)));

        private static void OnClipHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PhoneControl)d).OnClipHeightChanged();
        }

        private void OnClipHeightChanged()
        {
            SyncDetailsBorder.Clip = new RectangleGeometry(new Rect(0, -20, WhiteBorder.ActualWidth, BorderClipHeight));

            if (BorderClipHeight > topDiff)
            {
                SyncDetails.Visibility = System.Windows.Visibility.Visible;
                SyncDetails.Clip = new RectangleGeometry(new Rect(0, 0, WhiteBorder.ActualWidth, BorderClipHeight - topDiff));
            }
        }



        private string FormatSyncList(List<SyncItem> selectedList)
        {
            StringBuilder sb = new StringBuilder();
            foreach (SyncItem item in selectedList)
            {
                sb.AppendLine(item.Name);
            }
            return sb.ToString();
        }

        private PathGeometry CreateDetailsBorderPath(Point startLocation, double flukeWidth, double width, double height, double margin, double cornerRadius)
        {
            double top = startLocation.Y + cornerRadius;
            double bottom = top + height + margin;
            double right = margin + width;

            PathGeometry g = new PathGeometry();
            PathFigure f = new PathFigure();
            f.StartPoint = startLocation;
            f.IsClosed = true;
            g.Figures.Add(f);

            Point current = startLocation;
            bool hasLastCorner = true;

            Point[] corners = new Point[] { 
                 new Point(startLocation.X, top), 
                 new Point(margin, top), 
                 new Point(margin, bottom), 
                 new Point(right, bottom),  
                 new Point(right, top), 
                 new Point(startLocation.X + flukeWidth, top),
                 new Point(startLocation.X + flukeWidth, startLocation.Y) };

            if (right - 2 * cornerRadius < startLocation.X + flukeWidth)
            {
                right = startLocation.X + flukeWidth;
                hasLastCorner = false;
                corners = new Point[] { 
                 new Point(startLocation.X, top), 
                 new Point(margin, top), 
                 new Point(margin, bottom), 
                 new Point(right, bottom),  
                 new Point(right, top) };
                
            }

            Size arcSize = new Size(cornerRadius, cornerRadius);

            for (int i = 0, n = corners.Length; i < n; i++)
            {
                Point next = corners[i];
                if (i + 1 < n)
                {
                    Point future = corners[i + 1];
                    Vector v1 = next - current;
                    v1.Normalize();
                    v1 *= cornerRadius;
                    Vector v2 = future - next;
                    v2.Normalize();
                    v2 *= cornerRadius;

                    double angle = Vector.AngleBetween(v1, v2);
                    SweepDirection direction = angle < 0 ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;

                    Point lineTo = next - v1;
                    Point arcTo = next + v2;

                    f.Segments.Add(new LineSegment(lineTo, true));
                    f.Segments.Add(new ArcSegment(arcTo, arcSize, 90, false, direction, true));
                }
                current = next;

            }
            if (!hasLastCorner)
            {
                f.Segments.Add(new LineSegment(new Point(right, startLocation.Y), true));
            }

            return g;
        }
    }
}
