using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using hugm.graph;

namespace createmap
{
    class PopulateGraph
    {
        public List<VotingArea> Areas { get; private set; }
        public Graph G { get; private set; }

        public PopulateGraph(List<VotingArea> areas)
        {
            Areas = areas;
            G = new Graph();
        }
        
        public void PopulateNodes()
        {
            foreach (VotingArea area in Areas)
            {
                AreaNode n = new AreaNode(area.ID, area);
                G.AddNode(n);
            }
        }

        public void PopulateEdges(double tresh)
        {
            foreach (AreaNode n in G.V)
            {
                AddEdgeWithinDistance(n, tresh); //TODO: Manage same places but different districts
            }
        }

        private void AddEdgeWithinDistance(AreaNode org, double d)
        {
            foreach (VotingArea area in Areas)
            {
                if (Distance(org.Area.LatitudeLongitude, area.LatitudeLongitude) < d)
                    G.AddEdge(org.ID, area.ID);
            }
        }

        public double Distance(LatLong l1, LatLong l2)
        {
            double R = 6371.0e3;
            double phi1 = ToRadians(l1.Latitude);
            double phi2 = ToRadians(l2.Latitude);
            double dphi = ToRadians(l2.Latitude - l1.Latitude);
            double dlng = ToRadians(l2.Longitude - l1.Longitude);

            double a = Math.Sin(dphi / 2.0) * Math.Sin(dphi / 2.0) +
                Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Sin(dlng / 2.0) * Math.Sin(dlng / 2.0);

            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
            return R * c;
        }

        public double ToRadians(double d)
        {
            return d * Math.PI / 180.0;
        }
    }

    class AreaNode : Node
    {
        public VotingArea Area { get; private set; }

        public AreaNode(int id) : base(id) { }
        public AreaNode(int id, VotingArea area) : base(id) => Area = area;
    }
}
