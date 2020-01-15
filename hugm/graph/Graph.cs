using System;
using System.Collections.Generic;

namespace hugm.graph
{
    /// <summary>
    /// Arbitrary graph with adjacency list representation
    /// </summary>
    [Serializable()]
    public class Graph
    {
        /// <summary>
        /// Vertices (nodes) of the graph
        /// </summary>
        public List<Node> V { get; private set; }

        public double left, right, top, bottom;

        /// <summary>
        /// Edges of the graph
        /// </summary>
        public List<Edge> E { get; private set; }

        /// <summary>
        /// Create an empty graph with no vertices or edges
        /// </summary>
        public Graph()
        {
            V = new List<Node>();
            E = new List<Edge>();
        }

        public Graph(List<Node> v)
        {
            V = v;
            E = new List<Edge>();
        }

        /// <summary>
        /// Add a new vertex (node) to the graph
        /// </summary>
        public void AddNode()
        {
            Node newNode = new Node(V.Count);
            V.Add(newNode);
        }

        /// <summary>
        /// Add a new vertex (node) to the graph
        /// </summary>
        /// <param name="n">Node to add</param>
        public void AddNode(Node n)
        {
            V.Add(n);
        }

        /// <summary>
        /// Removes a vertex (node) from the graph
        /// </summary>
        public void RemoveNode(Node node)
        {
            V.Remove(node);
            foreach (var n in V)
            {
                n.Adjacents.Remove(node);
            }
        }

        /// <summary>
        /// Add an edge to the graph
        /// </summary>
        /// <param name="e">Edge to add</param>
        public void AddEdge(Edge e)
        {
            if (V.Contains(e.N1) && V.Contains(e.N2) && !Adjacent(e.N1, e.N2))
            {
                e.N1.Adjacents.Add(e.N2);
                e.N2.Adjacents.Add(e.N1);
                E.Add(e);
            }
        }

        /// <summary>
        /// Add an edge between existing vertices (nodes) if one does not exist already.
        /// </summary>
        /// <param name="n1">First vertex</param>
        /// <param name="n2">Second vertex</param>
        public void AddEdge(Node n1, Node n2)
        {
            if (V.Contains(n1) && V.Contains(n2) && !Adjacent(n1, n2))
            {
                n1.Adjacents.Add(n2);
                n2.Adjacents.Add(n1);
                Edge newEdge = new Edge(n1, n2);
                E.Add(newEdge);
            }
        }

        /// <summary>
        /// Add an edge between existing vertices (nodes) if one does not exist already.
        /// </summary>
        /// <param name="n1">ID of first vertex</param>
        /// <param name="n2">ID of second vertex</param>
        public void AddEdge(int n1, int n2)
        {
            Node m1 = FindByID(n1);
            Node m2 = FindByID(n2);
            AddEdge(m1, m2);
        }

        /// <summary>
        /// Removes an edge (connection) from the graph
        /// </summary>
        public void RemoveEdge(Edge e)
        {
            e.N1.Adjacents.Remove(e.N2);
            e.N2.Adjacents.Remove(e.N1);
        }

        /// <summary>
        /// Remove all edges connected to a given node
        /// </summary>
        /// <param name="n">Node to remove edges from</param>
        public void RemoveEdge(Node n)
        {
            foreach (Edge e in E)
                if (e.N1 == n || e.N2 == n)
                    E.Remove(e);
        }

        /// <summary>
        /// Find a vertex (node) by its unique ID
        /// </summary>
        /// <param name="id">ID to search for</param>
        /// <returns>Node with the specified ID</returns>
        public Node FindByID(int id)
        {
            Node m = V.Find(n => n.ID == id);
            return m;
        }

        /// <summary>
        /// Returns whether there is an edge between two vertices (nodes).
        /// </summary>
        /// <param name="n1">First vertex</param>
        /// <param name="n2">Second vertex</param>
        /// <returns>True, if there is an edge between n1 and n2. False otherwise.</returns>
        public bool Adjacent(Node n1, Node n2)
        {
            return n1.Adjacents.Contains(n2);
        }

        /// <summary>
        /// Returns whether there is an edge between two vertices (nodes).
        /// </summary>
        /// <param name="n1">ID of first vertex</param>
        /// <param name="n2">ID of second vertex</param>
        /// <returns>True, if there is an edge between n1 and n2. False otherwise.</returns>
        public bool Adjacent(int n1, int n2)
        {
            Node m = FindByID(n1);
            if (m != null)
                return m.Adjacents.Exists(n => n.ID == n2);
            return false;
        }

        /// <summary>
        /// Run BFS from given node
        /// </summary>
        /// <param name="n">Starting node</param>
        /// <returns>All nodes reachable from n</returns>
        public List<Node> BFSFrom(Node n, bool closure) //TODO: Test BFSFrom
        {
            Queue<Node> schedule = new Queue<Node>();
            List<Node> reachable = new List<Node>();
            schedule.Enqueue(n);
            reachable.Add(n);
            n.Marked = true;
            while (schedule.Count > 0)
            {
                Node m = schedule.Dequeue();
                foreach (Node p in m.Adjacents)
                {
                    if (!p.Marked)
                    {
                        if (closure && !V.Contains(p))
                            continue;

                        p.Marked = true;
                        schedule.Enqueue(p);
                        reachable.Add(p);
                    }
                }
            }
            return reachable;
        }

        /// <summary>
        /// Gets all connected components of the graph. Uses BFS.
        /// </summary>
        /// <param name="closure">True if only the adjacent nodes must also be in V</param>
        /// <returns>List of all connected components in the graph</returns>
        public List<ConnectedComponent> GetConnectedComponents(bool closure=false)
        {           
            List<ConnectedComponent> ret = new List<ConnectedComponent>();
            foreach (Node n in V)
            {
                if (!n.Marked)
                {
                    List<Node> reached = BFSFrom(n, closure);
                    ret.Add(new ConnectedComponent(reached));
                }                  
            }

            V.ForEach(x => x.Marked = false);
            return ret;
        }        
    }
}
