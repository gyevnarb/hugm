using System;
using System.Collections.Generic;
using System.Linq;
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
                AreaNode n = new AreaNode(area.ID, new List<VotingArea> { area });
                G.AddNode(n);
            }
        }

        public void PopulateEdges(double threshold)
        {
            List<AreaNode> grouped = GroupSameAreas();
            foreach (AreaNode group in grouped)
            {
                AddIntraGroupEdges(group);
                AddEdgeWithinDistance(group, threshold);
            }
        }

        private List<AreaNode> GroupSameAreas()
        {
            List<AreaNode> grouped = Areas.GroupBy(x => x.FormattedAddress).Select(x => new AreaNode(x.First().ID, x.ToList())).ToList();
            return grouped;
        }

        private void AddIntraGroupEdges(AreaNode group)
        {
            List<VotingArea> areas = group.Areas;
            for (int i = 0; i < areas.Count - 1; i++)
                for (int j = i + 1; j < areas.Count; j++)
                    G.AddEdge(areas[i].ID, areas[j].ID);
        }
        
        private void AddEdgeWithinDistance(AreaNode origin, double d)
        {
            foreach (VotingArea area in Areas)
            {
                double dist = Distance(origin.Areas[0].LatitudeLongitude, area.LatitudeLongitude);
                if (dist < d && dist > 0)
                    G.AddEdge(origin.ID, area.ID);
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
        /// <summary>
        /// Group voting areas with same voting location
        /// </summary>
        public List<VotingArea> Areas { get; private set; }

        public AreaNode(int id) : base(id) { }
        public AreaNode(int id, List<VotingArea> areas) : base(id) => Areas = areas;

        public override string ToString()
        {
            return string.Format($"ID = {ID}; FormatteAddress = {Areas.First().FormattedAddress}");
        }
    }
}
