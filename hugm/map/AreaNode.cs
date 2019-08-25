using System;
using System.Collections.Generic;
using System.Linq;
using hugm.graph;

namespace hugm.map
{
    /// <summary>
    /// Specialised subclass to group voting areas
    /// </summary>
    public class AreaNode : Node
    {
        /// <summary>
        /// Group voting areas with same voting location
        /// </summary>
        public List<VotingArea> Areas { get; private set; }

        /// <summary>
        /// Get coordinates of first areas in Areas
        /// </summary>
        public Coord LatitudeLongitude { get { return Areas[0].LatitudeLongitude; } }

        /// <summary>
        /// Empty voting areas with ID
        /// </summary>
        /// <param name="id">ID to assign</param>
        public AreaNode(int id) : base(id) => Areas = new List<VotingArea>();

        /// <summary>
        /// Initialise voting areas
        /// </summary>
        /// <param name="id">ID of node</param>
        /// <param name="areas">Areas the node is grouping</param>
        public AreaNode(int id, List<VotingArea> areas) : base(id) => Areas = areas;

        /// <summary>
        /// Human-readable string of node
        /// </summary>
        /// <returns>ID and Formatted Address of node</returns>
        public override string ToString()
        {
            return string.Format($"ID = {ID}; FormattedAddress = {Areas.First().FormattedAddress}");
        }

        /// <summary>
        /// Calculate if there are any adjacents node with a given distance
        /// </summary>
        /// <param name="dist">Max distance of nodes</param>
        /// <param name="distanceFunc">Distance function to use</param>
        /// <returns>True iff there is at least one node closer than dist</returns>
        public bool NodeWithinDistance(double dist, Func<Coord, Coord, double> distanceFunc)
        {
            foreach (var n in Adjacents)
            {
                AreaNode an = n as AreaNode;
                double d = distanceFunc(Areas[0].LatitudeLongitude, an.Areas[0].LatitudeLongitude);
                return 0 < d && d < dist;
            }
            return false;
        }
    }
}
