using System;
using hugm.graph;
using hugm.map;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;

namespace hugm
{
    class Program
    {
        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");

            Graph g = AreaUtils.Load("map.bin");
            //DisplayElectoralConnectedComponents(g);
            MapToData(g);
            //WriteNewPartitionGraph(g);
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

        private static void MapToData(Graph m)
        {
            Console.Write("Path to map: ");
            string map = Console.ReadLine();
            if (map == "") map = "map.bin";

            Console.Write("Write path: ");
            string output = Console.ReadLine();
            if (output == "") output = "../../";

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

            using (StreamWriter sw = new StreamWriter(output + @"votes.csv"))
            {
                sw.WriteLine("FideszKDNP,Osszefogas,Jobbik,LMP");
                foreach(AreaNode node in m.V)
                {
                    VoteResult r = node.Areas[0].Results;
                    sw.WriteLine($"{r.FideszKDNP},{r.Osszefogas},{r.Jobbik},{r.LMP}");
                }
            }
            Console.WriteLine($"Electoral districts results written to {output + @"votes.csv"}");

            using (StreamWriter sw = new StreamWriter(output + "ssdist.csv"))         
            {
                for (int i = 0; i < m.V.Count; i++)
                {
                    double dist = GetDistance(m.V[i], m.V[0]);
                    sw.Write($"{dist:F2}");

                    for (int j = 1; j < m.V.Count; j++)
                    {
                        dist = GetDistance(m.V[i], m.V[j]);
                        sw.Write($";{dist:F2}");
                    }
                    sw.Write("\n");
                }
            }
        }

        private static void DisplayElectoralConnectedComponents(Graph g)
        {
            foreach (var group in g.V.GroupBy(n => (n as AreaNode).Areas[0].ElectoralDistrict))
            {
                Graph m = new Graph(group.ToList());
                var c = m.GetConnectedComponents(true);
                Console.WriteLine($"{group.Key}: {c.Count}");
            }
        }

        private static void WriteNewPartitionGraph(Graph g)
        {
            Console.WriteLine("Writing new assignment");
            Random r = new Random();
            var file = File.ReadAllLines(@"..\..\..\visualizer\data\partitions.csv").Select(x => x.Split(',')).Select(x => x.Select(y => int.Parse(y) + 1).ToList()).ToList();
            var assignment = file[r.Next(file.Count)];
            int i = 0;
            foreach (AreaNode vert in g.V)
            {
                vert.Areas[0].ElectoralDistrict = assignment[i];
                i++;
            }
            AreaUtils.Save("../../map_new.bin", g);
            Console.WriteLine("Created new electoral assignment under file map_new.bin");
        }

        private static double GetDistance(Node n1, Node n2)
        {
            return Math.Pow(n1.X - n2.X, 2) + Math.Pow(n1.Y - n2.Y, 2);
        }
    }
}
