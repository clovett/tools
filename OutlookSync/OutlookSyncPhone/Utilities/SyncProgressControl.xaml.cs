using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using System.Diagnostics;

namespace OutlookSyncPhone.Utilities
{
    /// <summary>
    /// expecting DataContext of SyncProgress.
    /// </summary>
    public partial class SyncProgressControl : UserControl
    {
        public SyncProgressControl()
        {
            InitializeComponent();
            this.SizeChanged += SyncProgressControl_SizeChanged;
        }

        void SyncProgressControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double s = Math.Min(e.NewSize.Width, e.NewSize.Height);
            LayoutRoot.Width = LayoutRoot.Height = s;
            BackgroundRect.Width = BackgroundRect.Height = s;
        }

        public string SubText
        {
            get { return (string)GetValue(SubTextProperty); }
            set { SetValue(SubTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SubText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SubTextProperty =
            DependencyProperty.Register("SubText", typeof(string), typeof(SyncProgressControl), new PropertyMetadata(null, new PropertyChangedCallback(OnSubTextChanged)));

        private static void OnSubTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SyncProgressControl)d).OnSubTextChanged();
        }

        private void OnSubTextChanged()
        {
            SubTextBlock.Text = "" + SubText;
        }


        public double Count
        {
            get { return (double)GetValue(CountProperty); }
            set { SetValue(CountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Caption.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CountProperty =
            DependencyProperty.Register("Count", typeof(double), typeof(SyncProgressControl), new PropertyMetadata(0.0, new PropertyChangedCallback(OnCountChanged)));

        private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SyncProgressControl)d).OnCountChanged();
        }

        private void OnCountChanged()
        {
            int c = (int)this.Count;
            CaptionTextBlock.Text = c.ToString();
        }


        public Brush TileBackground
        {
            get { return (Brush)GetValue(TileBackgroundProperty); }
            set { SetValue(TileBackgroundProperty, value); }
        }


        // Using a DependencyProperty as the backing store for Caption.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TileBackgroundProperty =
            DependencyProperty.Register("TileBackground", typeof(Brush), typeof(SyncProgressControl), new PropertyMetadata(null, new PropertyChangedCallback(OnTileBackgroundChanged)));

        private static void OnTileBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SyncProgressControl)d).OnTileBackgroundChanged();
        }

        private void OnTileBackgroundChanged()
        {
            BackgroundRect.Fill = TileBackground;
        }

        public Brush TileForeground
        {
            get { return (Brush)GetValue(TileForegroundProperty); }
            set { SetValue(TileForegroundProperty, value); }
        }


        // Using a DependencyProperty as the backing store for Caption.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TileForegroundProperty =
            DependencyProperty.Register("TileForeground", typeof(Brush), typeof(SyncProgressControl), new PropertyMetadata(null, new PropertyChangedCallback(OnTileForegroundChanged)));

        private static void OnTileForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SyncProgressControl)d).OnTileForegroundChanged();
        }

        private void OnTileForegroundChanged()
        {
            CaptionTextBlock.Foreground = TileForeground;
            SubTextBlock.Foreground = TileForeground;
        }

        
    }
}
