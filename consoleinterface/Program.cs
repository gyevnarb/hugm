using core;
using core.graph;
using core.map;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace consoleinterface
{
    class Program
    {
        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");

            Console.WriteLine($"The arguments passed were:\n{string.Join(',', args)}");

            string generation_type = "mcmc";
            if (args.Length > 0) generation_type = args[0];

            RunTestGen(generation_type);

            return;
        }

        private static void RunTestGen(string generation_type)
        {
            var gu = new GraphUtility();

            var doneEvent = new ManualResetEvent(false);
            gu.StartBatchedGeneration("out", generation_type, 1, 100, AreaUtils.Load("data/map.bin"), new RandomWalkParams()
            {
                excludeSelected = false,
                invert = false,
                method = SamplingMethod.UNIFORM,
                numRun = 100,
                party = Parties.FIDESZ,
                partyProb = 0,
                walkLen = 5
            }, 4, (s, e) => Console.WriteLine(e.ProgressPercentage), (s, e) => doneEvent.Set());

            if (generation_type != "mcmc") doneEvent.WaitOne();
            Console.WriteLine("Done");
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

        private static void RandomWalkTest()
        {
            Graph g = AreaUtils.Load("data/map.bin");
            RandomWalkSimulation simulation = new RandomWalkSimulation(g, SamplingMethod.UNIFORM, 5, 100, false, false);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            simulation.Simulate();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine($"Code run in {elapsedMs}");
            RandomWalkAnalysis analysis = new RandomWalkAnalysis(simulation, DistCalcMethod.OCCURENCE_CNT);
            Console.ReadLine();
        }
    }
}
