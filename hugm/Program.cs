using System;
using hugm.graph;
using hugm.map;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hugm
{
    class Program
    {
        static void Main(string[] args)
        {
            Graph g = AreaUtils.Load("map.bin");
            /*
            foreach (var group in g.V.GroupBy(n => (n as AreaNode).Areas[0].ElectoralDistrict))
            {
                Graph m = new Graph(group.ToList());
                var c = m.GetConnectedComponents(true);
                Console.WriteLine(c.Count);
            }
            */
            MapToData();
            Console.ReadLine();
        }

        private static void MergeDistricts()
        {
            Console.Write("Source graph path: ");
            string source = Console.ReadLine();
            Console.Write("Destination graph path: ");
            string destination = Console.ReadLine();
            Console.Write("District in roman numerals: ");
            string district = Console.ReadLine();
            Graph s = AreaUtils.Load(source);
            Graph d = AreaUtils.Load(destination);
            Graph m = AreaUtils.MergeDistrictFromGraph(s, d, district);
            if (m != null) AreaUtils.Save("merge.bin", m);
            Console.WriteLine("Merged successfully");
        }

        private static void MapToData()
        {
            Console.Write("Path to map: ");
            string map = Console.ReadLine();
            if (map == "") map = "map.bin";

            Console.Write("Write path: ");
            string output = Console.ReadLine();
            if (output == "") output = "./";

            Graph m = AreaUtils.Load(map);

            using (StreamWriter sw = new StreamWriter(output + @"maplist.csv"))
                foreach (AreaNode node in m.V)
                    sw.WriteLine(string.Join(",", node.Adjacents.OrderBy(x => x.ID).Select(x => x.ID.ToString()).ToArray()));
            Console.WriteLine($"Adjacency list written to {output + @"maplist.csv"}");

            using (StreamWriter sw = new StreamWriter(output + @"mappop.csv"))
                foreach (AreaNode node in m.V)
                    sw.WriteLine(node.Areas[0].Results.Osszes);
            Console.WriteLine($"Population counts written to {output + @"mappop.csv"}");

            using (StreamWriter sw = new StreamWriter(output + @"initcds.csv"))
                foreach (AreaNode node in m.V)
                    sw.WriteLine(node.Areas[0].ElectoralDistrict);
            Console.WriteLine($"Electoral districts written to {output + @"initcds.csv"}");
        }
    }
}
