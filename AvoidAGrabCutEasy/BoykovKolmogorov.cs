using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using static System.Windows.Forms.LinkLabel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;

namespace AvoidAGrabCutEasy
{
    public class BoykovKolmogorov : IMaxFlowAlg //Correct Results but Too Slow For Pictures
    {
        private List<int> _activeList = new List<int>();
        private Queue<int> _orphans = new Queue<int>();

        private BitArray _activeItems = null;

        private int _source;
        private int _sink;
        private int _w;
        private int _h;
        private int[] Tree;
        private int[] _parent;
        private byte[] _origins;
        private List<int> _path = new List<int>();
        private double _bnCap;

        private int counter = 0;

        public DirectedGraph ResidualGraph { get; set; }
        public DirectedGraph Graph { get; private set; }
        public List<int> Result { get; private set; }
        public bool GetSourcePartition { get; internal set; }
        public int PL { get; private set; } = 0;

        public BackgroundWorker BGW { get; set; }

        private List<StartNode> _startNodes;

        private int _startMode;
        private int _oldCount;
        private List<int> _oldPath;
        private double _eps = 1e-3;
        private long _cnt;

        public int MaxCycles { get; internal set; } = 1000000;
        public int MaxIter
        {
            get { return MaxCycles; }
            set { MaxCycles = value; }
        }

        public bool QuickEstNoMinCut { get; internal set; } = true;
        public double NumItems { get; internal set; } = 10;
        public double NumCorrect { get; internal set; } = 2;
        public List<int> Result2 { get; private set; }
        public int CheckPathAt { get; private set; }
        public bool ReversedResult { get; private set; }
        internal BreakResult BreakResult { get; private set; }

        public event EventHandler<string> ShowInfo;

        //Grow etc updated
        public BoykovKolmogorov(DirectedGraph graph, DirectedGraph residualGraph, int source, int sink, int w, int h, List<StartNode> startNodes, bool addOnlySourceSink)
        {
            _activeList = new List<int>();
            _orphans = new Queue<int>();
            _path = new List<int>();

            this._source = source;
            this._sink = sink;
            this._w = w;
            this._h = h;

            int wh = w * h + 2;

            this._activeItems = new BitArray(wh, false);

            Tree = new int[wh];

            this.ResidualGraph = residualGraph;
            this.Graph = graph;
            this._parent = new int[wh];
            this._origins = new byte[wh];

            for (int j = 0; j < wh; j++)
            {
                this._parent[j] = -1;
                this._origins[j] = 0;
                //this._activeItems[j] = false;
                Tree[j] = -1;
            }

            if (startNodes != null && startNodes.Count > 0)
            {
                this.CheckPathAt = 0;
                if (addOnlySourceSink)
                {
                    //source and sink dont have parents
                    _activeList.Add(sink);
                    _activeItems[sink] = true;
                    //_parent[sink] = sink;
                    _origins[sink] = 2;
                    Tree[sink] = sink;

                    _activeList.Add(source);
                    _activeItems[source] = true;
                    //_parent[source] = source;
                    _origins[source] = 1;
                    Tree[source] = source;
                }
                else
                {
                    this.CheckPathAt = startNodes.Count;

                    for (int j = 0; j < startNodes.Count; j++)
                    {
                        _activeList.Add(startNodes[j].Indx);
                        _activeItems[startNodes[j].Indx] = true;

                        _parent[startNodes[j].Indx] = startNodes[j].Origin == 1 ? source : sink;
                        _origins[startNodes[j].Indx] = (byte)startNodes[j].Origin;
                        Tree[startNodes[j].Indx] = startNodes[j].Origin == 1 ? source : sink;
                    }
                }

                this._startNodes = startNodes;

                this._startMode = 1;
            }
            else
            {
                this.CheckPathAt = this._w * this._h * 2;

                //source and sink dont have parents
                _activeList.Add(sink);
                _activeItems[sink] = true;
                //_parent[sink] = sink;
                _origins[sink] = 2;
                Tree[sink] = sink;

                _activeList.Add(source);
                _activeItems[source] = true;
                //_parent[source] = source;
                _origins[source] = 1;
                Tree[source] = source;

                this._startMode = 0;
            }
        }

