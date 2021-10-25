using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace DependencyViewer {
    public class SubgraphShape : NodeShape {
        GraphCanvas diagram;
        Canvas content;
        GraphCanvas canvas = new GraphCanvas();
        IGraph subgraph;
        INode node;
        TextBlock nodeLabel;
        Border countBorder;
        TextBlock countLabel;
        const int ContentMargin = 10;

        public SubgraphShape() {
        }

        internal SubgraphShape(GraphCanvas owner, GraphNode model, INode node, TextBlock nodeLabel)
            : base(owner, model, node, nodeLabel) {

            this.diagram = owner;
            this.subgraph = canvas.InnerGraph;
            subgraph.UserData = this;
            this.node = node;
            canvas.ShowEdgeLabels = owner.ShowEdgeLabels;

            content = new System.Windows.Controls.Canvas();
            // Override the content to be this new container instead of the label.            
            this.Content = content;

            countLabel = new TextBlock();
            countLabel.FontSize = 9;            
            countLabel.Padding = new Thickness(3,0,3,0);
            countLabel.TextAlignment = TextAlignment.Right;
            countLabel.VerticalAlignment = VerticalAlignment.Center;

            countBorder = new Border();
            countBorder.CornerRadius = new CornerRadius(2);
            countBorder.HorizontalAlignment = HorizontalAlignment.Right;
            countBorder.Background = new LinearGradientBrush(Color.FromRgb(0xee, 0xee, 0xee), Color.FromRgb(0xaa, 0xaa, 0xaa), 90);
            countBorder.Child = countLabel;
                       
            content.Children.Add(countBorder);
            content.Children.Add(canvas);

            // todo: make this stylable.
            if (nodeLabel != null)
            {
                this.nodeLabel = nodeLabel;

                nodeLabel.FontWeight = FontWeights.ExtraBold;

                nodeLabel.Foreground = new SolidColorBrush(Colors.Green);
                nodeLabel.VerticalAlignment = VerticalAlignment.Top;
                nodeLabel.HorizontalAlignment = HorizontalAlignment.Left;
                nodeLabel.TextAlignment = TextAlignment.Left;
                nodeLabel.Padding = new Thickness(0);
                nodeLabel.Opacity = 0.40; // translucent
                nodeLabel.IsHitTestVisible = false;
                nodeLabel.Focusable = false;
                content.Children.Add(nodeLabel);
            }                       
        }

        double _emSize;
        Typeface _typeface;

        public Size MeasureText(string s)
        {

            if (_typeface == null) 
            {
                FontFamily fontFamily = (FontFamily)this.GetValue(TextBlock.FontFamilyProperty);
                FontStyle fontStyle = (FontStyle)this.GetValue(TextBlock.FontStyleProperty);
                FontWeight fontWeight = (FontWeight)this.GetValue(TextBlock.FontWeightProperty);
                FontStretch fontStretch = (FontStretch)this.GetValue(TextBlock.FontStretchProperty) ;
                _typeface = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
                _emSize = (double)this.GetValue(TextBlock.FontSizeProperty);
            }
            FormattedText ft = new FormattedText(s, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, _typeface, _emSize, Brushes.Navy);
            return new Size(ft.Width, ft.Height);
        }

        public int NodeCount
        {
            get
            {
                return canvas.Nodes.Count;
            }
        }

        internal IGraph SubGraph { get { return this.subgraph; } set { this.subgraph = value; } }

        public GraphCanvas Canvas { get { return canvas; } }

        protected override Size MeasureOverride(Size constraint) {
            countLabel.Text = (this.canvas != null) ? this.canvas.Nodes.Count.ToString() : "";
            // Save the desired size so we can use it on background thread in GetNodeBoundaryCurve
            Size s = base.MeasureOverride(constraint);

            Rect bbox = canvas.InnerGraph.BoundingBox;
            if (bbox.Width != 0 && !double.IsNaN(bbox.Width))
            {
                s = new Size(bbox.Width + ContentMargin, bbox.Height + ContentMargin);
            }
            
            this.s = s;
            return s;
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            System.Windows.Controls.Canvas.SetLeft(countBorder, arrangeBounds.Width - countBorder.ActualWidth - ContentMargin - 3);
            System.Windows.Controls.Canvas.SetTop(countBorder, 3 - ContentMargin );
            double cx = ContentMargin - (arrangeBounds.Width / 2);
            double cy = ContentMargin - (arrangeBounds.Height / 2);
            content.RenderTransform = new TranslateTransform(cx, cy);
            Size textSize = MeasureText(nodeLabel.Text);
            double ratioH = s.Height / textSize.Height;
            double ratioW = s.Width / textSize.Width;
            TransformGroup g = new TransformGroup();
            double ratioToUse = Math.Min(ratioH, ratioW) * 0.8;
            cx = ((arrangeBounds.Width  - (ratioToUse * textSize.Width)) / 2);
            cy = ((arrangeBounds.Height - (ratioToUse * textSize.Height)) / 2);
            g.Children.Add(new ScaleTransform(ratioToUse, ratioToUse));
            g.Children.Add(new TranslateTransform(cx - ContentMargin, cy - ContentMargin)); // center it.
            nodeLabel.RenderTransform = g;
            this.Width = arrangeBounds.Width;
            this.Height = arrangeBounds.Height;
            return base.ArrangeOverride(arrangeBounds);
        }

    }
}