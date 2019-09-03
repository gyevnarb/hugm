using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using hugm.graph;

namespace hugm.map
{
    public class AreaUtils
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
        public int NumberOfNodesInDistance(Graph g, AreaNode start, double dist, Func<Coord, Coord, double> distanceFunc)
        {
            int n = 0;
            foreach (AreaNode an in g.V)
            {
                double d = distanceFunc(start.LatitudeLongitude, an.LatitudeLongitude);
                if (0 < d && d < dist) n++;
            }
            return n;
        }

        /// <summary>
        /// Saves the graph to disk
        /// </summary>
        /// <param name="path">Save path</param>
        /// <returns>True if the save was succesful, false otherwise</returns>
        public static bool Save(string path, Graph g)
        {
            try
            {
                IFormatter formatter = new BinaryFormatter();
                using (Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    formatter.Serialize(stream, g);
                    Console.WriteLine($"File saved to {path}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Load graph from disk
        /// </summary>
        /// <param name="path">Save path</param>
        /// <returns>True if the save was succesful, false otherwise></returns>
        public static Graph Load(string path)
        {
            try
            {
                IFormatter formatter = new BinaryFormatter();
                using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    var ret = formatter.Deserialize(stream) as Graph;
                    Console.WriteLine($"File loaded {path}");
                    return ret;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public static Graph MergeDistrictFromGraph(Graph source, Graph destination, string district)
        {
            if (source.V.Count != destination.V.Count)
                return null;

            foreach (Edge e in destination.E)
            {
                if ((e.N1 as AreaNode).Areas[0].CityDistrict == district && (e.N2 as AreaNode).Areas[0].CityDistrict == district)
                    destination.RemoveEdge(e);
            }
            foreach (Edge e in source.E)
            {
                if ((e.N1 as AreaNode).Areas[0].CityDistrict == district && (e.N2 as AreaNode).Areas[0].CityDistrict == district)
                    destination.AddEdge(e.N1.ID, e.N2.ID);
            }

            return destination;
        }
    }
}
