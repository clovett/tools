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
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Diagnostics;

namespace ToonBuilder
{
    /// <summary>
    /// Interaction logic for CurveEditor.xaml
    /// </summary>
    public partial class CurveEditor : UserControl
    {
        RadialMenu menu;
        SegmentEditor current;
        SegmentEditor selected;
        ControlPoint active;
        SegmentEditor previous; // before active control point
        SegmentEditor next; // after active control point
        List<SegmentEditor> segments = new List<SegmentEditor>();
        DispatcherTimer menuFadeTimer;
        Point menuPos;
        TimeSpan MenuFadeTimeout = TimeSpan.FromSeconds(3); // seconds

        public CurveEditor()
        {
            InitializeComponent();

            this.Focusable = true;

            menu = new RadialMenu();
            var line = new RadialMenuItem()
            {
                Name = "Line",
                Icon = BitmapFrame.Create(CreateIconUri("Line.png"), BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.None)
            };
            line.Click += new RoutedEventHandler(OnNewLine);
            var curve = new RadialMenuItem()
            {
                Name = "Curve",
                Icon = BitmapFrame.Create(CreateIconUri("Curve.png"), BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.None)
            };
            curve.Click += new RoutedEventHandler(OnNewCurve);
            var arc = new RadialMenuItem()
            {
                Name = "Arc",
                Icon = BitmapFrame.Create(CreateIconUri("Arc.png"), BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.None)
            };
            arc.Click += new RoutedEventHandler(OnNewArc);
            var delete = new RadialMenuItem()
            {
                Name = "Delete",
                Icon = BitmapFrame.Create(CreateIconUri("Delete16.png"), BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.None)
            };
            delete.Click += new RoutedEventHandler(OnDelete);


            var padlock = new RadialMenuItem()
            {
                Name = "Lock",
                Icon = BitmapFrame.Create(CreateIconUri("PadLock.png"), BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.None)
            };
            padlock.Click += new RoutedEventHandler(OnLockClick);

            menu.Add(line);
            menu.Add(curve);
            menu.Add(arc);
            menu.Add(delete);
            menu.Add(padlock);

            AddHandler(ControlPoint.ActivatedEvent, new RoutedEventHandler(OnControlPointActivated));
            AddHandler(ControlPoint.DeactivatedEvent, new RoutedEventHandler(OnControlPointDeactivated));
        }

        SegmentEditor Next
        {
            get { return this.next; }
            set
            {
                if (this.next != null)
                {
                    this.next.IsSelected = false;
                }
                this.next = value;
                if (this.next != null)
                {
                    this.next.IsSelected = true;
                }
            }
        }

        SegmentEditor Previous
        {
            get { return this.previous; }
            set
            {
                if (this.previous != null)
                {
                    this.previous.IsSelected = false;
                }
                this.previous = value;
                if (this.previous != null)
                {
                    this.previous.IsSelected = true;
                }
            }
        }

        SegmentEditor Selected
        {
            get { return this.selected; }
            set
            {
                if (this.selected != null)
                {
                    this.selected.IsSelected = false;
                }
                this.selected = value;
                if (this.selected != null)
                {
                    this.selected.IsSelected = true;
                }
            }
        }

        void OnControlPointActivated(object sender, RoutedEventArgs e)
        {
            ControlPoint pt = (ControlPoint)e.OriginalSource;
            SegmentEditor next = null;
            SegmentEditor prev = null;
            foreach (SegmentEditor tool in this.segments)
            {
                if (tool.Start == pt)
                {
                    next = tool;
                }
                else if (tool.End == pt)
                {
                    prev = tool;
                }
            }
            if (next != null || prev != null)
            {
                Selected = null;
                this.Next = next;
                this.Previous = prev;
            }

            active = pt;
            if (this.Next != null || this.Previous != null)
            {
                ShowMenu(pt.Position);
            }
        }

        void OnControlPointDeactivated(object sender, RoutedEventArgs e)
        {
            ControlPoint pt = (ControlPoint)e.OriginalSource;
            active = null;
            FadeMenu();
        }

