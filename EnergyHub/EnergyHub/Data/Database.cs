using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnergyHub.Data
{
    class SqlDatabase
    {
        public static SqlDatabase OpenDatabase(string filename)
        {
            SqlDatabase data = new SqlDatabase();

            // Initialize the database if necessary
            using (var db = new SQLite.SQLiteConnection(filename))
            {
                // Create the tables if they don't exist
                db.CreateTable<EnergyData>();
            }

            return data;
        }
    }
}