        public BoykovKolmogorov(DirectedGraph graph, int source, int sink)
        {
            _activeList = new List<int>();
            _orphans = new Queue<int>();
            _path = new List<int>();

            this._source = source;
            this._sink = sink;

            int wh = graph.vertices;

            this._activeItems = new BitArray(wh, false);

            Tree = new int[wh];

            this.ResidualGraph = GetResGraph(graph);
            this.Graph = graph;
            this._parent = new int[wh];
            this._origins = new byte[wh];

            for (int j = 0; j < wh; j++)
            {
                this._parent[j] = -1;
                this._origins[j] = 0;
                //this._activeItems[j] = false;
                Tree[j] = -1;
            }

            //source and sink dont have parents
            _activeList.Add(source);
            _activeItems[source] = true;
            _origins[source] = 1;
            Tree[source] = source;

            _activeList.Add(sink);
            _activeItems[sink] = true;
            _origins[sink] = 2;
            Tree[sink] = sink;

            this._startMode = 0;
        }

        private DirectedGraph GetResGraph(DirectedGraph graph)
        {
            DirectedGraph residualGraph = new DirectedGraph(graph.vertices);

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
            return residualGraph;
        }

        //too slow for pictures
        public double RunMinCut()
        {
            counter = 0;
            int cnt = 0;
            OnShowInfo("Starting alg... This may take some time.");
            while (_activeList.Count > 0 && counter < this.MaxCycles)
            {
                if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                    break;

                bool f = Grow();

                //do a check, since not always all nodes will be removed from the activeList as expected, depending on the Initialization of the components by Kpp
                if (((this._startMode == 1 && counter > Math.Max(this._startNodes.Count, this.CheckPathAt)) ||
                     (this._startMode == 0 && (counter > this._w * this._h * 2 || counter > this.CheckPathAt))) &&
                     counter % 1000 == 0)
                {
                    if (!CheckPath(/*this._path.Count / 4*/ cnt))
                    {
                        this.BreakResult = new BreakResult(counter, this.Tree);
                        break;
                    }
                }

                if (counter % 100000 == 0)
                    cnt += 4;

                if (!f)
                    break;

                Augment();
                Adopt();
                counter++;

                this.ResidualGraph = this.ResidualGraph;

                if (counter % 500 == 0)
                    OnShowInfo(counter.ToString() + " cycles done. LP: " + this.PL.ToString());
            }

            OnShowInfo(counter.ToString() + " cycles done. LP: " + this.PL.ToString());

            Console.WriteLine(this.PL.ToString());

            List<int> result = new List<int>();

            if (GetSourcePartition)
            {
                this.Result = new List<int>();

                for (int i = 0; i < this.Tree.Length; i++)
                    if (this.Tree[i] == _source && i < Graph.vertices - 2)
                        this.Result.Add(i);

                if (this.Result.Count == 0 && this.Tree.Length > 0)
                {
                    for (int i = 0; i < this.Tree.Length; i++)
                        if (this.Tree[i] == _sink && i < Graph.vertices - 2)
                            this.Result.Add(i);

                    this.ReversedResult = true;
                }
            }
            else
            {
                this.Result = new List<int>();

                for (int i = 0; i < this.Tree.Length; i++)
                    if (this.Tree[i] == _sink && i < Graph.vertices - 2)
                        this.Result.Add(i);

                if (this.Result.Count == 0 && this.Tree.Length > 0)
                {
                    for (int i = 0; i < this.Tree.Length; i++)
                        if (this.Tree[i] == _source && i < Graph.vertices - 2)
                            this.Result.Add(i);

                    this.ReversedResult = true;
                }
            }

            double res = 0;

            //foreach (var u in ResidualGraph.adjacencyList.Keys)
            //{
            //    if (ResidualGraph.adjacencyList[u].Count > 0)
            //    {
            //        foreach (var v in ResidualGraph.adjacencyList[u].Keys)
            //        {
            //            if (v == _sink && ResidualGraph.adjacencyList[u][v] >= _eps)
            //                res += ResidualGraph.adjacencyList[u][v];
            //        }
            //    }
            //}

            return res;
        }

