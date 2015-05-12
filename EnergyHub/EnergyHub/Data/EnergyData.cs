using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnergyHub.Data
{
    class EnergyData
    {
        [SQLite.PrimaryKey]
        public long Id { get; set; }
        public DateTime Time { get; set; }
        public double Energy { get; set; }
    }

}
