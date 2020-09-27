using hugm.map;
using System;
using System.Collections.Generic;
using System.Linq;
using NumSharp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Security.Policy;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace hugm.graph
{
    using SimulationRun = Dictionary<Node, List<RandomWalk>>;
    using SimulationDist = Dictionary<Node, List<double>>;

    /// <summary>
    /// Determines which sampling method to use for random walks
    /// </summary>
    public enum SamplingMethod
    {
        [Description("Uniform")]
        UNIFORM,
        [Description("Larger Distance")]
        DIST_LARGE,
        [Description("Smaller Distance")]
        DIST_SMALL,
        [Description("Larger Population")]
        POP_LARGE,
        [Description("Smaller Population")]
        POP_SMALL
    }

    /// <summary>
    /// Perform random walk simulation on a graph
    /// </summary>
    public class RandomWalkSimulation
    {
        /// <summary>
        /// The maximum length of each individual walk
        /// </summary>
        public int MaxWalkLength { get; set; }

        /// <summary>
        /// The number of walks to perform
        /// </summary>
        public int MaxRuns { get; set; }

        /// <summary>
        /// The sampling method to apply for walks
        /// </summary>
        public SamplingMethod Method { get; set; }
        
        /// <summary>
        /// The simulation run.
        /// </summary>
        public SimulationRun Simulation { get; set; }

        public Graph Graph { get; set; }

        /// <summary>
        /// Initialise a random walk simulation on a graph with given parameters
        /// </summary>
        /// <param name="graph">The graph to perform the simulation on</param>
        /// <param name="method">The sampling method to use at each walk</param>
        /// <param name="maxLen">The maximum length of a walk</param>
        /// <param name="maxRuns">The maximum number of walks to perform at each node</param>
        public RandomWalkSimulation(Graph graph, SamplingMethod method, int maxLen, int maxRuns)
        {
            Graph = graph;
            Method = method;
            MaxWalkLength = maxLen;
            MaxRuns = maxRuns;
        }

        /// <summary>
        /// Run the simulation
        /// </summary>
        /// <returns>Dictionary with keys as nodes and values as a list of RandomWalks</returns>
        public void Simulate()
        {
            var ret = new Dictionary<Node, List<RandomWalk>>();
            foreach (Node node in Graph.V)
            {
                var walks = new List<RandomWalk>();
                for (int i = 0; i < MaxRuns; i++)
                {
                    RandomWalk newWalk = new RandomWalk(node, MaxWalkLength, Method);
                    newWalk.Walk();
                    walks.Add(newWalk);
                }
                ret.Add(node, walks);

            }
            Simulation = ret;
        }
    }

    public class RandomWalkAnalysis
    {
        public RandomWalkSimulation Simulation { get; set; }
        public SimulationDist Distribution { get; private set; }

        public int NumElectoralDistricts { get; set; }

        public RandomWalkAnalysis(RandomWalkSimulation sim, int numDist = 18)
        {
            Simulation = sim;
            NumElectoralDistricts = numDist;
            CalculateDistribution();
        }

        /// <summary>
        /// Given a simulation, calculate the electoral distribution of nodes
        /// </summary>
        /// <returns>Dictionary with keys as node and a list of doubles corresponding to the distribution of electoral districts</returns>
        public void CalculateDistribution()
        {
            var ret = new SimulationDist();
            foreach (Node node in Simulation.Simulation.Keys)
            {
                var walks = Simulation.Simulation[node];
                var counts = Enumerable.Repeat(0, NumElectoralDistricts).ToList<int>();
                walks.ForEach(walk =>
                {
                    AreaNode last = walk.Path.Last.Value as AreaNode;
                    counts[last.ElectorialDistrict - 1] += 1;
                });

                var dist = counts.Select(x => (double)x / counts.Sum()).ToList();
                ret.Add(node, dist);
            }
            Distribution = ret;
        }

        /// <summary>
        /// Select most likely district for each area. Assume uniform prior for now. Where there is equal probability, select the first district occurence. 
        /// TODO: Mark as boundary
        /// </summary>
        /// <returns>Dictionary with keys as nodes and values ints that show the most likely district.</returns>
        public Dictionary<Node, int> MAPDistrict
        {
            get
            {
                var districts = new Dictionary<Node, int>();
                foreach (Node node in Distribution.Keys)
                {
                    var distribution = Distribution[node];
                    int district = distribution.Select((n, i) => (Number: n, Index: i)).Max().Index + 1;
                    districts.Add(node, district);
                }
                return districts;
            }
        }

        /// <summary>
        /// Calculate the expected (fractional) district of an area. Assume uniform prior for now.
        /// </summary>
        /// <returns></returns>
        public Dictionary<Node, double> ExpectedDistrict
        {
            get
            {
                var districts = new Dictionary<Node, double>();
                foreach (Node node in Distribution.Keys)
                {
                    var distribution = Distribution[node];
                    double district = 0.0;
                    for (int i = 0; i < distribution.Count; i++)
                        district += distribution[i] * (i + 1);
                    districts.Add(node, district);
                }
                return districts;
            }
        }      

        /// <summary>
        /// Compute the standard deviation.
        /// </summary>
        public Dictionary<Node, double> StandardDeviationDistrict
        {
            get
            {
                var expected = ExpectedDistrict;
                var stds = new Dictionary<Node, double>();
                foreach (Node node in Distribution.Keys)
                {
                    double mu = expected[node];
                    double std = Math.Sqrt(Distribution[node].Select((x, i) => x * Math.Pow(i + 1 - mu, 2)).Sum());
                    stds.Add(node, std);
                }
                return stds;
            }
        }
    }

    /// <summary>
    /// Represent a single random walk in a graph
    /// </summary>
    public class RandomWalk
    {
        private static Random r = new Random(1);

        public Node Start { get; set; }

        public int MaxLength { get; set; }

        public SamplingMethod Method { get; set; }

        public LinkedList<Node> Path { get; private set; }

        public RandomWalk(Node start, int maxLen, SamplingMethod method)
        {
            Start = start;
            MaxLength = maxLen;
            Method = method;

            Path = new LinkedList<Node>();
        }

        public void Walk()
        {
            Path.AddLast(Start);
            while (Path.Count < MaxLength)
            {
                Node current = Path.Last.Value;
                Node previous = Path.Last == Path.First ? Node.EmptyNode : Path.Last.Previous.Value;

                try
                {
                    Node next = Sample(current, previous);
                    Path.AddLast(next);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    break;
                }
            }
        }

        public Node Sample(Node node, Node previous = Node.EmptyNode)
        {
            var adjacents = new List<Node>(node.Adjacents);
            adjacents.Remove(previous);
            if (adjacents.Count == 0)
                throw new Exception($"No valid node is available for {node} with previous {previous}!");
            else if (adjacents.Count == 1)
                return adjacents[0];

            double x = node.X;
            double y = node.Y;

            switch (Method)
            {
                case SamplingMethod.UNIFORM:
                    return UniformSample(adjacents);
                case SamplingMethod.DIST_LARGE:
                    return DistanceSample(adjacents, x, y, false);
                case SamplingMethod.DIST_SMALL:
                    return DistanceSample(adjacents, x, y, true);
                case SamplingMethod.POP_LARGE:
                    return PopulationSample(adjacents, false);
                case SamplingMethod.POP_SMALL:
                    return PopulationSample(adjacents, true);
                default:
                    break;
            }
            return node;
        }
        private Node UniformSample(List<Node> adjacents)
        {
            return adjacents[r.Next(0, adjacents.Count)];
        }

        private Node DistanceSample(List<Node> adjacents, double x, double y, bool large = false)
        {
            var dists = adjacents.Select(node => Math.Sqrt(Math.Pow(x - node.X, 2) + Math.Pow(y - node.Y, 2)));
            var weights = Normalise(dists, large);
            return Sample<Node>(adjacents, weights);
        }

        private Node PopulationSample(List<Node> adjacents, bool large = false)
        {
            var pops = adjacents.Select(node => (double)(node as AreaNode).Population);
            var weights = Normalise(pops, large);
            return Sample<Node>(adjacents, weights);
        }

        private T Sample<T>(List<T> vals, List<double> dist)
        {
            if (vals.Count > dist.Count)
                throw new Exception("Distribution is incomplete for sampling!");
            if (dist[0] != 0.0)
                dist.Insert(0, 0.0);

            double crit = r.NextDouble();
            for (int i = 0; i < vals.Count; i++)
            {
                if (dist[i] <= crit && crit < dist[i + 1])
                    return vals[i];
            }
            throw new Exception("Couldn't sample from adjacents with population criteria!");

        }

        private List<double> Normalise(IEnumerable<double> vals, bool invert = false)
        {
            var total = vals.Sum();
            var normalised = new List<double>();
            if (invert)
            {
                normalised = vals.Select(x => (double)total / x).ToList();
                var normSum = normalised.Sum();
                normalised = normalised.Select(x => x / normSum).ToList();
            }
            else
            {
                normalised = vals.Select(x => (double)x / total).ToList();
            }
            return normalised;
        }

        public override string ToString()
        {
            return $"Count = {Path.Count}";
        }
    }
}
