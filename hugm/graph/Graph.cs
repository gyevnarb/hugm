using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hugm.graph
{
    /// <summary>
    /// Arbitrary graph with adjacency list representation
    /// </summary>
    public class Graph
    {
        /// <summary>
        /// Vertices (nodes) of the graph
        /// </summary>
        public List<Node> V { get; private set; }

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

        /// <summary>
        /// Add a new vertex (node) to the graph
        /// </summary>
        public void AddNode()
        {
            Node newNode = new Node(V.Count);
            V.Add(newNode);
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
        /// Gets all connected components of the graph. Uses BFS.
        /// </summary>
        /// <returns>List of all connected components in the graph</returns>
        public List<ConnectedComponent> GetConnectedComponents()
        {
            Queue<Node> schedule = new Queue<Node>();
            List<ConnectedComponent> ret = new List<ConnectedComponent>();

            foreach (Node n in V)
            {
                if (!n.Marked)
                {
                    n.Marked = true;
                    schedule.Enqueue(n);
                    ConnectedComponent newCp = new ConnectedComponent();
                    newCp.CP.Add(n);
                    while (schedule.Count > 0)
                    {
                        Node m = schedule.Dequeue();
                        foreach (Node p in m.Adjacents)
                        {
                            if (!p.Marked)
                            {
                                p.Marked = true;
                                schedule.Enqueue(p);
                                newCp.CP.Add(p);
                            }
                        }
                    }
                    ret.Add(newCp);
                }
                    
            }

            V.ForEach(x => x.Marked = false);
            return ret;
        }
    }
}
