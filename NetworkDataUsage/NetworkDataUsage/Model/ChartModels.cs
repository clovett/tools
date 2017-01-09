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

    }

    public class Range
    {
        public double Minimum { get; set; }
        public double Maximum { get; set; }
    }

}
