using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unhex
{
    class Program
    {
        static void Main(string[] args)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;

            using (FileStream stream = new FileStream(args[1], FileMode.Create, FileAccess.ReadWrite))
            {

                using (StreamReader reader = new StreamReader(args[0]))
                {
                    while (true)
                    {
                        string line = reader.ReadLine();
                        if (line == null)
                        {
                            break;
                        }

                        foreach (string b in line.Split(' '))
                        {
                            int result;
                            if (int.TryParse(b, NumberStyles.HexNumber, culture, out result))
                            {
                                stream.WriteByte((byte)result);
                            }
                        }

                    }
                }
            }
        }
    }
}
