using hugm.map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace hugm.graph
{
    using SimulationRun = Dictionary<Node, List<RandomWalk>>;
    using SimulationDist = Dictionary<Node, List<double>>;

    /// <summary>
    /// Determines which sampling method to use for random walks
    /// </summary>
    enum SamplingMethod
    {
        UNIFORM
    }

    /// <summary>
    /// Perform random walk simulation on a graph
    /// </summary>
    class RandomWalkSimulation
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
        public SimulationRun Simulation { get; private set; }

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

        /// <summary>
        /// Given a simulation, calculate the electoral distribution of nodes
        /// </summary>
        /// <returns>Dictionary with keys as node and a list of doubles corresponding to the distribution of electoral districts</returns>
        public SimulationDist CalculateDistribution(int numElectoralDistricts)
        {
            var ret = new SimulationDist();
            foreach (Node node in Simulation.Keys)
            {
                var walks = Simulation[node];
                var counts = Enumerable.Repeat(0, numElectoralDistricts).ToList<int>();
                walks.ForEach(walk =>
                {
                    AreaNode last = walk.Path.Last.Value as AreaNode;
                    counts[last.ElectorialDistrict - 1] += 1;
                });

                var dist = counts.Select(x => (double)x / counts.Sum()).ToList();
                ret.Add(node, dist);
            }
            return ret;
        }
    }

    /// <summary>
    /// Represent a single random walk in a graph
    /// </summary>
    class RandomWalk
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
            switch (Method)
            {
                case SamplingMethod.UNIFORM:
                    return UniformSample(node, previous);
                default:
                    break;
            }
            return node;
        }
        private Node UniformSample(Node node, Node previous = Node.EmptyNode)
        {
            var adjacents = new List<Node>(node.Adjacents);
            adjacents.Remove(previous);
            if (adjacents.Count == 0)
                throw new Exception($"No valid node is available for {node} with previous {previous}!");

            return adjacents[r.Next(0, adjacents.Count)];
        }

        public override string ToString()
        {
            return $"Count = {Path.Count}";
        }
    }
}
