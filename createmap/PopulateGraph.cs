using System;
using System.Collections.Generic;
using System.Linq;
using hugm.graph;
using hugm.map;

namespace createmap
{
    /// <summary>
    /// Create graph from voting data
    /// </summary>
    public class PopulateGraph
    {
        /// <summary>
        /// Stores all voting area information
        /// </summary>
        public List<VotingArea> Areas { get; private set; }

        /// <summary>
        /// Graph of voting areas. Must be built first
        /// </summary>
        public Graph G { get; private set; }
        
        /// <summary>
        /// Convenience method to build the voting area graph
        /// </summary>
        /// <param name="path">Path to voting area data</param>
        /// <param name="geocode">True if geocoding has to be done</param>
        /// <param name="thresh">Neighbouring nodes edge distance limit</param>
        /// <param name="limit">Number of elements to subset from Areas</param>
        /// <returns>Fully build graph of all voting areas</returns>
        public static Graph BuildGraph(string path, bool geocode, double thresh = -1.0, int limit = -1)
        {
            Geocode coder = new Geocode();
            List<VotingArea> areas = coder.Run(path, geocode, limit).GetAwaiter().GetResult();

            PopulateGraph pop = new PopulateGraph(areas);
            pop.PopulateNodes();
            pop.PopulateEdges(thresh);
            pop.CalculateXY();

            return pop.G;
        }

        /// <summary>
        /// Create empty Graph and intialise Areas
        /// </summary>
        /// <param name="areas">Value with to initialse Areas</param>
        public PopulateGraph(List<VotingArea> areas)
        {
            Areas = areas;
            G = new Graph();
        }

        /// <summary>
        /// Calculate X-Y coordinates of nodes for display
        /// </summary>
        public void CalculateXY()
        {
            var origo = G.V[0] as AreaNode;
            double oLongitude = (800.0 / 360.0)*( 180.0 + origo.LatitudeLongitude.Lng);
            double oLatitude = (450.0 / 180.0) * (90.0 - origo.LatitudeLongitude.Lat);

            foreach(var v in G.V)
            {
                var an = v as AreaNode;
                an.X = (800.0 / 360.0) * ( 180.0 + an.LatitudeLongitude.Lng) - oLongitude;
                an.Y = (450.0 / 180.0) * (90.0 - an.LatitudeLongitude.Lat) - oLatitude;
                an.X *= 10000;
                an.Y *= 10000;
            }
        }
        
        /// <summary>
        /// Add all nodes to graph based on Areas
        /// </summary>
        public void PopulateNodes()
        {
            foreach (VotingArea area in Areas)
            {
                AreaNode n = new AreaNode(area.AreaID, new List<VotingArea> { area });
                G.AddNode(n);
            }
        }

        /// <summary>
        /// Add edges based on G.V. Add edges between groups first, then add edges based on threshold.
        /// </summary>
        /// <param name="threshold">Max distance between nodes that have edges. Set to -1 for automatic thresholding</param>
        public void PopulateEdges(double threshold)
        {
            List<AreaNode> grouped = GroupSameAreas();
            Coord centre = Areas[0].LatitudeLongitude;
            foreach (AreaNode group in grouped)
            {
                int areaNo = group.ID;
                double thr = threshold;
                AddIntraGroupEdges(group);
                AddEdgeWithinDistance(group, thr);

                /*
                if (group.Adjacents.Count == 0 && G.NumberOfNodesInDistance(group, 1000.0, Distance) < 10)
                    AddEdgeWithinDistance(group, 1000.0);
                else if (group.Adjacents.Count == 0 && G.NumberOfNodesInDistance(group, 2000.0, Distance) < 5)
                    AddEdgeWithinDistance(group, 2000.0);
                */
            }
        }

        //private double CalculateThreshold(double threshold, Coord centre, AreaNode node)
        //{
        //    if (threshold < 0) //This is bullshit lol
        //    {
        //        Coord nodeCentre = node.Areas.First().LatitudeLongitude;

        //        if (G.NumberOfNodesInDistance(node, 500.0, Distance) > 20)
        //            return 300.0;

        //        switch (Distance(nodeCentre, centre))
        //        {
        //            case double d when 0 <= d && d <= 1500:
        //                return 500.0;
        //            case double d when 1500 <= d && d < 3000:
        //                return 700.0;
        //            case double d when 3000 <= d && d < 6000:
        //                return 900.0;
        //            case double d when 6000 <= d && d < 12000:
        //                return 1200.0;
        //            case double d when 12000 <= d && d < 18000:
        //                return 2000.0;
        //            default:
        //                return 2500.0;
        //        }
        //    }
        //    return threshold;
        //}

        private List<AreaNode> GroupSameAreas()
        {
            List<AreaNode> groups = Areas.GroupBy(x => x.FormattedAddress).Select(x => new AreaNode(x.First().AreaID, x.ToList())).ToList();
            foreach (AreaNode group in groups)
            {
                OffsetVotingAreas(group.Areas);
            }
            return groups;
        }

        private void OffsetVotingAreas(List<VotingArea> areas)
        {
            double offset = 10.0 * 0.0002777778; //0°0'1" in degrees. One second is around 25m
            int areaCount = areas.Count();
            Coord centre = areas.First().LatitudeLongitude;
            for (int i = 1; i < areaCount; i++) // Skip first, which is the centre
            {
                double angle = 2.0 * Math.PI / areaCount;
                double xOffset = offset * Math.Cos(i * angle);
                double yOffset = offset * Math.Sin(i * angle);
                Coord coordOffset = new Coord(xOffset, yOffset);
                areas[i].LatitudeLongitude += coordOffset;
            }
        }

        private void AddIntraGroupEdges(AreaNode group)
        {
            List<VotingArea> areas = group.Areas;
            for (int i = 0; i < areas.Count - 1; i++)
                for (int j = i + 1; j < areas.Count; j++)
                    G.AddEdge(areas[i].AreaID, areas[j].AreaID);
        }
        
        private void AddEdgeWithinDistance(AreaNode origin, double d)
        {
            foreach (AreaNode n in G.V)
            {
                double dist = Distance(origin.LatitudeLongitude, n.LatitudeLongitude);
                if (dist < d && dist > 0) //&& !G.NodeWithinDistance(n, d, Distance))
                    G.AddEdge(origin.ID, n.ID);
            }
        }
        
        /// <summary>
        /// Calculate distance between two coordinates using the Haversine formula
        /// </summary>
        /// <param name="l1">First coordinate</param>
        /// <param name="l2">Second coordinate</param>
        /// <returns>Distance in metres between the two coordinates</returns>
        public double Distance(Coord l1, Coord l2)
        {
            double R = 6371.0e3;
            double phi1 = ToRadians(l1.Lat);
            double phi2 = ToRadians(l2.Lat);
            double dphi = ToRadians(l2.Lat - l1.Lat);
            double dlng = ToRadians(l2.Lng - l1.Lng);

            double a = Math.Sin(dphi / 2.0) * Math.Sin(dphi / 2.0) +
                Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Sin(dlng / 2.0) * Math.Sin(dlng / 2.0);

            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
            return R * c;
        }

        /// <summary>
        /// Convert degrees to radians
        /// </summary>
        /// <param name="d">Degrees</param>
        /// <returns>Radians</returns>
        public double ToRadians(double d)
        {
            return d * Math.PI / 180.0;
        }
    }
}
