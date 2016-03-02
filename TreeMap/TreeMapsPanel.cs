using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Collections;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Input;

namespace TreeMaps.Controls
{
    public class TreeNodeSelectedEventArgs : EventArgs
    {
        public TreeNodeData Selection { get; set; }
    }

    public class TreeMapsPanel : Canvas
    {
        #region fields

        private Rect _emptyArea;
        private double _weightSum = 0;
        private TreeNodeData _root;
        #endregion

        public TreeMapsPanel()
        {
        }

        protected enum RowOrientation
        {
            Horizontal,
            Vertical
        }

        public event EventHandler<TreeNodeSelectedEventArgs> SelectionChanged;

        private void OnSelectionChanged()
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(this, new TreeNodeSelectedEventArgs() { Selection = _root });
            }
        }


        #region properties

        protected TreeNodeData Root
        {
            get { return _root; }
            set
            {
                if (_root != value)
                {
                    _root = value;
                    OnSelectionChanged();
                    BindElements();
                }
            }
        }

        protected Rect EmptyArea
        {
            get { return _emptyArea; }
            set { _emptyArea = value; }
        }

        #endregion

        #region protected methods

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            if (this.Root != null && this.Root.Children != null)
            {
                foreach (TreeNodeData child in this.Root.Children)
                {
                    UIElement e = child.UiElement;
                    if (e != null)
                    {
                        Size s = child.ComputedSize;
                        if (double.IsNaN(s.Width) || double.IsInfinity(s.Width))
                        {
                            s.Width = 0;
                        }
                        if (double.IsNaN(s.Height) || double.IsInfinity(s.Height))
                        {
                            s.Height = 0;
                        }
                        e.Arrange(new Rect(child.ComputedLocation, s));
                    }
                }
            }
            return arrangeSize;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            this.EmptyArea = new Rect(0, 0, constraint.Width, constraint.Height);

            double area = this.EmptyArea.Width * this.EmptyArea.Height;

            if (this.Root != null && this.Root.Children != null)
            {
                foreach (TreeNodeData item in this.Root.Children)
                    item.RealArea = area * item.Weight / _weightSum;
            }

            this.ComputeBounds();

            if (this.Root != null && this.Root.Children != null)
            {
                foreach (TreeNodeData child in this.Root.Children)
                {
                    UIElement e = child.UiElement;
                    if (e != null)
                    {
                        if (this.IsValidSize(child.ComputedSize))
                        {
                            e.Measure(child.ComputedSize);
                        }
                        else {
                            e.Measure(new Size(0, 0));
                        }
                    }
                }
            }

            return constraint;
        }

        protected virtual void ComputeBounds()
        {
            if (this.Root != null && this.Root.Children != null)
            {
                this.ComputeTreeMaps(this.Root.Children);
            }
        }

        protected double GetShortestSide()
        {
            return Math.Min(this.EmptyArea.Width, this.EmptyArea.Height);
        }

        protected RowOrientation GetOrientation()
        {
            return (this.EmptyArea.Width > this.EmptyArea.Height ? RowOrientation.Horizontal : RowOrientation.Vertical);
        }

        protected virtual Rect GetRectangle(RowOrientation orientation, TreeNodeData item, double x, double y, double width, double height)
        {
            if (orientation == RowOrientation.Horizontal)
                return new Rect(x, y, item.RealArea / height, height);
            else
                return new Rect(x, y, width, item.RealArea / width);
        }

        protected virtual void ComputeNextPosition(RowOrientation orientation, ref double xPos, ref double yPos, double width, double height)
        {
            if (orientation == RowOrientation.Horizontal)
                xPos += width;
            else
                yPos += height;
        }

        protected void ComputeTreeMaps(IEnumerable<TreeNodeData> items)
        {
            RowOrientation orientation = this.GetOrientation();

            double areaSum = 0;

            if (items != null)
            {
                foreach (TreeNodeData item in items)
                    areaSum += item.RealArea;
            }

            Rect currentRow;
            if (orientation == RowOrientation.Horizontal)
            {
                currentRow = new Rect(_emptyArea.X, _emptyArea.Y, areaSum / _emptyArea.Height, _emptyArea.Height);
                _emptyArea = new Rect(_emptyArea.X + currentRow.Width, _emptyArea.Y, Math.Max(0, _emptyArea.Width - currentRow.Width), _emptyArea.Height);
            }
            else
            {
                currentRow = new Rect(_emptyArea.X, _emptyArea.Y, _emptyArea.Width, areaSum / _emptyArea.Width);
                _emptyArea = new Rect(_emptyArea.X, _emptyArea.Y + currentRow.Height, _emptyArea.Width, Math.Max(0, _emptyArea.Height - currentRow.Height));
            }

            double x = currentRow.X;
            double y = currentRow.Y;

            if (items != null)
            {
                foreach (TreeNodeData item in items)
                {
                    Rect rect = this.GetRectangle(orientation, item, x, y, currentRow.Width, currentRow.Height);
                    item.AspectRatio = rect.Width / rect.Height;
                    item.ComputedSize = rect.Size;
                    item.ComputedLocation = rect.Location;

                    this.ComputeNextPosition(orientation, ref x, ref y, rect.Width, rect.Height);
                }
            }
        }

        #endregion

        #region private methods

        private bool IsValidSize(Size size)
        {
            return (!size.IsEmpty && size.Width > 0 && size.Width != double.NaN && size.Height > 0 && size.Height != double.NaN);
        }

        private bool IsValidItem(TreeNodeData item)
        {
            return (item != null && item.Weight != double.NaN && Math.Round(item.Weight, 0) != 0);
        }

        public TreeNodeData TreeData
        {
            get { return (TreeNodeData)GetValue(TreeDataProperty); }
            set { SetValue(TreeDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TreeData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TreeDataProperty =
            DependencyProperty.Register("TreeData", typeof(TreeNodeData), typeof(TreeMapsPanel), new PropertyMetadata(null, new PropertyChangedCallback(OnTreeDataChanged)));

        private static void OnTreeDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TreeMapsPanel)d).OnTreeDataChanged();
        }

        private void OnTreeDataChanged()
        {
            this.Root = this.TreeData;
        }

        private void BindElements()
        {
            this.Children.Clear();
            _weightSum = 0;
            if (_root != null && _root.Children != null)
            {
                foreach (var item in _root.Children)
                {
                    _weightSum += item.Weight;
                    Grid b = new Grid();
                    string label = item.Label + " (" + item.Size + ")";
                    var text = new TextBlock() { Text = label, Margin = new Thickness(2), VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
                    b.Children.Add(text);
                    b.Background = new SolidColorBrush(item.Color);
                    item.UiElement = b;
                    b.DataContext = item;
                    this.Children.Add(b);
                    b.MouseLeftButtonDown += OnItemMouseLeftMouseDown;
                }
            }
            InvalidateMeasure();
            InvalidateArrange();
        }

        private void OnItemMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
        }

        private void OnItemMouseLeftMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Focus();

            Grid b = (Grid)sender;
            TreeNodeData data = (TreeNodeData)b.DataContext;

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                // go back up a level.
                if (data.Parent != null && data.Parent.Parent != null && data != this.TreeData && data.Parent != this.TreeData)
                {
                    this.Root = data.Parent.Parent;
                }
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                // add child treemap.
                if (b.Children.Count == 1 &&data.Children != null && data.Children.Count > 0)
                {
                    b.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    b.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
                    SquarifiedTreeMapsPanel subtree = new SquarifiedTreeMapsPanel();
                    subtree.TreeData = data;
                    Grid.SetRow(subtree, 1);
                    b.Children.Add(subtree);
                }

                // walk up parent chain increasing font size of each header element.
                SetHeaderFontSizes(b, 1);
            }
            else
            {
                // drill in to this item.
                if (data.Children != null && data.Children.Count > 0)
                {
                    this.Root = data;
                }
            }
            e.Handled = true;            
        }

        private void SetHeaderFontSizes(DependencyObject d, int depth)
        {
            Grid g = d as Grid;
            if (g != null)
            {
                if (g.Children.Count > 0)
                {
                    TextBlock header = g.Children[0] as TextBlock;
                    if (header != null)
                    {
                        header.FontSize = Math.Max(header.FontSize, 11 + (2 * depth));
                        depth++;
                    }
                }
            }
            d = VisualTreeHelper.GetParent(d);
            if (d != null)
            {
                SetHeaderFontSizes(d, depth);
            }
        }
        #endregion

    }
}
