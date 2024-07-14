using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AvoidAGrabCutEasy
{
    //https://www.geeksforgeeks.org/fifo-push-relabel-algorithm/
    /// <summary>
    ///         //note that this implementation is *not* computing a (real or correct) minCut!!!  (Due to runtime-speed)
    ///         //Use pushStd, if you want to compute a MinCut on a Graph
    /// </summary>
    public class PushRelabelFifo : IMaxFlowAlg, IQATHAlg
    {
        private readonly int source;
        private readonly int sink;
        private readonly DirectedGraph graph;
        private int _oldU;
        private bool _writeDebug = false;
        private long _cnt;
        private long _cnt2;
        private double _eps = 1e-3;
        private long _cnt4;    
        private int _oldV;

        public DirectedGraph residualGraph { get; set; }

        public List<int> Result { get; private set; }
        public List<int> Result2 { get; private set; }
        public int QATH { get; set; } = 1000000;
        public bool GetSourcePartition { get; internal set; }
        public double NumItems { get; internal set; } = 10;
        public double NumCorrect { get; internal set; } = 5;
        public double NumItems2 { get; internal set; } = 10;
        public double NumCorrect2 { get; internal set; } = 2;
        public int MaxIter { get; set; }
        public int QATH2 { get; private set; }

        public event EventHandler<string> ShowInfo;

        public PushRelabelFifo(DirectedGraph graph, int source, int sink)
        {
            this.graph = graph;
            this.source = source;
            this.sink = sink;
        }

        private void initResidualGraph()
        {
            residualGraph = new DirectedGraph(graph.vertices);

            // Construct residual graph
            for (int u = 0; u < graph.vertices; u++)
            {
                Dictionary<int, double> l = graph.adjacencyList[u];
                foreach (int v in l.Keys)
                {
                    // If forward edge already
                    // exists, update its weight
                    if (residualGraph.hasEdge(u, v))
                        residualGraph.adjacencyList[u][v] += l[v];

                    // In case it does not
                    // exist, create one
                    else
                        residualGraph.addEdge(u, v, l[v]); //v.w = l[v]; v.i = v

                    // If backward edge does
                    // not already exist, add it
                    if (!residualGraph.hasEdge(v, u))
                        residualGraph.addEdge(v, u, 0);
                }
            }
        }

        public double FIFOPushRelabel(bool quick, BackgroundWorker bgw)
        {
            //CheckGraph();

            if (this.residualGraph == null)
                initResidualGraph();

            Queue<int> queue = new Queue<int>();

            this.QATH2 = this.QATH / 10;

            // Step 1: Initialize pre-flow

            // to store excess flow
            double[] e = new double[graph.vertices];

            // to store height of vertices
            int[] h = new int[graph.vertices];

            BitArray inQueue = new BitArray(graph.vertices, false);
            Dictionary<int, int> queueAdded = new Dictionary<int, int>();

            // set the height of source to V
            h[source] = graph.vertices;

            if (bgw != null && bgw.WorkerSupportsCancellation && bgw.CancellationPending)
                return e[sink];

            // send maximum flow possible
            // from source to all its adjacent vertices
            Dictionary<int, double> l = graph.adjacencyList[source];
            foreach (int v in l.Keys)
            {
                residualGraph.adjacencyList[source][v] = 0;
                residualGraph.adjacencyList[v][source] = l[v];

                if (!queueAdded.ContainsKey(v))
                    queueAdded.Add(v, 1);

                // update excess flow
                e[v] = l[v] < _eps ? 0 : l[v];

                if (v != sink)
                {
                    queue.Enqueue(v);
                    inQueue.Set(v, true);
                }
            }

            if (bgw != null && bgw.WorkerReportsProgress)
                bgw.ReportProgress(40);

            if (bgw != null && bgw.WorkerSupportsCancellation && bgw.CancellationPending)
                return e[sink];

            int cnt = 0;

            // Step 2: Update the pre-flow
            // while there remains an applicable
            // push or relabel operation
            while (queue.Count > 0 && cnt < this.MaxIter)
            {
                if (bgw != null && bgw.WorkerSupportsCancellation && bgw.CancellationPending)
                    break;

                // vertex removed from
                // queue in constant time
                int u = queue.Dequeue();
                inQueue.Set(u, false);

                if (!queueAdded.ContainsKey(u))
                    queueAdded.Add(u, 1);
                else
                    queueAdded[u] += 1;

                if (u == source || u == sink)
                    continue;

                relabel(u, h);
                push(u, e, h, queue, inQueue, quick, queueAdded);

                cnt++;

                if (cnt % 1000000 == 0)
                    OnShowInfo("Queue count: " + queue.Count + ". Iteration: " + cnt.ToString("N0") + " of " + this.MaxIter.ToString("N0"));
            }

            Console.WriteLine((cnt == this.MaxIter).ToString());

            if (bgw != null && bgw.WorkerReportsProgress)
                bgw.ReportProgress(90);

            if (bgw != null && bgw.WorkerSupportsCancellation && bgw.CancellationPending)
                return e[sink];

            List<int> result = new List<int>();    
            List<int> result2 = new List<int>();

            if (GetSourcePartition)
            {
                Dictionary<int, double> z = residualGraph.adjacencyList[source];
                int[] r = z.Where(a => a.Key != source && a.Key != sink && this.graph.adjacencyList[source][a.Key] > 0).Select(a => a.Key).ToArray();
                //int[] r = z.Where(a => a.Key != source && a.Key != sink && a.Value >= _eps).Select(a => a.Key).ToArray();
                result.AddRange(r);
                //result2.AddRange(r);
            }
            else
            {
                foreach (var u in residualGraph.adjacencyList.Keys)
                {
                    if (residualGraph.adjacencyList[u].Count > 0)
                    {
                        foreach (var v in residualGraph.adjacencyList[u].Keys)
                        {
                            if (v == sink && residualGraph.adjacencyList[u][v] >= _eps)
                                result.Add(u);
                        }
                    }
                }
            }

            //Console.WriteLine("results:");
            //Console.WriteLine(result.Count == result2.Count);
            //Console.WriteLine(result.SequenceEqual(result2));
            this.Result = result.Distinct().ToList(); //.OrderBy(a => a).ToList();
            //this.Result2 = result2.Distinct().ToList();

            Console.WriteLine(e[sink]);
            Console.WriteLine("count4: " + this._cnt4);

            OnShowInfo("Queue count: " + queue.Count + ". Iteration: " + cnt.ToString("N0") + " of " + this.MaxIter.ToString("N0"));

            return e[sink];
        }

        public double FIFOPushRelabelStd(bool quick, BackgroundWorker bgw)
        {
            //CheckGraph();

            if (this.residualGraph == null)
                initResidualGraph();

            Queue<int> queue = new Queue<int>();

            // Step 1: Initialize pre-flow

            // to store excess flow
            double[] e = new double[graph.vertices];

            // to store height of vertices
            int[] h = new int[graph.vertices];

            BitArray inQueue = new BitArray(graph.vertices, false);

            // set the height of source to V
            h[source] = graph.vertices;

            if (bgw != null && bgw.WorkerSupportsCancellation && bgw.CancellationPending)
                return e[sink];

            // send maximum flow possible
            // from source to all its adjacent vertices
            Dictionary<int, double> l = graph.adjacencyList[source];
            foreach (int v in l.Keys)
            {
                residualGraph.adjacencyList[source][v] = 0;
                residualGraph.adjacencyList[v][source] = l[v];

                // update excess flow
                e[v] = l[v] < _eps ? 0 : l[v];

                if (v != sink)
                {
                    queue.Enqueue(v);
                    inQueue.Set(v, true);
                }
            }

            if (bgw != null && bgw.WorkerReportsProgress)
                bgw.ReportProgress(40);

            if (bgw != null && bgw.WorkerSupportsCancellation && bgw.CancellationPending)
                return e[sink];

            int cnt = 0;

            // Step 2: Update the pre-flow
            // while there remains an applicable
            // push or relabel operation
            while (queue.Count > 0 && cnt < this.MaxIter)
            {
                if (bgw != null && bgw.WorkerSupportsCancellation && bgw.CancellationPending)
                    break;

                // vertex removed from
                // queue in constant time
                int u = queue.Dequeue();
                inQueue.Set(u, false);

                if (u == source || u == sink)
                    continue;

                relabelStd(u, h);
                pushStd(u, e, h, queue, inQueue, quick);

                cnt++;

                if (cnt % 1000000 == 0)
                    OnShowInfo("Queue count: " + queue.Count + ". Iteration: " + cnt.ToString("N0") + " of " + this.MaxIter.ToString("N0"));
            }

            Console.WriteLine((cnt == this.MaxIter).ToString());

            if (bgw != null && bgw.WorkerReportsProgress)
                bgw.ReportProgress(90);

            if (bgw != null && bgw.WorkerSupportsCancellation && bgw.CancellationPending)
                return e[sink];

            List<int> result = new List<int>();  

            if (GetSourcePartition)
            {
                Dictionary<int, double> z = residualGraph.adjacencyList[source];
                int[] r = z.Where(a => a.Key != source && a.Key != sink && this.graph.adjacencyList[source][a.Key] > 0).Select(a => a.Key).ToArray();
                //int[] r = z.Where(a => a.Key != source && a.Key != sink && a.Value >= _eps).Select(a => a.Key).ToArray();
                result.AddRange(r);
            }
            else
            {
                foreach (var u in residualGraph.adjacencyList.Keys)
                {
                    if (residualGraph.adjacencyList[u].Count > 0)
                    {
                        foreach (var v in residualGraph.adjacencyList[u].Keys)
                        {
                            if (v == sink && residualGraph.adjacencyList[u][v] >= _eps)
                                result.Add(u);
                        }
                    }
                }
            }

            //Console.WriteLine("results:");
            //Console.WriteLine(result.Count == result2.Count);
            //Console.WriteLine(result.SequenceEqual(result2));
            this.Result = result.Distinct().ToList();

            Console.WriteLine(e[sink]);

            OnShowInfo("Queue count: " + queue.Count + ". Iteration: " + cnt.ToString("N0") + " of " + this.MaxIter.ToString("N0"));

            return e[sink];
        }
        private void OnShowInfo(string message)
        {
            ShowInfo?.Invoke(this, message);
        }

        private void relabel(int u, int[] h)
        {
            int minHeight = int.MaxValue;

            Dictionary<int, double> l = residualGraph.adjacencyList[u];

            foreach (int v in l.Keys)
            {
                if (l[v] >= _eps)
                    minHeight = Math.Min(h[v], minHeight);
            }

            if (minHeight < Int32.MaxValue)
                h[u] = minHeight + 1;

            if (h[u] < 0 || minHeight == Int32.MaxValue)
                h[u] = 0;
        }

        //note that this is *not* computing a (real or correct) minCut!!! Use pushStd, if you want to compute a MinCut on a Graph
        private void push(int u, double[] e, int[] h,
                            Queue<int> queue, BitArray inQueue, bool quick, Dictionary<int, int> queueAdded)
        {
            Dictionary<int, double> l = residualGraph.adjacencyList[u];
            List<(int, double)> ll = new List<(int, double)>();

            //e[u] = e[u] < _eps ? 0: e[u];
            this._cnt2 = 0;

            foreach (int v in l.Keys)
            {
                // after pushing flow if
                // there is no excess flow,
                // then break
                if (e[u] < _eps)
                    break;

                // push more flow to
                // the adjacent v if possible
                if (l[v] >= _eps && h[v] < h[u])
                {
                    // flow possible
                    double f = Math.Min(e[u], l[v]);
                    f = f < _eps ? 0 : f;

                    ll.Add((v, f));
                    residualGraph.adjacencyList[v][u] += f;

                    e[u] -= f;
                    e[v] += f;

                    // add the new overflowing
                    // immediate vertex to queue
                    if (!inQueue.Get(v) && v != source && v != sink && e[v] >= _eps && !residualGraph.adjacencyList[_oldV].ContainsKey(v))
                    {
                        if (!queueAdded.ContainsKey(v))
                            queueAdded.Add(v, 1);
                        else
                            queueAdded[v] += 1;

                        if (queueAdded[v] < this.QATH2) 
                        {
                            if (this._cnt2 < this.NumCorrect)
                            {
                                queue.Enqueue(v);
                                inQueue.Set(v, true);
                                _oldV = v;
                            }
                        }
                        else
                            if (_writeDebug)
                            Console.WriteLine("inner: " + u.ToString() + ", " + v.ToString());

                        this._cnt2++;
                        if (this._cnt2 == NumItems)
                            this._cnt2 = 0;

                        this._cnt4++;
                    }
                }
            }

            foreach ((int, int) j in ll)
                l[j.Item1] -= j.Item2;

            // if after sending flow to all the
            // intermediate vertices, the
            // vertex is still overflowing.
            // add it to queue again
            if (!quick && /*!residualGraph.adjacencyList[_oldU].ContainsKey(u) &&*/ e[u] >= _eps/* && h[u] > 0 && u != source && u != sink*/)
            //if (!quick && /*!residualGraph.adjacencyList[_oldU].ContainsKey(u) &&*/ Math.Round(e[u], 6) > 0 /*&& h[u] > 0 && u != source && u != sink*/)
            {
                if (!queueAdded.ContainsKey(u))
                    queueAdded.Add(u, 1);
                else
                    queueAdded[u] += 1;

                if (queueAdded[u] < this.QATH)
                {
                    //re add only each ...th to the queue 
                    if (this._cnt < this.NumCorrect2)
                    {
                        queue.Enqueue(u);
                        inQueue.Set(u, true);
                        _oldU = u;
                    }
                }
                else
                    if (_writeDebug)
                    Console.WriteLine("outer: " + u.ToString());

                this._cnt++;
                if (this._cnt == NumItems2)
                    this._cnt = 0;
            }
        }

        private void relabelStd(int u, int[] h)
        {
            int minHeight = int.MaxValue;

            Dictionary<int, double> l = residualGraph.adjacencyList[u];

            foreach (int v in l.Keys)
            {
                if (l[v] >= _eps)
                    minHeight = Math.Min(h[v], minHeight);
            }

            if (minHeight < Int32.MaxValue)
                h[u] = minHeight + 1;

            if (h[u] < 0 || minHeight == Int32.MaxValue)
                h[u] = 0;
        }

        private void pushStd(int u, double[] e, int[] h,
                            Queue<int> queue, BitArray inQueue, bool quick)
        {
            Dictionary<int, double> l = residualGraph.adjacencyList[u];
            List<(int, double)> ll = new List<(int, double)>();

            //e[u] = Math.Round(e[u], 6);

            foreach (int v in l.Keys)
            {
                // after pushing flow if
                // there is no excess flow,
                // then break
                e[u] = e[u] < _eps ? 0 : e[u];
                if (e[u] == 0)
                    break;

                // push more flow to
                // the adjacent v if possible
                if (l[v] > 0 && h[v] < h[u])
                {
                    // flow possible
                    double f = Math.Min(e[u], l[v]);
                    f = f < _eps ? 0 : f;

                    ll.Add((v, f));
                    residualGraph.adjacencyList[v][u] += f;

                    e[u] -= f;
                    e[v] += f;

                    // add the new overflowing
                    // immediate vertex to queue
                    if (!inQueue.Get(v) && v != source && v != sink)
                    {
                        queue.Enqueue(v);
                        inQueue.Set(v, true);
                    }
                }
            }

            foreach ((int, int) j in ll)
                l[j.Item1] -= j.Item2;

            // if after sending flow to all the
            // intermediate vertices, the
            // vertex is still overflowing.
            // add it to queue again
            if (!quick && /*e[u] > 0 &&*/ e[u] >= _eps)
            {
                queue.Enqueue(u);
                inQueue.Set(u, true);
            }
        }

        public void DFS(int[][] rGraph, int V, int s, bool[] visited)
        {
            Stack<int> stack = new Stack<int>();
            stack.Push(s);
            while (stack.Count > 0)
            {
                int v = stack.Pop();
                if (!visited[v])
                {
                    visited[v] = true;
                    for (int u = 0; u < V; u++)
                    {
                        if (rGraph[v][u] != 0)
                        {
                            stack.Push(u);
                        }
                    }
                }
            }
        }
    }
}
