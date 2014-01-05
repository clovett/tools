using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OutlookSync.Controls
{
    /// <summary>
    /// Interaction logic for SyncProgressControl.xaml
    /// </summary>
    public partial class SyncProgressControl : UserControl
    {
        public SyncProgressControl()
        {
            InitializeComponent();
            OnCurrentChanged();
        }

        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Maximum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(int), typeof(SyncProgressControl), new PropertyMetadata(0, new PropertyChangedCallback(OnMaximumChanged)));

        private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SyncProgressControl)d).OnMaximumChanged();
        }

        private void OnMaximumChanged()
        {
            ContactCount.Text = Maximum.ToString();

            UpdateFillHeight();
        }

        void UpdateFillHeight()
        {
            double fullHeight = OutlineCylinder.CylinderHeight;
            
            if (Maximum > 0)
            {
                if (Current >= Maximum)
                {
                    // make sure we have no rounding errors.
                    AnimateFillCylinder(fullHeight);
                }
                else
                {
                    AnimateFillCylinder(((double)Current * fullHeight) / (double)Maximum);
                }
            }
            else
            {
                AnimateFillCylinder(0);
            }
        }

        private void AnimateFillCylinder(double newHeight)
        {
            FillCylinder.BeginAnimation(CylinderControl.CylinderHeightProperty, 
                new DoubleAnimation(newHeight, new Duration(TimeSpan.FromSeconds(0.2))));
        }


        public int Current
        {
            get { return (int)GetValue(CurrentProperty); }
            set { SetValue(CurrentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Current.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentProperty =
            DependencyProperty.Register("Current", typeof(int), typeof(SyncProgressControl), new PropertyMetadata(0, new PropertyChangedCallback(OnCurrentChanged)));

        private static void OnCurrentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SyncProgressControl)d).OnCurrentChanged();
        }

        private void OnCurrentChanged()
        {
            UpdateFillHeight();
        }

    }
}
