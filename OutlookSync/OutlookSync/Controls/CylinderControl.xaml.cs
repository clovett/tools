using OutlookSync.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OutlookSync.Controls
{
    /// <summary>
    /// Interaction logic for CylinderControl.xaml
    /// </summary>
    public partial class CylinderControl : UserControl
    {
        public CylinderControl()
        {
            InitializeComponent();
            this.Loaded += CylinderControl_Loaded;
        }

        void CylinderControl_Loaded(object sender, RoutedEventArgs e)
        {
            OnCylinderFillChanged();
            OnCylinderStrokeChanged();
        }

        public Color CylinderFill
        {
            get { return (Color)GetValue(CylinderFillProperty); }
            set { SetValue(CylinderFillProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CylinderFill.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CylinderFillProperty =
            DependencyProperty.Register("CylinderFill", typeof(Color), typeof(CylinderControl), new PropertyMetadata(Colors.Transparent, OnCylinderFillChanged));

        private static void OnCylinderFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CylinderControl)d).OnCylinderFillChanged();
        }

        private void OnCylinderFillChanged()
        {
            Brush fill = null;
            Brush brush = null;
            if (CylinderFill != Colors.Transparent)
            {
                HlsColor hls = new HlsColor(CylinderFill);
                hls.Lighten(0.25f);
                LinearGradientBrush gradient = new LinearGradientBrush()
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0)
                };
                gradient.GradientStops.Add(new GradientStop() { Color = CylinderFill, Offset = 0 });
                gradient.GradientStops.Add(new GradientStop() { Color = hls.Color, Offset = 0.3 });
                gradient.GradientStops.Add(new GradientStop() { Color = CylinderFill, Offset = 1 });
                brush = gradient;
                fill = new SolidColorBrush(CylinderFill);
            }

            CylinderBody.Fill = brush;
            CylinderBase.Fill = brush;
            CylinderTop.Fill = fill;

            CylinderBody.InvalidateVisual();
            CylinderBase.InvalidateVisual();
            CylinderTop.InvalidateVisual();
        }


        public Color CylinderStroke
        {
            get { return (Color)GetValue(CylinderStrokeProperty); }
            set { SetValue(CylinderStrokeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CylinderStroke.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CylinderStrokeProperty =
            DependencyProperty.Register("CylinderStroke", typeof(Color), typeof(CylinderControl), new PropertyMetadata(Colors.Transparent, OnCylinderStrokeChanged));

        private static void OnCylinderStrokeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CylinderControl)d).OnCylinderStrokeChanged();
        }

        private void OnCylinderStrokeChanged()
        {
            Brush brush = null;
            if (CylinderStroke != Colors.Transparent)
            {
                brush = new SolidColorBrush(CylinderStroke);
            }
            CylinderBody.Stroke = brush;
            CylinderBase.Stroke = brush;
            CylinderTop.Stroke = brush;
            CylinderBody.InvalidateVisual();
            CylinderBase.InvalidateVisual();
            CylinderTop.InvalidateVisual();
        }


        public double CylinderHeight
        {
            get { return (double)GetValue(CylinderHeightProperty); }
            set { SetValue(CylinderHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CylinderHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CylinderHeightProperty =
            DependencyProperty.Register("CylinderHeight", typeof(double), typeof(CylinderControl), new PropertyMetadata(100.0, new PropertyChangedCallback(OnCylinderHeightChanged)));

        private static void OnCylinderHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CylinderControl)d).OnCylinderHeightChanged();
        }

        private void OnCylinderHeightChanged()
        {
            InvalidateMeasure();
            InvalidateArrange();
        }

        public double CylinderWidth
        {
            get { return (double)GetValue(CylinderWidthProperty); }
            set { SetValue(CylinderWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CylinderWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CylinderWidthProperty =
            DependencyProperty.Register("CylinderWidth", typeof(double), typeof(CylinderControl), new PropertyMetadata(200.0, new PropertyChangedCallback(OnCylinderWidthChanged)));

        private static void OnCylinderWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CylinderControl)d).OnCylinderWidthChanged();
        }

        private void OnCylinderWidthChanged()
        {
            InvalidateMeasure();
            InvalidateArrange();
        }

        public double CylinderLidHeight
        {
            get { return (double)GetValue(CylinderLidHeightProperty); }
            set { SetValue(CylinderLidHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CylinderLidHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CylinderLidHeightProperty =
            DependencyProperty.Register("CylinderLidHeight", typeof(double), typeof(CylinderControl), new PropertyMetadata(40.0, new PropertyChangedCallback(OnCylinderLidHeightChanged)));

        private static void OnCylinderLidHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CylinderControl)d).OnCylinderLidHeightChanged();
        }

        private void OnCylinderLidHeightChanged()
        {
            InvalidateMeasure();
            InvalidateArrange();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(CylinderWidth, CylinderHeight + (CylinderLidHeight * 2));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double w = this.CylinderWidth;
            double h = this.CylinderHeight;

            Debug.WriteLine("Cylinder " + this.Name + " height = " + h);
            double stroke = this.CylinderBody.StrokeThickness;
            double twostroke = stroke * 2;
            if (h < twostroke)
            {
                h = twostroke;
            }
            CylinderBody.Width = w;
            CylinderBody.Height = h + stroke + stroke;
            if (CylinderBody.Fill != null)
            {
                RectangleGeometry clip = (RectangleGeometry)CylinderBody.Clip;
                clip.Rect = new Rect(0, stroke, w, h - twostroke);
            }
            CylinderBase.Width = w;
            CylinderBase.Height = (CylinderLidHeight * 2);
            Canvas.SetTop(CylinderBase, h - stroke - 1);
            if (CylinderBase.Fill != null)
            {
                RectangleGeometry clip = (RectangleGeometry)CylinderBase.Clip;
                clip.Rect = new Rect(0, CylinderLidHeight, w, CylinderLidHeight);
            }

            CylinderTop.Width = w;
            CylinderTop.Height = (CylinderLidHeight * 2);

            CylinderTop.InvalidateVisual();
            CylinderBase.InvalidateVisual();
            CylinderBody.InvalidateVisual();
            InvalidateVisual();

            return base.ArrangeOverride(finalSize);
        }

    }
}
