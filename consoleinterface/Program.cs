using core.graph;
using core.map;
using System;
using System.Globalization;
using System.Linq;

namespace consoleinterface
{
    class Program
    {
        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");

            Graph g = AreaUtils.Load("data/map.bin");
            RandomWalkSimulation simulation = new RandomWalkSimulation(g, SamplingMethod.UNIFORM, 5, 100, false, false);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            simulation.Simulate();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine($"Code run in {elapsedMs}");
            RandomWalkAnalysis analysis = new RandomWalkAnalysis(simulation, DistCalcMethod.OCCURENCE_CNT);
            //File.WriteAllText("dist.json", analysis.Distribution.DistributionToJSON());
            //File.WriteAllText("MAP.json", analysis.MAPDistrict.ToJSON());
            //File.WriteAllText("expected.json", analysis.ExpectedDistrict.ToJSON());
            //File.WriteAllText("std.json", analysis.StandardDeviationDistrict.ToJSON());
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

        private static void DisplayElectoralConnectedComponents(Graph g)
        {
            foreach (var group in g.V.GroupBy(n => (n as AreaNode).Areas[0].ElectoralDistrict))
            {
                Graph m = new Graph(group.ToList());
                var c = m.GetConnectedComponents(true);
                Console.WriteLine($"{group.Key}: {c.Count}");
            }
        }

        private static void DispalyElectoralPopulations(Graph g)
        {
            int total = 0;
            int pre_pop = 0;
            foreach (var group in g.V.GroupBy(n => (n as AreaNode).Areas[0].ElectoralDistrict))
            {
                int pop = group.ToList().ConvertAll(x => x as AreaNode).Sum(x => x.Areas[0].Results.Osszes);
                Console.WriteLine($"{group.Key}: {pop} ({Math.Abs((double)pop - pre_pop) / Math.Max(pop, pre_pop) * 100:F3}%)");
                total += pop;
                pre_pop = pop;
            }
            Console.WriteLine($"Total: {total}");
        }

        private static double GetDistance(Node n1, Node n2)
        {
            return Math.Pow(n1.X - n2.X, 2) + Math.Pow(n1.Y - n2.Y, 2);
        }
    }
}
