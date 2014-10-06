using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sodoku
{
    public class Tile 
    {
        List<TileValue>  values = new List<TileValue>();

        public Tile()
        {
            for (int i = 0; i < 9; i++)
            {
                values.Add(null);
            }
        }

        public IEnumerable<TileValue> Values { get { return values; } }

        public TileValue T1 { get { return values[0]; } }
        public TileValue T2 { get { return values[1]; } }
        public TileValue T3 { get { return values[2]; } }
        public TileValue T4 { get { return values[3]; } }
        public TileValue T5 { get { return values[4]; } }
        public TileValue T6 { get { return values[5]; } }
        public TileValue T7 { get { return values[6]; } }
        public TileValue T8 { get { return values[7]; } }
        public TileValue T9 { get { return values[8]; } }


        internal void SetValue(int cellRow, int cellCol, TileValue v)
        {
            if (cellRow > 2 || cellCol > 2 || cellRow < 0 || cellCol < 0)
            {
                throw new IndexOutOfRangeException("Cell address must be in the range [0 <= index <= 2]");
            }
            int i = (cellRow * 3) + cellCol;
            values[i] = v;
            v.TileColumn = cellCol;
            v.TileRow = cellRow; 
        }


    }

    public class TileValue : INotifyPropertyChanged
    {
        int v;
        bool locked;
        public Tile Parent { get; set; }

        /// <summary>
        /// Row in the 3x3 parent tile.
        /// </summary>
        public int TileRow { get; set; }

        /// <summary>
        /// Column in the 3x3 parent tile.
        /// </summary>
        public int TileColumn { get; set; }

        public TileValue(int i)
        {
            v = i;
            if (i != 0)
            {
                this.locked = true;
            }
        }

        public int Value
        {
            get { return this.v; }
            set
            {
                if (this.v != value)
                {
                    this.v = value;
                    OnPropertyChanged("Value");
                }
            }
        }

        public bool Locked
        {
            get { return this.locked;  }
            set
            {
                if (this.locked != value)
                {
                    this.locked = value;
                    OnPropertyChanged("Locked");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }


        public string Name { get; set; }
    }
}
