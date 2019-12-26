using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chimpmunk.Model
{
    public delegate void ProgressHandler(int min, int max, int value);

    /// <summary>
    /// This class solves search problems by exploring all valid paths through a complete graph 
    /// made up of the given nodes.  Each nodes is given a set of items and it explores
    /// the placement of these items by moving them along these paths searching for improvements 
    /// in the solution.  It is an iterative algorithm that finishes when no further improvements 
    /// can be found.
    /// </summary>
    class VectorSolver
    {
        public VectorSolver()
        {
            Nodes = new List<INode>();
            Items = new List<IItem>();
        }

        /// <summary>
        /// Add the initial list of nodes
        /// </summary>
        public List<INode> Nodes { get; }

        /// <summary>
        /// Add the initial list of items.  These should get assigned to nodes
        /// but if there are any left here after calling Solve then no solution
        /// could be found.
        /// </summary>
        public List<IItem> Items { get; }

        private void GreedyPack()
        {
            while (Items.Count > 0)
            { 
                // start off by just packing what we can into each node.
                foreach (var n in Nodes)
                {
                    foreach (var i in Items.ToArray())
                    {
                        if (n.Cost > n.TrySwap(null, i))
                        {
                            n.Add(i);
                            Items.Remove(i);
                        }
                    }
                }

                // if we have some left over then we need to add more stock.
                INode node = Nodes.Last();
                Nodes.Add(node.Clone());
            }
        }

        public string Solve(ProgressHandler progressHandler)
        {
            if (Nodes.Count == 0)
            {
                return "must have some nodes";
            }

            GreedyPack();

            FindPaths();

            int iteration = 0;
            int min = 0;
            int max = paths.Count;
            int count = 0;
            int totalIterations = 0;
            double initialScore = this.TotalCost;

            bool found = true;
            while (found)
            {
                found = false;

                for (int i = 0, n = paths.Count; i < n; i++)
                {
                    totalIterations++;
                    
                    if (progressHandler != null)
                    {
                        progressHandler(min, max, count);
                    }
                    count++;
                    INode[] path = paths[i];

                    if (FindBestFlow(path))
                    {
                        if (!found)
                        {
                            found = true;
                            max += paths.Count;
                        }
                    }
                }

                iteration++;
            }

            if (progressHandler != null)
            {
                progressHandler(min, max, max);
            }

            double finalScore = this.TotalCost;

            return string.Format("Reduced cost by {0} after searching {1} possible combinations", (int)(initialScore - finalScore), totalIterations);
        }

        private bool FindBestFlow(INode[] path)
        {
            // Ok, the path establishes the flow of movement of items between nodes.
            // So here all we need to do is find the best flow given the existing item layout
            // where the flow of items also must satisfy the constraints of not overfilling each node.
            // To help prune the search we need only consider nodes that will fit in the next path.

            int[] flow = new int[path.Length];
            
            return false;
        }

        private bool TryFlow(INode[] path, int[] flow, int pos)
        {
            INode source = path[pos];
            if (pos + 1 < path.Length)
            {
                INode target = path[pos + 1];
                int i = 0;
                foreach (IItem item in source.Items)
                {
                    foreach (IItem toRemove in target.Items)
                    {
                        if (target.TrySwap(toRemove, item) < target.Cost)
                        { 

                            flow[pos] = i;
                        }
                    }
                    i++;
                }
            }

            // todo:
            return false;
        }

        private double TotalCost
        {
            get
            {
                double result = 0;
                foreach (var n in this.Nodes)
                {
                    result += n.Cost;
                }
                return result;
            }
        }

        private List<INode[]> paths;

        private void FindPaths()
        {
            paths = new List<INode[]>();

            foreach (INode e in this.Nodes)
            {
                FindPaths(e, new HashSet<INode>(), new List<INode>());
            }

        }

        int memoryLimit;

        private void FindPaths(INode e, HashSet<INode> visited, List<INode> path)
        {
            visited.Add(e);
            path.Add(e);

            if (path.Count > 1)
            {
                try
                {
                    // try and remember this path.
                    AddPath(path);
                }
                catch (Exception ex)
                {
                    memoryLimit = paths.Count;
                    paths.Clear();
                    GC.Collect();
                    Debug.WriteLine("{0} adding path number {1}", ex.Message, memoryLimit);
                    throw;
                }
            }

            foreach (INode f in this.Nodes)
            {
                if (!visited.Contains(f))
                {
                    FindPaths(f, visited, path);
                }
            }
            path.Remove(e);
            visited.Remove(e);
        }

        private void AddPath(List<INode> path)
        {
            paths.Add(path.ToArray());

            // close this path to make a cycle
            INode first = path[0];
            path.Add(first);
            paths.Add(path.ToArray());
            path.Remove(first);
        }
    }

    /// <summary>
    /// This represents a node in the graph.
    /// </summary>
    interface INode
    {
        IEnumerable<IItem> Items { get; }

        /// <summary>
        /// A measure of the cost that we are trying to minimize.
        /// </summary>
        double Cost { get; }

        /// <summary>
        /// This method returns the net gain (or loss) from the Cost when
        /// item "toRemove" is replaced with item "toAdd".  
        /// </summary>
        /// <param name="toRemove">item to remove or null if you just want to add</param>
        /// <param name="toAdd">Item to add or null if you just want to remove</param>
        /// <returns>Returns new Cost that would result from the swap</returns>
        double TrySwap(IItem toRemove, IItem toAdd);

        /// <summary>
        /// Actually do a swap
        /// </summary>
        /// <param name="toRemove">item to remove or null if you just want to add</param>
        /// <param name="toAdd">Item to add or null if you just want to remove</param>
        /// <returns>False if the swap is illegal</returns>
        bool Swap(IItem toRemove, IItem toAdd);

        /// <summary>
        /// Add this item to the node.
        /// </summary>
        /// <param name="item"></param>
        void Add(IItem item);

        INode Clone();
    }


    /// <summary>
    /// This represents items we are adding to the nodes.
    /// </summary>
    interface IItem
    {
    }
}
