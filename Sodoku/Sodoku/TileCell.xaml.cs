using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Sodoku
{
    public enum TileCellState
    {
        None,
        Highlighting,
    }

    public enum TileCellEditState
    {
        None,
        Editing,
        ErrorSource,
        ErrorScope
    }

    public sealed partial class TileCell : UserControl
    {
        bool editing;

        public TileCell()
        {
            this.InitializeComponent();
            Editor.LostFocus += OnEditorLostFocus;
        }

        void OnEditorLostFocus(object sender, RoutedEventArgs e)
        {
            if (editing)
            {
                Commit();
            }
        }

        public void Commit()
        {
            if (Editor.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                Editor.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                Text.Visibility = Windows.UI.Xaml.Visibility.Visible;
                CellEditState = TileCellEditState.None;
                editing = false;
                if (Committed != null)
                {
                    Committed(this, EventArgs.Empty);
                }
            }
        }

        private void OnBorderClicked(object sender, PointerRoutedEventArgs e)
        {
            BeginEdit();
        }
        

        internal void BeginEdit()
        {
            if (Editor.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
            {
                Editor.Visibility = Windows.UI.Xaml.Visibility.Visible;
                Text.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                var nowait = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
                {
                    Editor.SelectAll();
                    Editor.Focus(Windows.UI.Xaml.FocusState.Programmatic);
                    editing = true;
                    CellEditState = TileCellEditState.Editing;
                }));

                if (Editing != null)
                {
                    Editing(this, EventArgs.Empty);
                }
            }
        }



        public TileCellState CellState
        {
            get { return (TileCellState)GetValue(CellStateProperty); }
            set { SetValue(CellStateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CellState.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CellStateProperty =
            DependencyProperty.Register("CellState", typeof(TileCellState), typeof(TileCell), new PropertyMetadata(TileCellState.None, new PropertyChangedCallback(OnCellStateChanged)));

        private static void OnCellStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TileCell)d).OnCellStateChanged();
        }

        private Brush HighlightBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x2C, 0x26, 0x4F));

        void OnCellStateChanged()
        {
            UpdateCellBorder();
            if (this.Name == "G4.T6")
            {
                Debug.WriteLine("{0} CellStateChanged to {1}", this.Name, this.CellState);
            }
        }

        private void UpdateCellBorder()
        {
            Brush brush = null;
            switch (CellEditState)
            {
                case TileCellEditState.None:
                    switch (CellState)
                    {
                        case TileCellState.None:
                            brush = new SolidColorBrush() { Color = Colors.Transparent };
                            break;
                        case TileCellState.Highlighting:
                            brush = HighlightBrush;
                            break;
                    }
                    break;
                case TileCellEditState.Editing:
                    brush = Editor.Background;
                    break;
                case TileCellEditState.ErrorSource:
                    brush = new SolidColorBrush(Colors.Red);
                    break;
                case TileCellEditState.ErrorScope:
                    brush = new SolidColorBrush(Colors.Maroon);
                    break;
            }
            CellBorder.Background = brush;
        }


        public TileCellEditState CellEditState
        {
            get { return (TileCellEditState)GetValue(CellErrorStateProperty); }
            set { SetValue(CellErrorStateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CellState.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CellErrorStateProperty =
            DependencyProperty.Register("CellEditState", typeof(TileCellEditState), typeof(TileCell), new PropertyMetadata(TileCellEditState.None, new PropertyChangedCallback(OnCellErrorStateChanged)));

        private static void OnCellErrorStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TileCell)d).OnCellErrorStateChanged();
        }

        void OnCellErrorStateChanged()
        {
            UpdateCellBorder();
        }

        public TileGrid ParentTile { get; set; }


        public TileValue Value
        {
            get { return (TileValue)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(TileValue), typeof(TileCell), new PropertyMetadata(null, new PropertyChangedCallback(OnTileValueChanged)));

        private static void OnTileValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TileCell)d).OnTileValueChanged();
        }

        private void OnTileValueChanged()
        {
            Text.SetBinding(TextBlock.TextProperty, new Binding() { Source = Value, Path = new PropertyPath("Value"), Converter = new ZeroBlankConverter() });
            Editor.SetBinding(TextBox.TextProperty, new Binding() { Source = Value, Path = new PropertyPath("Value"), Converter = new ZeroBlankConverter(), Mode = BindingMode.TwoWay});
        }

        static bool shiftDown;

        private void OnEditorKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Shift)
            {
                shiftDown = true;
            }
            var key = e.Key;
            if (key == Windows.System.VirtualKey.Enter || key == Windows.System.VirtualKey.Escape)
            {
                Commit();
            }
            else if (key == Windows.System.VirtualKey.Left)
            {
                if (PreviousCell != null)
                {
                    PreviousCell(this, EventArgs.Empty);
                }
            }
            else if (key == Windows.System.VirtualKey.Right)
            {
                if (NextCell != null)
                {
                    NextCell(this, EventArgs.Empty);
                }
            }
            else if (key == Windows.System.VirtualKey.Tab)
            {
                if (shiftDown)
                {
                    if (PreviousCell != null)
                    {
                        PreviousCell(this, EventArgs.Empty);
                    }
                }
                else
                {
                    if (NextCell != null)
                    {
                        NextCell(this, EventArgs.Empty);
                    }
                }
            }
            else if (key == Windows.System.VirtualKey.Up)
            {
                if (UpCell != null)
                {
                    UpCell(this, EventArgs.Empty);
                    e.Handled = true;
                }
            }
            else if (key == Windows.System.VirtualKey.Down)
            {
                if (DownCell != null)
                {
                    DownCell(this, EventArgs.Empty);
                    e.Handled = true;
                }
            }
        }

        private void OnBorderPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }

        public event EventHandler NextCell;

        public event EventHandler PreviousCell;
        
        public event EventHandler UpCell;

        public event EventHandler DownCell;

        public event EventHandler Editing;

        public event EventHandler Committed;


        private void OnEditorKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Shift)
            {
                shiftDown = false;
            }
        }

        private void OnEditorGotFocus(object sender, RoutedEventArgs e)
        {
            CellEditState = TileCellEditState.Editing;
        }

    }
}
