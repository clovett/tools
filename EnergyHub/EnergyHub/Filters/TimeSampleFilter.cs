using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnergyHub.Filters
{
    /// <summary>
    /// This class takes a bunch of timestamped data values and returns a filtered set of values
    /// spread evenly over a given time range.  For example, you might start with the set 
    /// [(0,100),(5,90),(6,80),(10,70)] where the time values 0,5,6,10 might be seconds
    /// or milliseconds it doesn't matter - let's just call them Ticks for now.  
    /// Then when you request 10 values back covering a span of 10 Ticks.  This method returns:
    /// [(1,100),(2,97.5),(3,95),(4,92.5),(5,90),(6,80),(7,77.5),(8,75),(9,72.5),(10,70)]
    /// Notice 10 ticks from the last recording drops the 0 ticks off the beginning of the list.  
    /// Then notice that it smoothly ramps the values to meet each recorded data point along the way
    /// using a LowPassFilter algorithm.
    /// </summary>
    public class TimeSampleFilter
    {
        double smoothingRatio;
        LowPassFilter filter;
        List<TimeSample> data = new List<TimeSample>();
        TimeSample current;

        public TimeSampleFilter(double smoothingRatio)
        {
            this.smoothingRatio = smoothingRatio;
            filter = new LowPassFilter(smoothingRatio);
        }

        public void Add(TimeSample value)
        {
            current = value;
            data.Add(value);
        }

        /// <summary>
        /// If you call this method at a steady rate (like 30 times a second) you get a nice smoothed graph.
        /// </summary>
        /// <returns></returns>
        public double GetNextFilteredValue()
        {
            if (current == null)
            {
                return 0;
            }
            double result = filter.Filter(current.Value);
            return result;
        }


        /// <summary>
        /// Return the subset of the given data that contains TimeSamples in the given time range.
        /// </summary>
        /// <param name="data">The data to subset</param>
        /// <param name="startTicks">The time of the first TimeSample in the returned set</param>
        /// <param name="endTicks">The time of the last TimeSample in the returned set</param>
        /// <returns>The subset list of TimeSamples</returns>
        public List<TimeSample> GetSubset(long startTicks, long endTicks)
        {
            List<TimeSample> result = new List<TimeSample>();
            foreach (TimeSample s in data)
            {
                var ticks = s.Ticks;
                if (startTicks <= ticks && ticks <= endTicks)
                {
                    result.Add(s);
                }
            }
            return result;
        }

        /// <summary>
        /// Simulate a series of GetNextFilteredValue calls using past data in the given time range.
        /// </summary>
        /// <param name="startTicks">Time of first sample</param>
        /// <param name="endTicks">Time of last sample</param>
        /// <param name="numSamples">The number of GetNextFilteredValue values to return</param>
        /// <returns>The re-filtered values</returns>
        public List<double> Rerun(long startTicks, long endTicks, int numSamples)
        {
            var subset = GetSubset(startTicks, endTicks);
            
            List<double> result = new List<double>();
            TimeSampleFilter temp = new TimeSampleFilter(this.smoothingRatio);
            double timescale = endTicks - startTicks;
            timescale /= (double)numSamples;

            if (subset.Count > 0) 
            {
                int i = 0;
                TimeSample sample = subset[i];
                var next = (i + 1 < subset.Count) ? subset[i + 1] : null;
                temp.Add(sample);

                for (int x = 0; x < numSamples; x++)
                {
                    result.Add(temp.GetNextFilteredValue());

                    if (next != null && next.Ticks <= startTicks + (x * timescale))
                    {
                        i++;
                        temp.Add(next);
                        next = (i + 1 < subset.Count) ? subset[i + 1] : null;
                    }
                }
            }

            return result;
        }


        public void Clear()
        {
            filter = new LowPassFilter(smoothingRatio);
            data = new List<TimeSample>();
            current = null;
        }
    }

    /// <summary>
    /// This class contains a simple timestamp and value.
    /// </summary>
    public class TimeSample
    {
        public TimeSample()
        {
        }

        public TimeSample(TimeSample other) 
        {
            this.Ticks = other.Ticks;
            this.Value = other.Value;
        }
        public TimeSample(long ticks, double value)
        {
            this.Ticks = ticks;
            this.Value = value;
        }

        /// <summary>
        /// Whatever time scale you want to use.
        /// </summary>
        public long Ticks { get; set; }

        /// <summary>
        /// The value recorded at this time.
        /// </summary>
        public double Value { get; set; }
    }

}
