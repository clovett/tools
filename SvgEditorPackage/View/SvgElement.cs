using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Controls;
using System.Xml;
using Microsoft.VisualStudio.XmlEditor;
using System.Windows.Threading;

namespace Microsoft.SvgEditorPackage.View
{
    public class SvgElement : FrameworkElement
    {
        XElement element;
        string transform;

        public SvgElement()
        {
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(OnDataContextChanged);
        }

        protected virtual void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.element = e.NewValue as XElement;
            SetStyles();
            InvalidateVisual();
        }

        public static SvgWindowPane GetSvgWindowPane(DependencyObject obj)
        {
            return (SvgWindowPane)obj.GetValue(SvgWindowPaneProperty);
        }

        public static void SetSvgWindowPane(DependencyObject obj, SvgWindowPane value)
        {
            obj.SetValue(SvgWindowPaneProperty, value);
        }

        // Using a DependencyProperty as the backing store for XmlModel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SvgWindowPaneProperty =
            DependencyProperty.RegisterAttached("SvgWindowPane", typeof(SvgWindowPane), typeof(SvgElement), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// save pending changes back to XDocument.
        /// </summary>
        public virtual void Update()
        {
            if (element == null)
            {
                return;
            }

            XElement rect = this.Element;
            if (transform == null)
            {
                XAttribute a = rect.Attribute("transform");
                if (a != null) a.Remove();
            }
            else
            {
                rect.SetAttributeValue("transform", transform);
            }
        }

        public void CommitEdits(string undoCaption)
        {
            using (var scope = GetSvgWindowPane(this).CreateEditingScope(undoCaption))
            {
                Update();
                scope.Complete();
            }
        }

        public XElement Element { get { return element; } }

        // style="fill:red;stroke:black;stroke-width:5;opacity:0.5"
        private void SetStyles()
        {
            if (element == null)
            {
                return;
            }

            // this is a poor man's approximation...
            string style = (string)element.Attribute("style");
            if (!string.IsNullOrEmpty(style))
            {
                string[] parts = style.Split(';');
                foreach (string part in parts)
                {
                    string[] namevalue = part.Split(':');
                    if (namevalue.Length == 2)
                    {
                        string name = namevalue[0].Trim();
                        string value = namevalue[1].Trim();

                        switch (name)
                        {
                            case "fill":
                                SetValue(Shape.FillProperty, value.ParseBrush());
                                break;
                            case "stroke":
                                SetValue(Shape.StrokeProperty, value.ParseBrush());
                                break;
                            case "stroke-width":
                                SetValue(Shape.StrokeThicknessProperty, value.ParseLength());
                                break;
                            case "opacity":
                                this.Opacity = Math.Max(0, Math.Min(1, double.Parse(value)));
                                break;
                            case "font-family":
                                SetValue(TextBlock.FontFamilyProperty, value.ParseFontFamily());
                                break;
                            case "font-weight":
                                break;
                            case "font-style":
                                break;
                            case "font-size":
                                SetValue(TextBlock.FontSizeProperty, value.ParseLength());
                                break;
                        }
                    }
                }
            }

            // now parse explicit properties
            foreach (XAttribute a in element.Attributes())
            {
                switch (a.Name.LocalName)
                {
                    case "fill":
                        SetValue(Shape.FillProperty, a.Value.ParseBrush());
                        break;
                    case "stroke":
                        SetValue(Shape.StrokeProperty, a.Value.ParseBrush());
                        break;
                    case "stroke-width":
                        SetValue(Shape.StrokeThicknessProperty, a.Value.ParseLength());
                        break;
                    case "font-family":
                        this.SetValue(TextBlock.FontFamilyProperty, a.Value.ParseFontFamily());
                        break;
                    case "font-size":
                        this.SetValue(TextBlock.FontSizeProperty, a.Value.ParseLength());
                        break;
                    case "transform":
                        transform = a.Value;
                        this.RenderTransform = a.Value.ParseTransform();
                        break;
                }
            }
        }

        public Rect Bounds
        {
            get
            {
                Rect bounds = VisualTreeHelper.GetDescendantBounds(this);
                double left = Canvas.GetLeft(this);
                if (!double.IsNaN(left)) bounds.X += left;
                double top = Canvas.GetTop(this);
                if (!double.IsNaN(top)) bounds.Y += top;
                return bounds;
            }
        }

        /// <summary>
        /// The default implementation applies a transform
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public virtual Rect Resize(Rect bounds)
        {
            Rect geometryBounds = this.Bounds;

            // Ok, now figure out what transform we have to apply to make the geometry bounds equal to the given bounds.

            double tx = bounds.Left - geometryBounds.Left;
            double ty = bounds.Top - geometryBounds.Top;
            double sx = bounds.Width / geometryBounds.Width;
            double sy = bounds.Height / geometryBounds.Height;

            this.transform = null;
            if (tx != 0 || ty != 0)
            {
                transform = "translate(" + XmlConvert.ToString(tx) + "," + XmlConvert.ToString(ty) + ")";
            }
            if (sx != 1 || sy != 1)
            {
                if (transform != null)
                {
                    transform += " ";
                }
                transform += "scale(" + XmlConvert.ToString(sx) + "," + XmlConvert.ToString(sy) + ")";
            }
            if (transform != null)
            {
                this.RenderTransform = transform.ParseTransform();
            }
            else
            {
                this.RenderTransform = null;
            }
            return bounds;
        }
    }
}
