using System;
using System.Collections.Generic;
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
    public sealed partial class TileGrid : UserControl
    {
        List<TileCell> cells = new List<TileCell>();

        public TileGrid()
        {
            this.InitializeComponent();

            cells.Add(T1);
            cells.Add(T2);
            cells.Add(T3);
            cells.Add(T4);
            cells.Add(T5);
            cells.Add(T6);
            cells.Add(T7);
            cells.Add(T8);
            cells.Add(T9);

            this.Loaded += TileGrid_Loaded;
        }

        void TileGrid_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (TileCell cell in cells)
            {
                cell.ParentTile = this;
                cell.Name = this.Name + "." + cell.Name;
                cell.Value.Name = cell.Name;
            }
        }

        public IEnumerable<TileCell> Cells
        {
            get { return this.cells;  }
        }

        public Thickness TileBorderThickness
        {
            get { return (Thickness)GetValue(TileBorderThicknessProperty); }
            set { SetValue(TileBorderThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TileBorderThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TileBorderThicknessProperty =
            DependencyProperty.Register("TileBorderThickness", typeof(Thickness), typeof(TileGrid), new PropertyMetadata(null, new PropertyChangedCallback(OnTileBorderThicknessChanged)));

        private static void OnTileBorderThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TileGrid)d).OnTileBorderThicknessChanged();
        }

        private void OnTileBorderThicknessChanged()
        {
            this.Border.BorderThickness = this.TileBorderThickness;
        }

        
        public Tile Tile
        {
            get { return (Tile)GetValue(TileProperty); }
            set { SetValue(TileProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Tile.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TileProperty =
            DependencyProperty.Register("Tile", typeof(Tile), typeof(TileGrid), new PropertyMetadata(null, new PropertyChangedCallback(OnTileChanged)));

        private static void OnTileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TileGrid)d).OnTileChanged();
        }

        private void OnTileChanged()
        {
            
            // DataBinding can't quite do this, if we bind T1.Value to Tile.T1 Path=Value, then
            // TileCell wants to set it's DataContext to a Tile, but Tile doen't have a Value property
            // so the Path=Value fails.  Interesting limitation of nested bindings...
            
            T1.Value = Tile.T1;
            T2.Value = Tile.T2;
            T3.Value = Tile.T3;
            T4.Value = Tile.T4;
            T5.Value = Tile.T5;
            T6.Value = Tile.T6;
            T7.Value = Tile.T7;
            T8.Value = Tile.T8;
            T9.Value = Tile.T9;
            
        }

        internal void EditCell(int index)
        {
            TileCell cell = cells[index];
            cell.BeginEdit();
        }

        internal void UnhighlightAll()
        {
            foreach (TileCell cell in cells)
            {
                cell.CellState = TileCellState.None;
            }
        }

        internal TileCell GetCell(int cellRow, int cellCol)
        {
            int i = (cellRow * 3) + cellCol;
            return cells[i];
        }

        public TileCell GetCellForTileValue(TileValue value)
        {
            foreach (var cell in this.cells)
            {
                if (cell.Value == value)
                {
                    return cell;
                }
            }
            return null;
        }
    }
}
