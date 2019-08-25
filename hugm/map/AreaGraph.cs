using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using hugm.graph;

namespace hugm.map
{
    public class AreaGraph : Graph
    {
        /// <summary>
        /// Calculate if there are any adjacents node with a given distance
        /// </summary>
        /// <param name="start">Node to measure distance from</param>
        /// <param name="dist">Max distance of nodes</param>
        /// <param name="distanceFunc">Distance function to use</param>
        /// <returns>True iff there is at least one node closer than dist</returns>
        public bool NodeWithinDistance(AreaNode start, double dist, Func<Coord, Coord, double> distanceFunc)
        {
            foreach (AreaNode an in start.Adjacents)
            {
                double d = distanceFunc(start.LatitudeLongitude, an.LatitudeLongitude);
                return 0 < d && d < dist;
            }
            return false;
        }

        /// <summary>
        /// Calculate how many nodes are within a given distance
        /// </summary>
        /// <param name="start">Node to count from</param>
        /// <param name="dist">Max distance of a node</param>
        /// <param name="distanceFunc">Distance function to use</param>
        /// <returns></returns>
        public int NumberOfNodesInDistance(AreaNode start, double dist, Func<Coord, Coord, double> distanceFunc)
        {
            Queue<AreaNode> schedule = new Queue<AreaNode>();
            int n = 0;
            schedule.Enqueue(start);
            start.Marked = true;
            while (schedule.Count > 0)
            {
                Node m = schedule.Dequeue();
                foreach (AreaNode p in m.Adjacents)
                {
                    if (!p.Marked && distanceFunc(start.LatitudeLongitude, p.LatitudeLongitude) < dist)
                    {
                        p.Marked = true;
                        schedule.Enqueue(p);
                        n++;
                    }
                }
            }
            return n;
        }
    }
}
