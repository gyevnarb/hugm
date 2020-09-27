using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using hugm.map;

namespace hugm.graph
{
    [Serializable]
    public class Node
    {
        public const Node EmptyNode = null;

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

        public virtual string ToJSON()
        {
            return "{" + $"\"id\": {ID},\"adjacents\": [{string.Join(",", Adjacents.Select(x => x.ID))}], \"marked\": {Marked.ToString().ToLower()}, \"x\": {X}, \"y\": {Y}" + "}";
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Node))
                return false;

            Node n = obj as Node;
            return ID == n.ID;
        }

        public override int GetHashCode()
        {
            return ID * (ID * (ID + 3) + 7) % 986324051;
        }

        public override string ToString()
        {
            return ID.ToString(); //$"{ID}: {string.Join(",", Adjacents)}";
        }
    }
}
