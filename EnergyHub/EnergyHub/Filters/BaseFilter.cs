using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnergyHub.Filters
{
    public abstract class BaseFilter
    {
        BaseFilter chain;

        public BaseFilter()
        {

        }

        public BaseFilter(BaseFilter chained)
        {
            this.chain = chained;
        }

        public double Filter(double v)
        {
            if (chain != null)
            {
                v = chain.Filter(v);
            }
            return DoFilter(v);
        }

        protected abstract double DoFilter(double v);
    }
}
