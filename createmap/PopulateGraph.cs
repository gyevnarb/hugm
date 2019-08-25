using System;
using System.Collections.Generic;
using System.Linq;
using hugm.graph;
using hugm.map;

namespace createmap
{
    public class PopulateGraph
    {
        public List<VotingArea> Areas { get; private set; }
        public Graph G { get; private set; }
        
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

        public PopulateGraph(List<VotingArea> areas)
        {
            Areas = areas;
            G = new Graph();
        }

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
                an.X *= 20000;
                an.Y *= 20000;
            }
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
            Coord centre = Areas[0].LatitudeLongitude;
            foreach (AreaNode group in grouped)
            {
                int areaNo = group.ID;
                double thr = CalculateThreshold(threshold, centre, group);
                AddIntraGroupEdges(group);
                AddEdgeWithinDistance(group, thr);
            }
        }

        private double CalculateThreshold(double threshold, Coord centre, AreaNode node)
        {
            if (threshold < 0)
            {
                Coord nodeCentre = node.Areas.First().LatitudeLongitude;
                switch (Distance(nodeCentre, centre))
                {
                    case double d when 0 <= d && d <= 500:
                        return 300.0;
                    case double d when 500 <= d && d < 1500:
                        return 500.0;
                    case double d when 1500 <= d && d < 3000:
                        return 700.0;
                    case double d when 3000 <= d && d < 6000:
                        return 900.0;
                    case double d when 6000 <= d && d < 12000:
                        return 1200.0;
                    case double d when 12000 <= d && d < 18000:
                        return 2000.0;
                    default:
                        return 2500.0;
                }
            }
            return threshold;
        }

        private List<AreaNode> GroupSameAreas()
        {
            List<AreaNode> groups = Areas.GroupBy(x => x.FormattedAddress).Select(x => new AreaNode(x.First().ID, x.ToList())).ToList();
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
                    G.AddEdge(areas[i].ID, areas[j].ID);
        }
        
        private void AddEdgeWithinDistance(AreaNode origin, double d)
        {
            foreach (AreaNode n in G.V)
            {
                double dist = Distance(origin.LatitudeLongitude, n.LatitudeLongitude);
                if (dist < d && dist > 0 && !n.NodeWithinDistance(d, Distance))
                    G.AddEdge(origin.ID, n.ID);
            }
        }
        
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

        public double ToRadians(double d)
        {
            return d * Math.PI / 180.0;
        }
    }
}
