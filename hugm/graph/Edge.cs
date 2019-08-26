using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hugm.graph
{
    [Serializable]
    public class Edge
    {
        public Node N1 { get; private set; }
        public Node N2 { get; private set; }

        public Edge(Node n1, Node n2)
        {
            N1 = n1;
            N2 = n2;
        }
    }
}
