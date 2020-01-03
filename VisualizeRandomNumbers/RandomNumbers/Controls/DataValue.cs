using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace RandomNumbers.Controls
{
    public class DataValue
    {
        public double X;
        public double Y;
        public string Label;
        public Brush Color;

        public override string ToString()
        {
            return string.Format("{0},{1}", X, Y);
        }
    }

    public class LineData
    {
        public double X1;
        public double Y1;
        public double X2;
        public double Y2;
        public Brush Color;
    }

}
