using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Microsoft.Journal.Common
{

    public class ReorderableListView : ListView
    {
        public ReorderableListView()
        {
            this.SelectionChanged += OnSelectionChanged;
        }



        public Brush SelectedItemBackground
        {
            get { return (Brush)GetValue(SelectedItemBackgroundProperty); }
            set { SetValue(SelectedItemBackgroundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedItemBackground.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedItemBackgroundProperty =
            DependencyProperty.Register("SelectedItemBackground", typeof(Brush), typeof(ReorderableListView), new PropertyMetadata(null));





        public Brush ReorderItemBackground
        {
            get { return (Brush)GetValue(ReorderItemBackgroundProperty); }
            set { SetValue(ReorderItemBackgroundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ReorderItemBackground.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReorderItemBackgroundProperty =
            DependencyProperty.Register("ReorderItemBackground", typeof(Brush), typeof(ReorderableListView), new PropertyMetadata(null, OnReorderItemBackgroundChanged));

        private static void OnReorderItemBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ReorderableListView)d).OnReorderItemBackgroundChanged();
        }

        private void OnReorderItemBackgroundChanged()
        {
            UpdateListViewItemSelectionColor();
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            var newItem = new ReorderableListViewItem(this);
            return newItem;
        }

        public void FlipItems(int index1, int index2, Action moveDataItemsAction)
        {
            object selected = this.SelectedItem; // preserve selection
            var item1 = this.ContainerFromIndex(index1) as ReorderableListViewItem;
            var item2 = this.ContainerFromIndex(index2) as ReorderableListViewItem;
            if (item1 != null && item2 != null)
            {
                item1.BeginFlipAnimation(0, item1.ActualHeight);
                Storyboard s = item2.BeginFlipAnimation(0, -item1.ActualHeight);

                if (s != null)
                {
                    s.Completed += (sender, args) =>
                    {
                        if (moveDataItemsAction != null)
                        {
                            moveDataItemsAction();
                        }
                        var nowait = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, new Windows.UI.Core.DispatchedHandler(() =>
                        {
                            this.SelectedItem = selected;
                        }));
                    };
                }
            }
        }


        public bool IsReorderable
        {
            get { return (bool)GetValue(IsReorderableProperty); }
            set { SetValue(IsReorderableProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsReorderable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsReorderableProperty =
            DependencyProperty.Register("IsReorderable", typeof(bool), typeof(ReorderableListView), new PropertyMetadata(false, new PropertyChangedCallback(OnReorderableChanged)));

        private static void OnReorderableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ReorderableListView)d).OnReorderableChanged();
        }
        private void OnReorderableChanged()
        {
            UpdateListViewItemSelectionColor();
        }

        private void UpdateListViewItemSelectionColor()
        {
            foreach (object item in this.Items)
            {
                ReorderableListViewItem listItem = this.ContainerFromItem(item) as ReorderableListViewItem;
                if (listItem != null)
                {
                    listItem.OnSelectionChanged();
                }
            }
        }


        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (object added in e.AddedItems)
            {
                ReorderableListViewItem item = this.ContainerFromItem(added) as ReorderableListViewItem;
                if (item != null)
                {
                    item.OnSelectionChanged();
                }
            }
            foreach (object removed in e.RemovedItems)
            {
                ReorderableListViewItem item = this.ContainerFromItem(removed) as ReorderableListViewItem;
                if (item != null)
                {
                    item.OnSelectionChanged();
                }
            }
        }



    }

    public class ReorderableListViewItem : ListViewItem
    {
        ReorderableListView list;

        public ReorderableListViewItem(ReorderableListView list)
        {
            this.list = list;
            
        }

        // Using a DependencyProperty as the backing store for FlipPosition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FlipPositionProperty =
            DependencyProperty.Register("FlipPosition", typeof(double), typeof(ReorderableListViewItem), new PropertyMetadata(0.0,
                new PropertyChangedCallback(OnFlipPositionChanged)));

        public double FlipPosition
        {
            get { return (double)GetValue(FlipPositionProperty); }
            set { SetValue(FlipPositionProperty, value); }
        }

        private static void OnFlipPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ReorderableListViewItem)d).OnFlipPositionChanged();
        }

        private void OnFlipPositionChanged()
        {
            TranslateTransform tt = this.RenderTransform as TranslateTransform;
            if (tt == null)
            {
                tt = new TranslateTransform();
                this.RenderTransform = tt;
            }
            tt.Y = FlipPosition;
        }

        public Storyboard BeginFlipAnimation(double from, double to)
        {
            if (storyboard != null)
            {
                storyboard.Stop();
            }
            DoubleAnimation animation = new DoubleAnimation()
            {
                From = from,
                To = to,
                Duration = new Duration(TimeSpan.FromMilliseconds(100)),
                EnableDependentAnimation = true,
                FillBehavior = FillBehavior.HoldEnd,
                EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseIn, Exponent = 0.5 }
            };

            Storyboard s = storyboard = new Storyboard();
            s.Children.Add(animation);

            Storyboard.SetTarget(animation, this);
            Storyboard.SetTargetProperty(animation, "(ReorderableListViewItem.FlipPosition)");

            s.Begin();

            return s;
        }

        Storyboard storyboard;

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            OnSelectionChanged();
        }

        Brush accentBrush;

        internal void OnSelectionChanged()
        {
            if (accentBrush == null)
            {
                accentBrush = list.SelectedItemBackground;
            }
            if (this.IsSelected)
            {
                this.Background = accentBrush;
            }
            else if (list.IsReorderable && list.ReorderItemBackground != null)
            {
                this.Background = list.ReorderItemBackground;
            }
            else
            {
                this.ClearValue(ListViewItem.BackgroundProperty);
            }
        }
    }
}
