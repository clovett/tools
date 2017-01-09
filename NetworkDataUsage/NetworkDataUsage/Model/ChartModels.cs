using System.Collections.Generic;

namespace NetgearDataUsage.Model
{
    public class DataSeries
    {
        public string Name { get; set; }

        public List<DataValue> Values { get; set; }
    }

    public class DataValue
    {
        public double X { get; set; }

        public double Y { get; set; }

        public string Label { get; set; }

        public string TipFormat; // has two arguments, the {0} actual and {1} target (or max value)

        public string ShortLabel; // ideal for short column labels.
    }

    public class Range
    {
        public double Minimum { get; set; }
        public double Maximum { get; set; }
    }

}
