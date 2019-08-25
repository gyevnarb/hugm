using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using hugm.map;

namespace hugm.graph
{
    public class Node
    {
        public int ID { get; private set; }

        public List<Node> Adjacents { get; set; }

        public bool Marked { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public Node(int id)
        {
            ID = id;
            Adjacents = new List<Node>();
        }
    }
}
