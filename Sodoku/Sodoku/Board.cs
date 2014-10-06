using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Sodoku
{
    public enum SodokuValidationScope {
        Tile,
        Row,
        Column
    }

    public class SodokuValidationEventArgs : EventArgs
    {
        public string Message { get; set; }
        public TileValue Changed { get; set; }
        public TileValue Conflict { get; set; }
        public SodokuValidationScope Scope { get; set; }
    }

    public class Board
    {
        TileValue[,] matrix = new TileValue[9,9];
        List<Tile> tiles = new List<Tile>();
        bool solved;

        public event EventHandler<SodokuValidationEventArgs> ValidationFailed;
        public event EventHandler ValidationSuccess;
        public event EventHandler Completed; // board is full

        public Board(int[] values)
        {
            if (values == null || values.Length != 81)
            {
                throw new Exception("Board must be initialized with 81 values");
            }
            for (int i = 0; i < 9; i++ )
            {
                tiles.Add(new Tile());
            }

            for (int i = 0; i < values.Length; i++ )
            {
                int row = i / 9;
                int col = i % 9;
                int v = values[i];
                var cell = new TileValue(v);
                matrix[row, col] = cell;

                int tileRow = row / 3;
                int tileCol = col / 3;
                Tile tile = tiles[(tileRow * 3) + tileCol];

                int cellRow = row % 3;
                int cellCol = col % 3;
                tile.SetValue(cellRow, cellCol, cell);
                cell.Parent = tile;

                cell.PropertyChanged += OnTilePropertyChanged;
            }

        }

        void OnTilePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value")
            {
                Validate((TileValue)sender, true);
            }
        }

        public bool Validate(TileValue changed, bool raiseValidationEvents)
        {
            Tile tile = changed.Parent;

            if (changed.Value != 0)
            {
                // make sure value is unique within this tile.
                foreach (TileValue v in tile.Values)
                {
                    if (v == changed || v.Value == 0)
                    {
                        continue;
                    }
                    if (v.Value == changed.Value)
                    {
                        // badness.
                        if (ValidationFailed != null && raiseValidationEvents)
                        {
                            ValidationFailed(this, new SodokuValidationEventArgs()
                            {
                                Message = "This tile must not contain duplicates",
                                Changed = changed,
                                Conflict = v,
                                Scope = SodokuValidationScope.Tile
                            });
                        }

                        return false;
                    }
                }


                int i = tiles.IndexOf(tile);
                int row = i / 3;
                int col = i % 3;
                int cellRow = changed.TileRow;
                int cellCol = changed.TileColumn;
                int j = (row * 3) + cellRow;
                int k = (col * 3) + cellCol;

                // validate the column is unique.
                for (int y = 0; y < 9; y++)
                {
                    TileValue v = matrix[y, k];
                    if (v == changed || v.Value == 0)
                    {
                        continue;
                    }
                    if (v.Value == changed.Value)
                    {
                        // badness.
                        if (ValidationFailed != null && raiseValidationEvents)
                        {
                            ValidationFailed(this, new SodokuValidationEventArgs()
                            {
                                Message = "This column must not contain duplicates",
                                Changed = changed,
                                Conflict = v,
                                Scope = SodokuValidationScope.Column
                            });
                        }

                        return false;
                    }
                }

                // validate the row is unique.
                for (int x = 0; x < 9; x++)
                {
                    TileValue v = matrix[j, x];
                    if (v == changed || v.Value == 0)
                    {
                        continue;
                    }
                    if (v.Value == changed.Value)
                    {
                        // badness.
                        if (ValidationFailed != null && raiseValidationEvents)
                        {
                            ValidationFailed(this, new SodokuValidationEventArgs()
                            {
                                Message = "This row must not contain duplicates",
                                Changed = changed,
                                Conflict = v,
                                Scope = SodokuValidationScope.Row
                            });
                        }

                        return false;
                    }
                }
            }


            if (ValidationSuccess != null)
            {
                ValidationSuccess(this, EventArgs.Empty);
            }

            // see if the game is complete
            solved = true;
            foreach (var t in tiles)
            {
                foreach (var v in t.Values)
                {
                    if (v.Value == 0)
                    {
                        solved = false;
                    }
                }

            }
            if (solved)
            {
                if (Completed != null)
                {
                    Completed(this, EventArgs.Empty);
                }
            }

            return true;
        }

        public Tile Tile1 { get { return tiles[0]; }   }
        public Tile Tile2 { get { return tiles[1]; }   }
        public Tile Tile3 { get { return tiles[2]; }   }
        public Tile Tile4 { get { return tiles[3]; }   }
        public Tile Tile5 { get { return tiles[4]; }   }
        public Tile Tile6 { get { return tiles[5]; }   }
        public Tile Tile7 { get { return tiles[6]; }   }
        public Tile Tile8 { get { return tiles[7]; }   }
        public Tile Tile9 { get { return tiles[8]; } }

        internal void Solve()
        {    
            // iterate forever:
            //  iterate each empty cell
            //  check if there is a value from 0 to 9 that is valid and is unique.
            //  store each valid choice, if there is only one choice set it.

            // this is the easy case, there is a more difficult case that requires look ahead when
            // there is no single valid choice, so you have to "try" setting one of the choices, plough ahead
            // and see what that does and if we get stuck with an empty cell that has NO valid choice then 
            // we have to backtrack and try one of the other choices.  This means we need to keep a list
            // of decisions so that we can backtrack.

            List<Choice> history = new List<Choice>();

            while (!solved)
            {
                bool changed = false;
                bool stuck = false;

                Dictionary<TileValue, Choice> choices = new Dictionary<TileValue, Choice>();

                foreach (Tile t in tiles)
                {
                    foreach (TileValue v in t.Values)
                    {
                        if (v.Value == 0)
                        {
                            List<int> validChoices = new List<int>();
                            // found empty cell.
                            for (int i = 1; i < 10; i++)
                            {
                                v.Value = i;
                                if (Validate(v, false))
                                {
                                    validChoices.Add(i);
                                }
                                v.Value = 0;
                            }

                            if (validChoices.Count == 1)
                            {
                                // hey we found one valid choice, so let's go with this!
                                int s = validChoices[0];
                                v.Value = s;
                                history.Add(new Choice() { Value = v, Choices = validChoices, Trial = s });
                                changed = true;
                            }
                            else if (validChoices.Count == 0)
                            {
                                // oh oh, no valid choices means we need to backtrack!
                                stuck = true;
                                break;
                            }
                            else
                            {
                                choices[v] = new Choice() { Value = v, Choices = validChoices, Trial = 0 };
                            }
                        }
                    }
                }
                if (stuck)
                {
                    // undo history back to a choice that has multiple valid choices.
                    for (int i = history.Count - 1; i >= 0; i--)
                    {
                        var choice = history[i];
                        if (choice.Choices.Count == 1)
                        {
                            choice.Value.Value = 0; // undo this change.
                        }
                        else
                        {
                            break;
                        }
                    }

                }
                
                if (!changed)
                {
                    // oh no, we're stuck, now we have to try the backtracking algorithm to search for a solution
                    // but it will work best if we pick the cell that has the smallest number of choices.
                    Choice bestChoice = null;
                    int shortest = int.MaxValue;
                    foreach (var pair in choices)
                    {
                        int len = pair.Value.Choices.Count;
                        if (len < shortest)
                        {
                            shortest = len;
                            bestChoice = pair.Value;
                        }
                    }

                    // now pick one of the valid values.

                    
                }
            }
        }

        class Choice
        {
            public TileValue Value;
            public int Trial; // the value we are trying for this cell.
            public List<int> Choices;
            public List<int> Invalid = new List<int>(); // invalid choices found by searching ahead
        }
    }
}