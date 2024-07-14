using System.Collections.Generic;

namespace AvoidAGrabCutEasy
{
    //https://www.geeksforgeeks.org/fifo-push-relabel-algorithm/ (modified: errors in code fixed)
    public partial class DirectedGraph
    {
        readonly public Dictionary<int, Dictionary<int, double>> adjacencyList;
        public int vertices;

        public DirectedGraph(int vertices)
        {
            this.vertices = vertices;

            adjacencyList = new Dictionary<int, Dictionary<int, double>>();
            for (int i = 0; i < vertices; i++)
                adjacencyList.Add(i, new Dictionary<int, double>());
        }

        public void addEdge(int u, int v,
                            double weight)
        {
            //if (!adjacencyList.ContainsKey(u))
            //    adjacencyList.Add(u, new Dictionary<int, double>());
            //if (!adjacencyList[u].ContainsKey(v))
            adjacencyList[u].Add(v, weight);
        }

        public bool hasEdge(int u, int v)
        {
            if (u >= vertices)
                return false;

            if (adjacencyList.ContainsKey(u))
            {
                Dictionary<int, double> l = adjacencyList[u];
                if (l.ContainsKey(v))
                    return true;
            }

            return false;
        }

        // Returns -1 if no edge
        // is found between u and v
        public int getEdge(int u, int v)
        {
            if (adjacencyList.ContainsKey(u))
            {
                Dictionary<int, double> l = adjacencyList[u];
                if (l.ContainsKey(v))
                    return v;
            }

            return -1;
        }
    }
}
