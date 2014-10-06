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
    public sealed partial class SodokuGrid : UserControl
    {
        TileCell[,] matrix = new TileCell[9, 9];
        List<TileGrid> grids = new List<TileGrid>();

        public SodokuGrid()
        {
            this.InitializeComponent();

            grids.Add(G1);
            grids.Add(G2);
            grids.Add(G3);
            grids.Add(G4);
            grids.Add(G5);
            grids.Add(G6);
            grids.Add(G7);
            grids.Add(G8);
            grids.Add(G9);

            for(int j = 0; j < 9; j++)
            {
                for (int i = 0; i < 9; i++)
                {
                    int gridRow = j / 3;
                    int gridCol = i / 3;
                    TileGrid grid = grids[(gridRow * 3) + gridCol];

                    int cellRow = j - (gridRow * 3);
                    int cellCol = i - (gridCol * 3);

                    TileCell cell = grid.GetCell(cellRow, cellCol);
                    matrix[i,j] = cell;

                    cell.DownCell += OnDownCell;
                    cell.UpCell += OnUpCell;
                    cell.NextCell += OnNextCell;
                    cell.PreviousCell += OnPreviousCell;
                    cell.Editing += OnEditingCell;
                    cell.Committed += OnCommittedCell;
                }
            }

        }

        private void GetCellLocation(TileCell cell, TileGrid grid, out int col, out int row)
        {

            int index = grids.IndexOf(grid);
            int gridRow = index / 3;
            int gridCol = index % 3;
            int cellRow = cell.Value.TileRow;
            int cellCol = cell.Value.TileColumn;
            row = (gridRow * 3) + cellRow;
            col = (gridCol * 3) + cellCol;
        }

        private void OnDownCell(object sender, EventArgs e)
        {
            TileCell cell = (TileCell)sender;
            TileGrid grid = cell.ParentTile;
            int i, j;
            GetCellLocation(cell, grid, out i, out j);

            // move from top to bottom in the current column, simply wrap around when we hit the bottom.
            TileCell next = null;
            do
            {
                j++;
                if (j == 9)
                {
                    j = 0;
                }
                next = matrix[i, j];
            }
            while (next.Value.Locked && next != cell);

            next.BeginEdit();
        }


        private void OnUpCell(object sender, EventArgs e)
        {
            TileCell cell = (TileCell)sender;
            TileGrid grid = cell.ParentTile;
            int i, j;
            GetCellLocation(cell, grid, out i, out j);

            // move up from bottom to top in columns, simply wrap around when we hit the top.

            TileCell next = null;
            do
            {
                j--;
                if (j < 0)
                {
                    j = 8;
                }
                next = matrix[i, j];
            }
            while (next.Value.Locked && next != cell);
            next.BeginEdit();
        }

        private void OnPreviousCell(object sender, EventArgs e)
        {
            TileCell cell = (TileCell)sender;
            TileGrid grid = cell.ParentTile;
            int i, j;
            GetCellLocation(cell, grid, out i, out j);

            // move right left across the rows of the matrix, moving up when we hit the beginning of the row
            TileCell next = null;
            do
            {
                i--;
                if (i < 0)
                {
                    i = 8;
                    j--;
                    if (j < 0)
                    {
                        j = 8;
                    }
                }

                next = matrix[i, j];
            }
            while (next.Value.Locked && next != cell);

            next.BeginEdit();
        }

        void OnNextCell(object sender, EventArgs e)
        {
            TileCell cell = (TileCell)sender;
            TileGrid grid = cell.ParentTile;
            int i, j;
            GetCellLocation(cell, grid, out i, out j);

            // move left to right across the rows of the matrix, moving down when we hit the end of a row.
            TileCell next = null;
            do
            {
                i++;
                if (i == 9)
                {
                    i = 0;
                    j++;
                    if (j == 9)
                    {
                        j = 0;
                    }
                }

                next = matrix[i, j];
            }
            while (next.Value.Locked && next != cell);

            next.BeginEdit();
        }


        internal void HighlightRow(int row)
        {
            for (int i = 0; i < 9; i++ )
            {
                TileCell cell = matrix[i, row];
                cell.CellState = TileCellState.Highlighting;
            }
        }

        internal void HighlightColumn(int col)
        {

            for (int i = 0; i < 9; i++)
            {
                TileCell cell = matrix[col, i];
                cell.CellState = TileCellState.Highlighting;
            }
        }




        private void OnEditingCell(object sender, EventArgs e)
        {
            Unhighlight();

            TileCell cell = (TileCell)sender;
            TileGrid grid = cell.ParentTile;
            int i, j;
            GetCellLocation(cell, grid, out i, out j);

            HighlightRow(j);
            HighlightColumn(i);

        }

        private void Unhighlight()
        {
            foreach (TileGrid g in grids)
            {
                g.UnhighlightAll();
            }
        }

        private IEnumerable<TileGrid> GetColumnContaining(TileGrid grid)
        {
            int i = grids.IndexOf(grid); 
            int col = i % 3;

            foreach (TileGrid g in grids)
            {
                i = grids.IndexOf(g);
                int c = i % 3;
                if (c == col)
                {
                    yield return g;
                }
            }
        }

        private IEnumerable<TileGrid> GetRowContaining(TileGrid grid)
        {
            int i = grids.IndexOf(grid);
            int row = i / 3;

            foreach (TileGrid g in grids)
            {
                i = grids.IndexOf(g);
                int r = i / 3;
                if (r == row)
                {
                    yield return g;
                }
            }
        }

        private void OnCommittedCell(object sender, EventArgs e)
        {
            var nowait = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                TileCell cell = (TileCell)sender;
                Board.Validate(cell.Value, true);
            }));
        }

        public Board Board
        {
            get { return (Board)GetValue(BoardProperty); }
            set { SetValue(BoardProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Board.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BoardProperty =
            DependencyProperty.Register("Board", typeof(Board), typeof(SodokuGrid), new PropertyMetadata(null, new PropertyChangedCallback(OnBoardChanged)));

        
        static void OnBoardChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SodokuGrid)d).OnBoardChanged();
        }

        private void OnBoardChanged()
        {
            Board b = this.Board;
            G1.Tile = b == null ? null : Board.Tile1;
            G2.Tile = b == null ? null : Board.Tile2;
            G3.Tile = b == null ? null : Board.Tile3;
            G4.Tile = b == null ? null : Board.Tile4;
            G5.Tile = b == null ? null : Board.Tile5;
            G6.Tile = b == null ? null : Board.Tile6;
            G7.Tile = b == null ? null : Board.Tile7;
            G8.Tile = b == null ? null : Board.Tile8;
            G9.Tile = b == null ? null : Board.Tile9;

            b.ValidationFailed += OnValidationFailed;
            b.ValidationSuccess += OnValidationSuccess;
            b.Completed += OnCompleted;
        }

        void OnCompleted(object sender, EventArgs e)
        {
            ClearErrorState();
            Unhighlight();
            Message.Text = "Congratulations !!";
        }

        void OnValidationSuccess(object sender, EventArgs e)
        {
            // remove error highlights.
            ClearErrorState();

            Message.Text = "";
        }

        private void ClearErrorState()
        {
            foreach (TileGrid grid in this.grids)
            {
                foreach (TileCell cell in grid.Cells)
                {
                    if (cell.CellEditState != TileCellEditState.None)
                    {
                        cell.CellEditState = TileCellEditState.None;
                    }
                }
            }
        }

        void OnValidationFailed(object sender, SodokuValidationEventArgs e)
        {
            Message.Text = e.Message;

            ClearErrorState();

            switch (e.Scope)
            {
                case SodokuValidationScope.Tile:
                    HighlightErrorTile(e.Changed, e.Conflict);
                    break;
                case SodokuValidationScope.Row:
                    HighlightErrorRow(e.Changed, e.Conflict);
                    break;
                case SodokuValidationScope.Column:
                    HighlightErrorColumn(e.Changed, e.Conflict);
                    break;
                default:
                    break;
            }
        }

        TileCell GetCellContainingValue(TileValue v)
        {
            foreach (TileGrid grid in grids)
            {
                TileCell cell = grid.GetCellForTileValue(v);
                if (cell != null)
                {
                    return cell;
                }
            }
            return null;
        }

        private void HighlightErrorColumn(TileValue v1, TileValue v2)
        {
            var cell = GetCellContainingValue(v1);
            TileGrid tile = cell.ParentTile;
            int i = grids.IndexOf(tile);
            int tileRow = i / 3;
            int tileCol = i % 3;
            int x = (tileCol * 3) + v1.TileColumn;
            for (int y = 0; y < 9; y++)
            {
                cell = matrix[x, y];
                if (cell.Value == v1 || cell.Value == v2)
                {
                    cell.CellEditState = TileCellEditState.ErrorSource;
                }
                else
                {
                    cell.CellEditState = TileCellEditState.ErrorScope;
                }
            }
        }

        private void HighlightErrorRow(TileValue v1, TileValue v2)
        {
            var cell = GetCellContainingValue(v1);
            TileGrid tile = cell.ParentTile;
            int i = grids.IndexOf(tile);
            int tileRow = i / 3;
            int tileCol = i % 3;
            int y = (tileRow * 3) + v1.TileRow;
            for (int x = 0; x < 9; x++)
            {
                cell = matrix[x, y];
                if (cell.Value == v1 || cell.Value == v2)
                {
                    cell.CellEditState = TileCellEditState.ErrorSource;
                }
                else
                {
                    cell.CellEditState = TileCellEditState.ErrorScope;
                }
            }
        }

        private void HighlightErrorTile(TileValue v1, TileValue v2)
        {
            var vcell = GetCellContainingValue(v1);
            TileGrid tile = vcell.ParentTile;

            foreach (TileCell cell in tile.Cells)
            {
                if (cell.Value == v1 || cell.Value == v2)
                {
                    cell.CellEditState = TileCellEditState.ErrorSource;
                }
                else
                {
                    cell.CellEditState = TileCellEditState.ErrorScope;
                }
            }
        }

    }
}
