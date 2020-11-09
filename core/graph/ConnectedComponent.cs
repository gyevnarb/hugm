using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace core.graph
{
    public class ConnectedComponent
    {
        public List<Node> CP { get; set; }

        public ConnectedComponent() => CP = new List<Node>();
        public ConnectedComponent(List<Node> nodes) => CP = nodes;
    }
}
