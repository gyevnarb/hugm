using System;
using System.Collections.Generic;
using System.Linq;
using hugm.graph;

namespace createmap
{
    /// <summary>
    /// Aggregate class for creating a graph based on voting areas
    /// </summary>
    public class PopulateGraph
    {
        /// <summary>
        /// All voting areas present
        /// </summary>
        public List<VotingArea> Areas { get; private set; }

        /// <summary>
        /// Stores the graph. Has to be built first.
        /// </summary>
        public Graph G { get; private set; }
        
        /// <summary>
        /// Convenience method to build a voting areas graph
        /// </summary>
        /// <param name="path">Path of voting areas csv file</param>
        /// <returns>Graph representation of the voting areas</returns>
        public static Graph BuildGraph(string path)
        {
            Geocode coder = new Geocode();
            List<VotingArea> areas = coder.Run(path, false).GetAwaiter().GetResult();
            PopulateGraph pop = new PopulateGraph(areas);
            pop.PopulateNodes();
            pop.PopulateEdges(500.0);
            return pop.G;
        }

        /// <summary>
        /// Assign the voting areas and initialise empty graph
        /// </summary>
        /// <param name="areas">List of all voting areas</param>
        public PopulateGraph(List<VotingArea> areas)
        {
            Areas = areas;
            G = new Graph();
        }
        
        /// <summary>
        /// Add all nodes to the graph based on all voting areas
        /// </summary>
        public void PopulateNodes()
        {
            foreach (VotingArea area in Areas)
            {
                AreaNode n = new AreaNode(area.ID, new List<VotingArea> { area });
                G.AddNode(n);
            }
        }

        /// <summary>
        /// Add edges between nodes withing a given distance
        /// </summary>
        /// <param name="threshold">The limiting distance</param>
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
        
        /// <summary>
        /// Calculate distance between two coordinates using the Haversine-formula
        /// </summary>
        /// <param name="l1">First coordinate</param>
        /// <param name="l2">Second coordinate</param>
        /// <returns>Distance between the two coordinates in metres</returns>
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

        /// <summary>
        /// Degrees to radians
        /// </summary>
        /// <param name="d">Degrees</param>
        /// <returns>Radians</returns>
        public double ToRadians(double d)
        {
            return d * Math.PI / 180.0;
        }
    }
}