        private void FadeMenu()
        {
            if (EditorCanvas.Children.Contains(menu))
            {
                if (menuFadeTimer == null)
                {
                    menuFadeTimer = new DispatcherTimer(MenuFadeTimeout, DispatcherPriority.Normal, OnHideMenu, this.Dispatcher);
                }
                menuFadeTimer.Stop();
                menuFadeTimer.Start();
                menu.BeginAnimation(RadialMenu.OpacityProperty, new DoubleAnimation(1, 0, new Duration(MenuFadeTimeout)));
                menu.PreviewMouseMove += new MouseEventHandler(RadialMenu_PreviewMouseMove);
            }
        }

        private Uri CreateIconUri(string filename)
        {
            return new Uri("pack://application:,,,/ToonBuilder;component/Icons/" + filename);
        }

        public void Clear()
        {
            EditorCanvas.Children.Clear();
            segments.Clear();
            current = null;
            next = previous = null;
            active = null;
            selected = null;
        }

        void OnDelete(object sender, RoutedEventArgs e)
        {
            if (this.Previous != null)
            {
                RemoveSegment(this.Previous);
                this.Previous = null;
                this.Next = null;
            }
            else if (this.Next != null && !(this.current != null && this.current.IsEditing))
            {
                // this only happens if previous was null, in which case we 
                // are editing the first segment in the curve.
                RemoveSegment(this.Next);
                this.Next = null;
            }
            
            if (current != null)
            {
                RemoveSegment(this.current);
                this.current = null;
            }
            HideMenu();
        }

        void RemoveSegment(SegmentEditor seg)
        {
            if (this.current == seg)
            {
                this.current = null;
            }
            EditorCanvas.Children.Remove(seg);
            seg.OnRemoved();
            int index = segments.IndexOf(seg);
            segments.RemoveAt(index);
            if (index < segments.Count)
            {
                SegmentEditor next = segments[index];
                if (index > 0)
                {
                    SegmentEditor previous = segments[index - 1];
                    next.Join(previous);
                }
                else
                {
                    next.MakeHead();
                }
            }
            else if (index-1 > 0)
            {
                SegmentEditor previous = segments[index-1];
                previous.MakeTail();
            }
        }

        void OnNewArc(object sender, RoutedEventArgs e)
        {
            int index = segments.Count;
            if (next != null)
            {
                index = segments.IndexOf(next);
            }
            AddNewSegment(menuPos, index, new ArcSegmentEditor(menuPos));
        }

        void OnNewCurve(object sender, RoutedEventArgs e)
        {
            int index = segments.Count;
            if (next != null)
            {
                index = segments.IndexOf(next);
            }
            AddNewSegment(menuPos, index, new BezierSegmentEditor(menuPos));
        }

        void OnNewLine(object sender, RoutedEventArgs e)
        {
            int index = segments.Count;
            if (next != null)
            {
                index = segments.IndexOf(next);
            }
            AddNewSegment(menuPos, index, new LineSegmentEditor(menuPos));
        }

