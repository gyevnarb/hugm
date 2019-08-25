using System;
using System.Collections.Generic;
using System.Linq;
using hugm.graph;

namespace createmap
{
    public class PopulateGraph
    {
        public List<VotingArea> Areas { get; private set; }
        public Graph G { get; private set; }
        
        public static Graph BuildGraph(string path, bool geocode, int limit = -1)
        {
            Geocode coder = new Geocode();
            List<VotingArea> areas = coder.Run(path, geocode, limit).GetAwaiter().GetResult();
            PopulateGraph pop = new PopulateGraph(areas);
            pop.PopulateNodes();
            pop.PopulateEdges(500.0);
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
            double oLongitude = (800.0 / 360.0)*( 180.0 + origo.Areas[0].LatitudeLongitude.Lng);
            double oLatitude = (450.0 / 180.0) * (90.0 - origo.Areas[0].LatitudeLongitude.Lat);

            foreach(var v in G.V)
            {
                var an = v as AreaNode;
                an.X = (800.0 / 360.0) * ( 180.0 + an.Areas[0].LatitudeLongitude.Lng) - oLongitude;
                an.Y = (450.0 / 180.0) * (90.0 - an.Areas[0].LatitudeLongitude.Lat) - oLatitude;
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
            foreach (AreaNode group in grouped)
            {
                AddIntraGroupEdges(group);
                AddEdgeWithinDistance(group, threshold);
            }
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
                areas[i].LatitudeLongitude.Add(coordOffset);
                ;
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
            foreach (VotingArea area in Areas)
            {
                double dist = Distance(origin.Areas[0].LatitudeLongitude, area.LatitudeLongitude);
                if (dist < d && dist > 0)
                    G.AddEdge(origin.ID, area.ID);
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
