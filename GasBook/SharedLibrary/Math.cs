using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary
{
    static class Math
    {
        /// <summary>
        /// Returns true if the two decimal values are within the given tolerance of each other.
        /// For example, if value x is 10 then x.IsClose(11, 1) returns true whereas x.IsClose(11, 0.1) 
        /// returns false.
        /// </summary>
        /// <param name="value">The value to compare</param>
        /// <param name="other">The other value to compare</param>
        /// <param name="tolerance">The tolerance that the difference must be less than</param>
        /// <returns>Returns true if the difference between the two numbers is less than the given tolerance</returns>
        public static bool IsClose(this decimal value, decimal other, decimal tolerance)
        {
            decimal diff = value - other;
            if (diff < 0) diff = -diff;
            return (diff <= tolerance);
        }
    }
}