        private bool CheckPath(int allowedChanges)
        {
            int j = this._activeList.Count;

            if (this._oldCount <= j)
            {
                if (this._oldPath == null)
                    this._oldPath = new List<int>();
                int c = 0;
                for (int i = 0; i < this._path.Count; i++)
                    if (this._oldPath.Count > i && this._path.Contains(this._oldPath[i]))
                        c++;
                Console.WriteLine(c);

                if (c >= this._path.Count - allowedChanges) // / 2)
                    return false;
            }

            this._oldCount = j;

            this._oldPath.Clear();
            this._oldPath.AddRange(this._path);

            return true;
        }

        public bool Grow()
        {
            while (_activeList.Count > 0)
            {
                int p = _activeList[0];
                if (p != -1 && _activeItems[p])
                {
                    Dictionary<int, double> l = ResidualGraph.adjacencyList[p];

                    foreach (int q in l.Keys)
                    {
                        double rCap = this.ResidualGraph.adjacencyList[q][p];
                        if (l[q] >= _eps || rCap >= _eps)
                        {
                            if (Tree[q] == -1)
                            {
                                _parent[q] = p;
                                _origins[q] = _origins[p];
                                Tree[q] = Tree[p];
                                _activeList.Insert(0, q);
                                _activeItems[q] = true;
                            }
                            else if (Tree[q] != -1 && Tree[p] != Tree[q])
                                return ConstructPath(p, q);
                        }
                    }
                }

                _activeList.RemoveAt(_activeList.IndexOf(p));
                _activeItems[p] = false;
            }

            this._path.Clear();
            return false;
        }

        private bool ConstructPath(int p, int q)
        {
            int fw = p;
            int bw = q;
            if (_origins[p] != 1)
            {
                fw = q;
                bw = p;
            }

            double bnCap = double.MaxValue;

            this._path.Clear();
            this._path.Add(fw);

            while (fw != _source)
            {
                int pr = _parent[fw];
                _path.Insert(0, pr);
                fw = pr;
            }

            while (bw != _sink)
            {
                int ch = _parent[bw];
                _path.Add(bw);
                bw = ch;
            }

            this._path.Add(_sink);

            this._bnCap = bnCap;

            if (_path.Count > this.PL)
                this.PL = _path.Count;

            return true;
        }

        public void Augment()
        {
            double bnCap = double.MaxValue;

            for (int i = 0; i < _path.Count - 1; i++)
                if (this.ResidualGraph.adjacencyList[_path[i]][_path[i + 1]] < bnCap)
                    bnCap = this.ResidualGraph.adjacencyList[_path[i]][_path[i + 1]];

            this._bnCap = bnCap;
            for (int i = 0; i < _path.Count - 1; i++)
            {
                if (this.ResidualGraph.adjacencyList[_path[i]].ContainsKey(_path[i + 1]))
                {
                    this.ResidualGraph.adjacencyList[_path[i]][_path[i + 1]] -= this._bnCap;
                    if (this.ResidualGraph.adjacencyList[_path[i]][_path[i + 1]] < _eps)
                        this.ResidualGraph.adjacencyList[_path[i]][_path[i + 1]] = 0;

                    if (_path[i] == _source || _path[i + 1] == _sink)
                        continue;

                    this.ResidualGraph.adjacencyList[_path[i + 1]][_path[i]] += this._bnCap;
                    if (this.ResidualGraph.adjacencyList[_path[i + 1]][_path[i]] < _eps)
                        this.ResidualGraph.adjacencyList[_path[i + 1]][_path[i]] = 0;


                }
                else
                    Console.WriteLine("should not happen " + i);
            }

            for (int i = 0; i < _path.Count - 1; i++)
            {
                //if (this.ResidualGraph.adjacencyList[_path[i]].ContainsKey(_path[i + 1]) /*||
                //    this.ResidualGraph.adjacencyList[_path[i + 1]].ContainsKey(_path[i])*/)
                //{
                if (this.ResidualGraph.adjacencyList[_path[i]][_path[i + 1]] < _eps)
                {
                    if (this.Tree[_path[i]] == this.Tree[_path[i + 1]])
                    {
                        if (this.Tree[_path[i]] == _source)
                        {
                            this._orphans.Enqueue(this._path[i + 1]);
                            this._parent[this._path[i + 1]] = -1;
                            this._origins[this._path[i + 1]] = 0;
                        }
                        else if (this.Tree[_path[i + 1]] == _sink)
                        {
                            this._orphans.Enqueue(this._path[i]);
                            this._parent[this._path[i]] = -1;
                            this._origins[this._path[i]] = 0;
                        }
                    }
                }
                //}
            }
        }

