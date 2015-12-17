using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Media3D;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace DateSpinnerSample
{
    public sealed partial class DateSpinner : UserControl
    {
        private double radius = 1000;
        private double midPoint;
        private double visibleWidth;
        private double visibleHeight;
        private double visibleAngle;
        private int cacheCount; // how many buttons to create either side of center button
        private Button selection; // what button is currently in the center?

        const double RadiansToDegrees = 180 / Math.PI;

        public DateSpinner()
        {
            this.InitializeComponent();

            this.SizeChanged += DateSpinner_SizeChanged;

            this.ManipulationMode = ManipulationModes.All;

            this.SelectedDate = DateTime.Today;
        }

        public DateTime SelectedDate
        {
            get { return (DateTime)GetValue(SelectedDateProperty); }
            set { SetValue(SelectedDateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedDate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register("SelectedDate", typeof(DateTime), typeof(DateSpinner), new PropertyMetadata(DateTime.Today, OnDateChanged));


        private static void OnDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DateSpinner ds = (DateSpinner)d;
            ds.OnDateChanged();
        }

        private void OnDateChanged()
        {
            DateTime date = this.SelectedDate;

            Button found = null;

            // find the button matching selected date and rotate it into view.
            foreach (Button child in SpinnerPanel.Children)
            {
                ButtonInfo info = (ButtonInfo)child.Tag;
                if (info.Date.Date == date.Date)
                {
                    found = child;
                    break;
                }
            }
            if (found != null)
            {
                if (found != selection)
                {
                    SelectButton(found);
                }
            }
            else
            {
                // repopulate buttons around this selected date.
                Populate(visibleWidth, true, true);
            }
        }

        /// <summary>
        /// Angle of the wheel, note: we allow angle to increase and decrease infinitely in order to reach all possible dates.
        /// This property can be animated.
        /// </summary>
        public double Angle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Angle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AngleProperty =
            DependencyProperty.Register("Angle", typeof(double), typeof(DateSpinner), new PropertyMetadata(0.0, OnAngleChanged));

        private static void OnAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DateSpinner ds = (DateSpinner)d;
            ds.RotateTo(ds.Angle, true);
        }

        double startAngle;

        protected override void OnManipulationStarted(ManipulationStartedRoutedEventArgs e)
        {
            startAngle = this.Angle;
            base.OnManipulationStarted(e);
        }

        protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
        {
            Point delta = e.Cumulative.Translation;
            double dx = -delta.X;

            // how much of the circle is this?
            double angleDelta = Math.Asin(Math.Max(-1, Math.Min(1, dx / (2 * radius))));
            RotateTo(startAngle + angleDelta, true);

            e.Handled = true;
        }

        protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
        {
            Point delta = e.Cumulative.Translation;
            double dx = -delta.X;
            double angleDelta = Math.Asin(dx / (2 * radius));

            this.Angle = startAngle + angleDelta;

            e.Handled = true;

            RemoveHiddenButtons();
        }

        private void RemoveHiddenButtons()
        {
            List<Button> hidden = new List<Button>();
            foreach (Button child in SpinnerPanel.Children)
            {
                if (child.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
                {
                    hidden.Add(child);
                }
            }

            foreach (Button b in hidden)
            {
                SpinnerPanel.Children.Remove(b);
            }
        }

        void DateSpinner_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Size s = e.NewSize;
            visibleHeight = s.Height;

            double width = s.Width;
            bool changed = (this.visibleWidth != width);
            radius = width * 2;
            visibleAngle = 2 * Math.Atan(width / radius);
            this.visibleWidth = width;
            if (changed || SpinnerPanel.Children.Count == 0)
            {
                Populate(width, true, true);
            }
        }

        private void RotateTo(double newAngle, bool extend = false)
        {
            bool moreToRight = true;
            bool moreToLeft = true;

            foreach (Button b in SpinnerPanel.Children)
            {
                ButtonInfo info = (ButtonInfo)b.Tag;
                double originalAngle = info.Angle;

                double angle = originalAngle + newAngle;

                PlaneProjection pp = (PlaneProjection)b.Projection;
                if (pp == null)
                {
                    b.Projection = RotateAboutYAxis(angle);
                }
                else
                {
                    pp.RotationY = angle * RadiansToDegrees;
                }

                if (angle > visibleAngle)
                {
                    b.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    moreToLeft = false;
                }
                else if (angle < -visibleAngle)
                {
                    b.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    moreToRight = false;
                }
                else
                {
                    b.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
            }

            if (extend && (moreToRight || moreToLeft))
            {
                RealizeButtons(moreToLeft, moreToRight);
            }
            else if (extend)
            {
                addMoreToLeft = addMoreToRight = false;
            }
        }

        int buttonCheckId;
        int buttonCheckIdQueued;
        bool addMoreToLeft;
        bool addMoreToRight;

        bool populateMoreToLeft;
        bool populateMoreToRight;
        DispatcherTimer buttonCheckTimer;

        private void RealizeButtons(bool moreToLeft, bool moreToRight)
        {
            addMoreToLeft |= moreToLeft;
            addMoreToRight |= moreToRight;
            populateMoreToLeft |= moreToLeft;
            populateMoreToRight |= moreToRight;
            buttonCheckId++;
            if (buttonCheckTimer == null)
            {
                buttonCheckTimer = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromMilliseconds(10)
                };
                buttonCheckTimer.Tick += buttonCheckTimer_Tick;
            }

            buttonCheckTimer.Stop();
            buttonCheckTimer.Start();
            buttonCheckIdQueued = buttonCheckId;
        }

        void buttonCheckTimer_Tick(object sender, object e)
        {
            Populate(visibleWidth, populateMoreToLeft, populateMoreToRight);

            populateMoreToLeft = false;
            populateMoreToRight = false;

            Layout();

            buttonCheckTimer.Stop();
            buttonCheckTimer.Tick -= buttonCheckTimer_Tick;
            buttonCheckTimer = null;

            if (buttonCheckIdQueued != buttonCheckId)
            {
                RealizeButtons(addMoreToLeft, addMoreToRight);
            }
            else
            {
                // we have caught up.
                buttonCheckId = buttonCheckIdQueued = 0;
                addMoreToLeft = addMoreToRight = false;
            }

        }

        private void SelectButton(Button button)
        {
            if (this.selection != null)
            {
                Brush b = this.selection.FindResource<Brush>("FocusVisualBlackStrokeThemeBrush");
                this.selection.BorderBrush = b;
            }

            this.selection = button;

            if (this.selection != null)
            {
                SelectedDate = ((ButtonInfo)button.Tag).Date;

                Brush b = this.selection.FindResource<Brush>("SelectedVisualBlackStrokeThemeBrush");
                this.selection.BorderBrush = b;

                ScrollToCenter(button);
            }
        }

        /// <summary>
        /// This method ensures we have enough buttons either side of the center focus
        /// to fill the given control width.
        /// </summary>
        /// <param name="width">The width of this control</param>
        private void Populate(double width, bool addMoreToLeft, bool addMoreToRight)
        {
            if (width == 0)
            {
                // not ready yet.
                return;
            }

            DateTime today = DateTime.Today;
            DateTime focusDate = today;
            bool added = false;

            int count = cacheCount;
            if (count == 0)
            {
                // no estimate yet, so assume each button is about 50 pixels wide
                count = cacheCount = (int)(width / 50);
                if (count == 0) count = 1;
            }

            ButtonInfo leftMost = null;
            ButtonInfo rightMost = null;

            if (selection != null)
            {
                ButtonInfo selectionInfo = (ButtonInfo)selection.Tag;
                leftMost = selectionInfo.LeftMost;
                rightMost = selectionInfo.RightMost;
            }
            else
            {
                addMoreToLeft = true;
                addMoreToRight = true;
                selection = CreateButton(DateTime.Today);
                SelectButton(selection);
                leftMost = rightMost = (ButtonInfo)selection.Tag;
            }


            if (addMoreToRight)
            {
                DateTime d = rightMost.Date.AddDays(1);

                // ensure we have 'count' buttons to the right of the current central focus button.
                for (int i = 0; i < count; i++)
                {
                    added = true;
                    Button b = CreateButton(d);
                    ButtonInfo info = (ButtonInfo)b.Tag;
                    // link in the new button
                    rightMost.Next = b;
                    info.Previous = rightMost.Owner;
                    rightMost = info;
                    d = d.AddDays(1);
                }
            }

            if (addMoreToLeft)
            {
                DateTime d = leftMost.Date.AddDays(-1);

                // ensure we have 'count' buttons to the left of the current central focus button.
                for (int i = 0; i < count; i++)
                {
                    added = true;
                    Button b = CreateButton(d);
                    ButtonInfo info = (ButtonInfo)b.Tag;
                    // link in the new button
                    leftMost.Previous = b;
                    info.Next = leftMost.Owner;
                    leftMost = info;
                    d = d.AddDays(-1);
                }
            }

            if (added)
            {
                InvalidateArrange();
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            visibleWidth = finalSize.Width;

            Layout();

            SpinnerPanel.Clip = new RectangleGeometry()
            {
                Rect = new Rect(0, 0, finalSize.Width, finalSize.Height)
            };

            // arrange the canvas.
            return base.ArrangeOverride(finalSize);
        }

        void Layout()
        {
            if (selection != null)
            {

                double buttonAngle = 0;
                double leftAngle = 0;

                DateTime today = DateTime.Today;

                midPoint = visibleWidth / 2;

                // project the buttons onto a drum of size "radius".

                for (Button b = selection; b != null; b = ((ButtonInfo)b.Tag).Next)
                {
                    ButtonInfo info = (ButtonInfo)b.Tag;
                    info.Angle = -buttonAngle;

                    b.Projection = RotateAboutYAxis(-buttonAngle);

                    // approximate how much of the wheel surface this button covers in angular distance.
                    double buttonWidth = b.DesiredSize.Width;
                    double angleSpan = 2 * Math.Asin(buttonWidth / (2 * radius));
                    info.AngleSpan = angleSpan;

                    if (b == selection)
                    {
                        //midPoint += buttonWidth;
                        leftAngle = angleSpan / 2;
                        buttonAngle = -angleSpan / 2;
                        info.Angle = -buttonAngle;
                        b.Projection = RotateAboutYAxis(-buttonAngle);
                    }

                    buttonAngle += angleSpan;
                }

                buttonAngle = leftAngle;

                for (Button b = ((ButtonInfo)selection.Tag).Previous; b != null; b = ((ButtonInfo)b.Tag).Previous)
                {
                    ButtonInfo info = (ButtonInfo)b.Tag;

                    // approximate how much of the wheel surface this button covers in angular distance.
                    double buttonWidth = b.DesiredSize.Width;
                    double angleSpan = 2 * Math.Asin(buttonWidth / (2 * radius));
                    info.AngleSpan = angleSpan;
                    buttonAngle += angleSpan;
                    b.Projection = RotateAboutYAxis(-buttonAngle);
                    info.Angle = buttonAngle;
                }
            }

            // clip buttons behind
            RotateTo(this.Angle);
        }

        private Projection RotateAboutYAxis(double angle)
        {
            Debug.Assert(midPoint != 0);
            if (midPoint == 0)
            {
                midPoint = 1;
            }

            PlaneProjection pp = new PlaneProjection()
            {
                RotationY = angle * RadiansToDegrees,
                LocalOffsetX = midPoint,
                CenterOfRotationX = radius / midPoint,
                CenterOfRotationY = 0,
                CenterOfRotationZ = -radius
            };
            return pp;
        }

        private Button CreateButton(DateTime date)
        {
            Button b = new Button()
            {
                VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch
            };

            b.Click += OnButtonClick;

            TextBlock content = new TextBlock()
            {
                TextAlignment = TextAlignment.Center
            };

            content.Inlines.Add(new Run() { Text = date.ToString("ddd") });
            content.Inlines.Add(new LineBreak());
            content.Inlines.Add(new Run() { Text = date.ToString("dd") });
            content.Inlines.Add(new LineBreak());
            content.Inlines.Add(new Run() { Text = date.ToString("MMM") });

            b.Content = content;

            b.Tag = new ButtonInfo()
            {
                Owner = b,
                Date = date
            };

            if (date == DateTime.Today)
            {
                b.Background = new SolidColorBrush(Colors.Teal);
            }

            SpinnerPanel.Children.Add(b);
            return b;

        }

        void OnButtonClick(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            SelectButton(b);
        }

        private void ScrollToCenter(Button b)
        {
            ButtonInfo info = (ButtonInfo)b.Tag;
            AnimateAngle(-info.Angle + info.AngleSpan / 2, new Duration(TimeSpan.FromSeconds(.3)));
        }


        public void AnimateAngle(double newAngle, Duration duration)
        {
            DoubleAnimation animation = new DoubleAnimation()
            {
                To = newAngle,
                Duration = duration,
                EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseInOut }
            };

            // for some weird reason, by default WinRT does NOT run this animation because it thinks
            // it's targeting a property that will slow down the UI too much, so we override that here.
            animation.EnableDependentAnimation = true;

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, this);
            Storyboard.SetTargetProperty(animation, "Angle");
            storyboard.Begin();
        }

        private class ButtonInfo
        {
            public Button Owner;
            public DateTime Date;
            public double Angle;
            public double AngleSpan;
            public Button Next;
            public Button Previous;

            public ButtonInfo NextInfo
            {
                get
                {
                    if (Next != null)
                    {
                        return (ButtonInfo)Next.Tag;
                    }
                    return null;
                }
            }
            public ButtonInfo PreviousInfo
            {
                get
                {
                    if (Previous != null)
                    {
                        return (ButtonInfo)Previous.Tag;
                    }
                    return null;
                }
            }

            public ButtonInfo LeftMost
            {
                get
                {
                    for (ButtonInfo b = this; b != null; b = b.PreviousInfo)
                    {
                        if (b.Previous == null)
                        {
                            return b;
                        }
                    }
                    return null;
                }
            }

            public ButtonInfo RightMost
            {
                get
                {
                    for (ButtonInfo b = this; b != null; b = b.NextInfo)
                    {
                        if (b.NextInfo == null)
                        {
                            return b;
                        }
                    }
                    return null;
                }
            }
        }


    }
}