        void AddNewSegment(Point pos, int index, SegmentEditor newSegment)
        {
            current = newSegment;
            current.BeginEdit(); // put it into edit mode.

            if (index == -1 || index == segments.Count)
            {
                segments.Add(current);
            }
            else
            {
                segments.Insert(index, current);
            }
            EditorCanvas.Children.Add(newSegment);

            if (next != null)
            {
                next.Join(current);
            }
            if (previous != null)
            {
                current.Join(previous);
            }

            previous = null;
            HideMenu();

        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            this.Focus();
            Point pos = e.GetPosition(EditorCanvas);

            if (current != null)
            {
                if (!current.Add(pos))
                {
                    current.IsSelected = false;
                    int nextIndex = this.segments.IndexOf(current) + 1;
                    var segment = this.current.CreateNew(current.End, pos);
                    AddNewSegment(pos, nextIndex, segment);
                    HideMenu();
                }
            }
            else if (active == null)
            {
                // starting new disjoing path.
                this.Next = null;
                this.Previous = null;
                ShowMenu(pos);
            }
            else 
            {
                HideMenu();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Point pos = e.GetPosition(EditorCanvas);
            if (current != null)
            {
                if (current.IsEditing)
                {
                    current.Move(pos);
                    return;
                }
            }

            SegmentEditor hit = FindSegmentAt(pos);
            if (hit != null)
            {
                this.Selected = hit;
            }
        }

        protected SegmentEditor FindSegmentAt(Point pos) 
        {
            SegmentEditor hit = null;
            int radius = 1;
            while (hit == null && radius < 16)
            {
                VisualTreeHelper.HitTest(EditorCanvas, null, new HitTestResultCallback((result) =>
                {
                    DependencyObject found = result.VisualHit;
                    while (found != null && found != this) 
                    {
                        if (found is ControlPoint)
                        {
                            // let control point win the hit test competition.
                            hit = null;
                            return HitTestResultBehavior.Stop;
                        }
                        hit = found as SegmentEditor;
                        if (hit != null)
                        {
                            return HitTestResultBehavior.Stop;
                        }
                        found = VisualTreeHelper.GetParent(found);
                    }

                    return HitTestResultBehavior.Continue;
                }), new GeometryHitTestParameters(new EllipseGeometry(pos, radius, radius)));

                radius *= 2;
            }
            return hit;
        }


        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Key == Key.Escape)
            {
                // cancel last segment.
                if (current != null)
                {
                    OnDelete(this, e);
                }
                HideMenu();
            }
        }


        void OnHideMenu(object sender, EventArgs e)
        {
            HideMenu();
        }

        public void HideMenu()
        {
            if (EditorCanvas.Children.Contains(menu))
            {
                EditorCanvas.Children.Remove(menu);
            }
            if (menuFadeTimer != null)
            {
                menuFadeTimer.Stop();
                menuFadeTimer = null;
            }
        }

        private void ShowMenu(ControlPoint pt)
        {
            Point pos = new Point(Canvas.GetLeft(pt), Canvas.GetTop(pt));
            ShowMenu(pos);
        }

        private void ShowMenu(Point pos)
        {
            menuPos = pos;
            if (!EditorCanvas.Children.Contains(menu))
            {
                EditorCanvas.Children.Add(menu);
            }
            Canvas.SetLeft(menu, pos.X);
            Canvas.SetTop(menu, pos.Y);
            menu.InvalidateArrange();

            menu.BeginAnimation(RadialMenu.OpacityProperty, null);
            menu.Opacity = 0.9;

            if (menuFadeTimer != null)
            {
                menuFadeTimer.Stop();
                menuFadeTimer.Start();
            }
        }

        void RadialMenu_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            menu.BeginAnimation(RadialMenu.OpacityProperty, null);
            menu.Opacity = 0.9;
            if (menuFadeTimer != null)
            {
                menuFadeTimer.Stop();
                menuFadeTimer.Start();
            }
        }

        void OnLockClick(object sender, RoutedEventArgs e)
        {
            if (Finished != null)
            {
                Finished(this, EventArgs.Empty);
            }
        }

        public event EventHandler Finished;

        public Path PeekFinishedPath()
        {
            if (this.segments.Count == 0)
            {
                return null;
            }

            PathGeometry geometry = new PathGeometry();
            foreach (SegmentEditor seg in this.segments)
            {
                seg.WriteSegment(geometry);
                EditorCanvas.Children.Remove(seg);
            }
            geometry.Figures.FirstOrDefault().IsClosed = true;

            Path snap = new Path();
            //snap.Fill = Brushes.White;            
            snap.Stroke = Brushes.Teal;
            snap.StrokeThickness = 1;
            snap.Data = geometry;
            return snap;
        }

        public Path GetFinishedPath()
        {
            Path result = PeekFinishedPath();

            segments.Clear();
            this.next = null;
            this.previous = null;
            this.current = null;
            this.active = null;

            return result;
        }

    }
}
