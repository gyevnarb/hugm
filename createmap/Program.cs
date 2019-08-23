using System;
using System.Collections.Generic;
using hugm.graph;

namespace createmap
{
    class Program
    {
        public static void Main(string[] args)
        {
            Geocode coder = new Geocode();
            List<VotingArea> areas = coder.Run("../../data/korok_new.csv", false).GetAwaiter().GetResult();
            PopulateGraph pop = new PopulateGraph(areas);
            pop.PopulateNodes();
            pop.PopulateEdges(500.0);
            Graph g = pop.G;
            Console.ReadKey();
        }
    }
}
