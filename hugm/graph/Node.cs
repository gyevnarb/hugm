using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hugm.graph
{
    class Node
    {
        public int ID { get; private set; }
        public List<Node> Adjacents { get; set; }
        public bool Marked { get; set; }

        public Node(int id)
        {
            ID = id;
            Adjacents = new List<Node>();
        }
    }

    class ConnectedComponent
    {
        public List<Node> CP { get; set; }

        public ConnectedComponent() => CP = new List<Node>();
    }
}
