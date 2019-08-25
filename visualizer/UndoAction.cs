using hugm.graph;

namespace visualizer
{
    public class UndoAction
    {
        private Graph graph;
        private enum Type { NodeRemove, ConnectionRemove }
        private Type type;
        private Node removedNode;
        private Edge removedEdge;

        public UndoAction(Graph g, Node node)
        {
            graph = g;
            removedNode = node;
            type = Type.NodeRemove;
        }

        public UndoAction(Graph g, Edge edge)
        {
            graph = g;
            removedEdge = edge;
            type = Type.ConnectionRemove;
        }

        public void Undo()
        {
            switch(type)
            {
                case Type.NodeRemove: UndoNodeRemove(); break;
                case Type.ConnectionRemove: UndoConnectionRemove(); break;
            }
        }

        private void UndoConnectionRemove()
        {
            graph.AddEdge(removedEdge.N1, removedEdge.N2);
        }

        private void UndoNodeRemove()
        {
            foreach (var v in graph.V)
            {
                if (removedNode.Adjacents.Contains(v)) v.Adjacents.Add(removedNode);
            }
            graph.V.Add(removedNode);
        }
    }
}
