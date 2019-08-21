using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hugm.graph
{
    public class Node
    {
        public int ID { get; private set; }
        public List<Node> Adjacents { get; set; }
        public bool Marked { get; set; }
        public double X, Y;

        public Node(int id)
        {
            ID = id;
            Adjacents = new List<Node>();
        }
    }

    public class ConnectedComponent
    {
        public List<Node> CP { get; set; }

        public ConnectedComponent() => CP = new List<Node>();
    }
}