        public void Adopt()
        {
            while (this._orphans.Count > 0)
            {
                int orphan = this._orphans.Dequeue();

                bool hasValidParent = false;

                Dictionary<int, double> l = ResidualGraph.adjacencyList[orphan];
                foreach (int neighbor in l.Keys)
                {
                    if (_origins[neighbor] == _source || _origins[neighbor] == _sink)
                        if (Tree[orphan] == Tree[neighbor] &&
                            (ResidualGraph.adjacencyList[neighbor].ContainsKey(orphan) && ResidualGraph.adjacencyList[neighbor][orphan] >= _eps ||
                            ResidualGraph.adjacencyList[orphan].ContainsKey(neighbor) && ResidualGraph.adjacencyList[orphan][neighbor] >= _eps))
                        {
                            this._parent[orphan] = neighbor;
                            this._origins[orphan] = this._origins[neighbor];
                            hasValidParent = true;
                            break;
                        }
                }

                if (!hasValidParent)
                {
                    foreach (int neighbor in l.Keys)
                    {
                        if (Tree[orphan] == Tree[neighbor])
                        {
                            if (ResidualGraph.adjacencyList[neighbor].ContainsKey(orphan) && ResidualGraph.adjacencyList[neighbor][orphan] >= _eps ||
                                ResidualGraph.adjacencyList[orphan].ContainsKey(neighbor) && ResidualGraph.adjacencyList[orphan][neighbor] >= _eps)
                            {
                                if (!this._activeItems[neighbor])
                                    this._activeItems[neighbor] = true;

                                if (this._activeList.IndexOf(neighbor) == -1)
                                    this._activeList.Add(neighbor);
                            }

                            if (this._parent[neighbor] == orphan)
                            {
                                this._parent[neighbor] = -1;
                                this._orphans.Enqueue(neighbor);
                            }
                        }
                    }

                    Tree[orphan] = -1;
                    this._activeItems[orphan] = false;

                    int j = this._activeList.IndexOf(orphan);
                    if (this._activeList.Count > 0 && j != -1)
                        this._activeList.RemoveAt(j);
                }
            }
        }

        private void OnShowInfo(string message)
        {
            ShowInfo?.Invoke(this, message);
        }

