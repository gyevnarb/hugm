using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using core.graph;

namespace core
{
    using SimulationDist = Dictionary<Node, List<double>>;

    public static class ExtensionMethods
    {
        public static string ToJSON<K, V>(this Dictionary<K, V> dict)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append(string.Join(",\n", dict.Select(p => $"\"{p.Key}\": {p.Value}")));
            sb.Append("}");
            return sb.ToString();
        }

        public static string DistributionToJSON(this SimulationDist dist)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append(string.Join(",\n", dist.Select(p => $"\"{p.Key}\": {"[" + string.Join(", ", p.Value) + "]"}")));
            sb.Append("}");
            return sb.ToString();
        }
    }
}
