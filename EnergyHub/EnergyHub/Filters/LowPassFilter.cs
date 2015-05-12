using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnergyHub.Filters
{
    public class LowPassFilter : BaseFilter
    {
        double smoothingFactor;
        bool first = true;
        double previous;

        public LowPassFilter(double smoothingFactor)
        {
            this.smoothingFactor = smoothingFactor;
        }

        public LowPassFilter(BaseFilter chained, double smoothingFactor)
            : base(chained)
        {
            this.smoothingFactor = smoothingFactor;
        }

        protected override double DoFilter(double input)
        {
            double result;

            if (first)
            {
                first = false;
                result = input;
            }
            else
            {
                double diff = input - previous;
                result = previous + (smoothingFactor * diff);
            }
            previous = result;
            return result;
        }
    }
}
