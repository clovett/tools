using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if WINDOWS_PHONE
using System.Windows;
#else
using Windows.Foundation;
#endif


namespace FoscamExplorer
{
    struct Vector
    {
        double x;
        double y;

        public Vector(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public Vector(Point start, Point end)
        {
            this.x = end.X - start.X;
            this.y = end.Y - start.Y;
        }

        public double X
        {
            get { return x; }
            set { x = value; }
        }
        public double Y
        {
            get { return y; }
            set { y = value; }
        }

        public static Vector operator *(Vector left, double scalar)
        {
            return new Vector(left.X * scalar, left.Y * scalar);
        }

        public static Point operator +(Point start, Vector v)
        {
            return new Point(start.X + v.X, start.Y + v.Y);
        }

        public static Point operator -(Point start, Vector v)
        {
            return new Point(start.X - v.X, start.Y - v.Y);
        }

        public static Vector operator -(Vector v)
        {
            return new Vector(-v.X, -v.Y);
        }

        /// <summary>
        /// Normalize this vector (so the length is 1)
        /// </summary>
        public void Normalize()
        {
            double length = Math.Sqrt((x * x) + (y * y));
            if (length != 0)
            {
                // normalize the vector
                x /= length;
                y /= length;
            }
        }

        /// <summary>
        /// get the 90 degree normal to the given vector
        /// </summary>
        /// <returns></returns>
        public Vector Normal()
        {
            return new Vector(this.y, -this.x);
        }

        /// <summary>
        /// The length of the vector
        /// </summary>
        public double Length
        {
            get
            {
                return Math.Sqrt((x * x) + (y * y));
            }
        }

    }
}