        //Doesnt compute a correct minCut...but works with pictures
        public int RunPic()
        {
            counter = 0;
            OnShowInfo("Starting alg... This may take some time.");
            while (_activeList.Count > 0 && counter < this.MaxCycles)
            {
                if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                    break;

                bool f = GrowPic();

                //do a check, since not always all nodes will be removed from the activeList as expected, depending on the Initialization of the components by Kpp
                if (((this._startMode == 1 && counter > this._startNodes.Count) ||
                    (this._startMode == 0 && counter > this._w * this._h)) &&
                    counter % 1000 == 0)
                {
                    if (!CheckPath(this._path.Count / 2))
                    {
                        this.BreakResult = new BreakResult(counter, this.Tree);
                        break;
                    }
                }

                if (!f)
                    break;
                AugmentPic();
                AdoptPic();
                counter++;

                if (counter % 500 == 0)
                    OnShowInfo(counter.ToString() + " cycles done. LP: " + this.PL.ToString());
            }

            OnShowInfo(counter.ToString() + " cycles done. LP: " + this.PL.ToString());

            Console.WriteLine(this.PL.ToString());

            if (this._startMode == 0)
            {
                if (GetSourcePartition)
                {
                    this.Result = new List<int>();

                    for (int i = 0; i < this.Tree.Length; i++)
                        if (this.Tree[i] == _source && i < Graph.vertices - 2)
                            this.Result.Add(i);

                    if (this.Result.Count == 0 && this.Tree.Length > 0)
                    {
                        for (int i = 0; i < this.Tree.Length; i++)
                            if (this.Tree[i] == _sink && i < Graph.vertices - 2)
                                this.Result.Add(i);

                        this.ReversedResult = true;
                    }
                }
                else
                {
                    this.Result = new List<int>();

                    for (int i = 0; i < this.Tree.Length; i++)
                        if (this.Tree[i] == _sink && i < Graph.vertices - 2)
                            this.Result.Add(i);

                    if (this.Result.Count == 0 && this.Tree.Length > 0)
                    {
                        for (int i = 0; i < this.Tree.Length; i++)
                            if (this.Tree[i] == _source && i < Graph.vertices - 2)
                                this.Result.Add(i);

                        this.ReversedResult = true;
                    }
                }
            }
            else
            {
                //return the same for both modes, to get rect + scribble mode working
                this.Result = new List<int>();

                for (int i = 0; i < this.Tree.Length; i++)
                    if (this.Tree[i] == _sink && i < Graph.vertices - 2)
                        this.Result.Add(i);
            }

            return 0;
        }

        public bool GrowPic()
        {
            while (_activeList.Count > 0)
            {
                int p = _activeList[0];

                if (p != -1 && _activeItems[p])
                {
                    Dictionary<int, double> l = ResidualGraph.adjacencyList[p];

                    foreach (int q in l.Keys)
                    {
                        //just look in one direction for use with pictures, correct would be: double rCap = this.ResidualGraph.adjacencyList[q][p]; and "or" this in the "if"
                        double rCap = this.ResidualGraph.adjacencyList[q][p];
                        if (l[q] > 0 || (!this.QuickEstNoMinCut && rCap > 0))
                        {
                            if (Tree[q] == -1)
                            {
                                _parent[q] = p;
                                _origins[q] = _origins[p];
                                Tree[q] = Tree[p];
                                _activeList.Insert(0, q);
                                _activeItems[q] = true;
                            }
                            else
                            {
                                if (/*Tree[q] != 0 &&*/ Tree[p] != Tree[q])
                                    return ConstructPathPic(p, q);
                            }
                        }
                    }
                }
                else
                {
                    _activeList.RemoveAt(0);
                    _activeItems[p] = false;
                    continue;
                }

                if (this.QuickEstNoMinCut)
                {
                    if (this._cnt < this.NumCorrect)
                    {
                        _activeList.RemoveAt(_activeList.IndexOf(p));
                        _activeItems[p] = false;
                    }
                    else
                    {
                        //just remove the first element, this is incorrect, but needed for speed in pictures, correct would be: _activeList.IndexOf(p)
                        _activeList.RemoveAt(0);
                        _activeItems[0] = false;
                    }

                    this._cnt++;
                    if (this._cnt == NumItems)
                        this._cnt = 0;
                }
                else
                {
                    _activeList.RemoveAt(_activeList.IndexOf(p));
                    _activeItems[p] = false;
                }
            }

            this._path.Clear();
            return false;
        }

        private bool ConstructPathPic(int p, int q)
        {
            int fw = p;
            int bw = q;
            if (_origins[p] != 1)
            {
                fw = q;
                bw = p;
            }

            double bnCap = double.MaxValue;

            this._path.Clear();
            this._path.Add(fw);

            while (fw != _source)
            {
                int pr = _parent[fw];
                _path.Insert(0, pr);
                if (pr != _source && this.ResidualGraph.adjacencyList[fw].ContainsKey(pr))
                    bnCap = Math.Min(bnCap, this.ResidualGraph.adjacencyList[fw][pr]);
                fw = pr;
            }

            while (bw != _sink)
            {
                int pr = _parent[bw];
                _path.Add(pr);
                if (pr != _sink && this.ResidualGraph.adjacencyList[bw].ContainsKey(pr))
                    bnCap = Math.Min(bnCap, this.ResidualGraph.adjacencyList[bw][pr]);
                bw = pr;
            }

            this._bnCap = bnCap;

            if (_path.Count > this.PL)
                this.PL = _path.Count;

            return true;
        }

