using Avalonia.Animation;
using Avalonia;
using System.Collections.Generic;
using System.Diagnostics;

namespace AvaloniaAnimatingBarChart.Utilities
{
    internal class PointCollectionTransition : InterpolatingTransitionBase<IList<Point>>
    {

        protected override IList<Point> Interpolate(double progress, IList<Point> from, IList<Point> to)
        {
            List<Point> result = new List<Point>();
            for (int i = 0, n = to.Count; i < n; i++)
            {
                Point p = new Point();
                if (i < from.Count)
                {
                    p = from[i];
                }
                Point q = to[i];
                Point r = new Point();
                if (progress == 1)
                {
                    r = q;
                }
                else
                {
                    // interpolate positions
                    r = r.WithX(p.X + (progress * (q.X - p.X)))
                         .WithY(p.Y + (progress * (q.Y - p.Y)));
                }
                if (i >= result.Count)
                {
                    result.Add(r);
                }
                else
                {
                    result[i] = r;
                }
            }
            return result;
        }
    }
}
