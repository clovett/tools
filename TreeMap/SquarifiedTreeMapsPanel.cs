using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace TreeMaps.Controls
{
    public class SquarifiedTreeMapsPanel : TreeMapsPanel
    {
        #region protected methods

        protected override Rect GetRectangle(RowOrientation orientation, TreeNodeData item, double x, double y, double width, double height)
        {
            if (orientation == RowOrientation.Horizontal)
            {
                return new Rect(x, y, width, item.RealArea / width);
            }
            else
            {
                return new Rect(x, y, item.RealArea / height, height);
            }
        }

        protected override void ComputeNextPosition(RowOrientation orientation, ref double xPos, ref double yPos, double width, double height)
        {
            if (orientation == RowOrientation.Horizontal)
                yPos += height;
            else
                xPos += width;
        }

        protected override void ComputeBounds()
        {
            if (this.Root != null && this.Root.Children != null)
            {
                this.Squarify(this.Root.Children, new List<TreeNodeData>(), this.GetShortestSide());
            }
        }

        #endregion

        #region private methods

        private void Squarify(List<TreeNodeData> items, List<TreeNodeData> row, double sideLength)
        {
            if (items.Count == 0)
            {
                this.AddRowToLayout(row);
                return;
            }

            TreeNodeData item = items[0];
            List<TreeNodeData> row2 = new List<TreeNodeData>(row);
            row2.Add(item);
            List<TreeNodeData> items2 = new List<TreeNodeData>(items);
            items2.RemoveAt(0);

            double worst1 = this.Worst(row, sideLength);
            double worst2 = this.Worst(row2, sideLength);

            if (row.Count == 0 || worst1 > worst2)
            {
                this.Squarify(items2, row2, sideLength);
            }
            else
            {
                this.AddRowToLayout(row);
                this.Squarify(items, new List<TreeNodeData>(), this.GetShortestSide());
            }
        }

        private void AddRowToLayout(List<TreeNodeData> row)
        {
            base.ComputeTreeMaps(row);
        }

        private double Worst(List<TreeNodeData> row, double w)
        {
            if (row.Count == 0) return 0;
            double maxArea = 0;
            double minArea = double.MaxValue;
            double s = 0;
            foreach (TreeNodeData item in row)
            {
                maxArea = Math.Max(maxArea, item.RealArea);
                minArea = Math.Min(minArea, item.RealArea);
                s += item.RealArea;
            }
            if (minArea == double.MaxValue) minArea = 0;
            double sSqr = s * s;
            double wSqr = w * w;
            double val1 = (wSqr * maxArea) / sSqr;
            double val2 = sSqr / (wSqr * minArea);
            return Math.Max(val1, val2);
        }

        #endregion
    }
}