        public void AugmentPic()
        {
            for (int i = 0; i < _path.Count - 1; i++)
            {
                if (this.ResidualGraph.adjacencyList[_path[i]].ContainsKey(_path[i + 1]))
                {
                    this.ResidualGraph.adjacencyList[_path[i]][_path[i + 1]] -= this._bnCap;
                    if (Math.Round(this.ResidualGraph.adjacencyList[_path[i]][_path[i + 1]], 4) == 0)
                        this.ResidualGraph.adjacencyList[_path[i]][_path[i + 1]] = 0;
                    if (_path[i] == _source || _path[i + 1] == _sink)
                        continue;
                    this.ResidualGraph.adjacencyList[_path[i + 1]][_path[i]] += this._bnCap;
                    if (Math.Round(this.ResidualGraph.adjacencyList[_path[i + 1]][_path[i]], 4) == 0)
                        this.ResidualGraph.adjacencyList[_path[i + 1]][_path[i]] = 0;
                }
                else
                    Console.WriteLine(i);
            }

            for (int i = 0; i < _path.Count - 1; i++)
            {
                if (this.ResidualGraph.adjacencyList[_path[i]].ContainsKey(_path[i + 1]))
                {
                    if (Math.Round(this.ResidualGraph.adjacencyList[_path[i]][_path[i + 1]], 4) == 0)
                    {
                        if (this.Tree[_path[i]] == this.Tree[_path[i + 1]])
                        {
                            if (this.Tree[_path[i]] == _source)
                            {
                                this._orphans.Enqueue(this._path[i + 1]);
                                this._parent[this._path[i + 1]] = -1;
                            }
                            else if (this.Tree[_path[i]] == _sink)
                            {
                                this._orphans.Enqueue(this._path[i]);
                                this._parent[this._path[i]] = -1;
                            }
                        }
                    }
                }
            }
        }

        public void AdoptPic()
        {
            while (this._orphans.Count > 0)
            {
                int p = this._orphans.Dequeue();

                bool hasValidParent = false;

                Dictionary<int, double> l = ResidualGraph.adjacencyList[p];
                foreach (int q in l.Keys)
                {
                    if (_origins[q] == _source || _origins[q] == _sink)
                        if (Tree[p] == Tree[q] && this._origins[q] != 0 && q != -1 &&
                            ResidualGraph.adjacencyList[q].ContainsKey(p) && ResidualGraph.adjacencyList[q][p] > 0 ||
                            ResidualGraph.adjacencyList[p].ContainsKey(q) && ResidualGraph.adjacencyList[p][q] > 0)
                        {
                            this._parent[p] = q;
                            this._origins[p] = this._origins[q];
                            hasValidParent = true;
                            break;
                        }
                }

                if (!hasValidParent)
                {
                    foreach (int q in l.Keys)
                    {
                        if (Tree[p] == Tree[q])
                        {
                            if (ResidualGraph.adjacencyList[q].ContainsKey(p) && ResidualGraph.adjacencyList[q][p] > 0 ||
                                ResidualGraph.adjacencyList[p].ContainsKey(q) && ResidualGraph.adjacencyList[p][q] > 0)
                            {
                                if (!this._activeItems[q] && q != -1)
                                {
                                    this._activeList.Add(q);
                                    this._activeItems[q] = true;
                                }
                            }

                            if (this._parent[q] == p)
                            {
                                this._parent[q] = -1;
                                this._orphans.Enqueue(q);
                            }
                        }
                    }
                }

                Tree[p] = -1;

                int j = this._activeList.IndexOf(p);
                if (this._activeList.Count > 0 && j != -1)
                {
                    this._activeList.RemoveAt(j);
                    this._activeItems[p] = false;
                }
            }

            if (_activeList.Count > 0 && _activeItems[_activeList[0]])
            {
                _activeItems[_activeList[0]] = false;
                _activeList.RemoveAt(0);
            }
        }

    }
}
