using hugm.map;
using System;
using System.Collections.Generic;
using System.Linq;
using NumSharp;

namespace hugm.graph
{
    using SimulationRun = Dictionary<Node, List<RandomWalk>>;
    using SimulationDist = Dictionary<Node, List<double>>;

    /// <summary>
    /// Determines which sampling method to use for random walks
    /// </summary>
    public enum SamplingMethod
    {
        UNIFORM,
        DISTANCE,
        POPULATION,
        PREFER_PARTY,
        PREFER_OWN
    }

    public enum DistCalcMethod
    {
        OCCURENCE_CNT,
        LAST_ONLY
    }

    public enum PlotCalculationMethod
    {
        EXPECTED,
        MAP
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
        
        /// <summary>
        /// Which to favour during sampling
        /// </summary>
        public Parties PartyPreference { get; set; }

        /// <summary>
        /// The probability of selecting a node with dominant party given by PartyPreference
        /// </summary>
        public double PartyProbability { get; set; }

        /// <summary>
        /// Whether to exclude atjelentkezo nodes from simulation
        /// </summary>
        public bool ExcludeSelected { get; set; }

        /// <summary>
        /// Whether to invert the distribution of the sampling method
        /// </summary>
        public bool InvertSamplingDistribution { get; set; }

        public Graph Graph { get; set; }

        /// <summary>
        /// Initialise a random walk simulation on a graph with given parameters
        /// </summary>
        /// <param name="graph">The graph to perform the simulation on</param>
        /// <param name="method">The sampling method to use at each walk</param>
        /// <param name="maxLen">The maximum length of a walk</param>
        /// <param name="maxRuns">The maximum number of walks to perform at each node</param>
        public RandomWalkSimulation(Graph graph, SamplingMethod method, int maxLen, int maxRuns, bool excludeSelected, bool invert)
        {
            Graph = graph;
            Method = method;
            MaxWalkLength = maxLen;
            MaxRuns = maxRuns;
            ExcludeSelected = excludeSelected;
            InvertSamplingDistribution = invert;
        }

        /// <summary>
        /// Run the simulation
        /// </summary>
        /// <returns>Dictionary with keys as nodes and values as a list of RandomWalks</returns>
        public void Simulate()
        {
            var ret = new SimulationRun();
            foreach (Node node in Graph.V)
            {
                var walks = new List<RandomWalk>();
                for (int i = 0; i < MaxRuns; i++)
                {
                    RandomWalk newWalk = null;
                    if (Method == SamplingMethod.PREFER_PARTY)
                        newWalk = new RandomWalk(node, MaxWalkLength, Method, ExcludeSelected, InvertSamplingDistribution, PartyPreference, PartyProbability);
                    else
                        newWalk = new RandomWalk(node, MaxWalkLength, Method, ExcludeSelected, InvertSamplingDistribution);
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
        public DistCalcMethod DistributionCalcMethod { get; set; }

        public RandomWalkAnalysis(RandomWalkSimulation sim, DistCalcMethod method, int numDist = 18)
        {
            Simulation = sim;
            NumElectoralDistricts = numDist;
            DistributionCalcMethod = method;
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
                    switch (DistributionCalcMethod)
                    {
                        case DistCalcMethod.OCCURENCE_CNT:
                            var visited = walk.GetDistrictCounts();
                            foreach (var district in visited)
                                counts[district.Key - 1] += district.Value;
                            break;
                        case DistCalcMethod.LAST_ONLY:
                            AreaNode last = walk.Path.Last.Value as AreaNode;
                            counts[last.ElectorialDistrict - 1] += 1;
                            break;
                        default:
                            break;
                    }
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

        public List<(double, double)> NumWrongDistrict(PlotCalculationMethod method)
        {
            var rtn = new List<(double, double)>();
            switch (method)
            {
                case PlotCalculationMethod.EXPECTED:
                    for (int i = 0; i < NumElectoralDistricts; i++)
                    {
                        // Calculate mean and std of errors 
                        var filterByDistrict = Distribution.Where(kv => (kv.Key as AreaNode).ElectorialDistrict == i + 1);
                        var districtError = filterByDistrict.Select(kv => 1 - kv.Value[i]).ToList();
                        var muError = districtError.Sum() / districtError.Count;
                        var errorStd = Math.Sqrt(districtError.Select(x => Math.Pow(x - muError, 2) / districtError.Count).Sum());
                        var testerror = np.std(np.array(districtError));
                        rtn.Add((muError, errorStd));
                    }
                    break;
                case PlotCalculationMethod.MAP:
                    for (int i = 0; i < NumElectoralDistricts; i++)
                    {
                        var filterByDistrict = MAPDistrict.Where(kv => (kv.Key as AreaNode).ElectorialDistrict == i + 1).ToList();
                        var districtError = filterByDistrict.Where(kv => kv.Value != i + 1).ToList();
                        var vals = ((double)districtError.Count / filterByDistrict.Count, 0.0);
                        rtn.Add(vals);
                    }
                    break;
                default:
                    break;
            }
            return rtn;
        }

        public List<double> AverageDistributionForDistrict(int districtId)
        {
            var rtn = Enumerable.Repeat(0.0, NumElectoralDistricts).ToList();
            var filterByDistrict = Distribution.Where(kv => (kv.Key as AreaNode).ElectorialDistrict == districtId).ToList();
            foreach (var node in filterByDistrict)
            {
                var distForNode = node.Value;
                for (int i = 0; i < distForNode.Count; i++)
                {
                    rtn[i] += distForNode[i];
                }
            }
            return rtn.Select(x => x / filterByDistrict.Count).ToList();
        }
    }

    /// <summary>
    /// Represent a single random walk in a graph
    /// </summary>
    public class RandomWalk
    {
        private static Random r = new Random(1);
        private static Dictionary<(Node, List<Node>), List<double>> weightsCache = new Dictionary<(Node, List<Node>), List<double>>();
        private static SamplingMethod previousMethod;

        public Node Start { get; set; }
        public int MaxLength { get; set; }
        public SamplingMethod Method { get; set; }
        public LinkedList<Node> Path { get; private set; }
        public List<int> VisitedCount { get; private set; }
        public bool ExcludeSelected { get; set; }
        public bool Invert { get; set; }
        public Parties Party { get; set; }
        public double PartyProbability { get; set; }

        public RandomWalk(Node start, int maxLen, SamplingMethod method, bool excludeSelected, bool invert)
        {
            Start = start;
            MaxLength = maxLen;
            Method = method;
            ExcludeSelected = excludeSelected;
            Invert = invert;

            Path = new LinkedList<Node>();
        }

        public RandomWalk(Node start, int maxLen, SamplingMethod method, bool excludeSelected, bool invert, Parties party, double partyProb)
        {
            Start = start;
            MaxLength = maxLen;
            Method = method;
            ExcludeSelected = excludeSelected;
            Invert = invert;
            Party = party;
            PartyProbability = partyProb;

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
            AreaNode currentNode = node as AreaNode;
            AreaNode previousNode = previous as AreaNode;

            var adjacents = new List<Node>(node.Adjacents);
            adjacents.Remove(previous);
            if (ExcludeSelected)
                adjacents = adjacents.Where(n => !(n as AreaNode).Atjelentkezes).ToList();
            if (adjacents.Count == 0)
                throw new Exception($"No valid node is available for {node} with previous {previous}!");
            else if (adjacents.Count == 1)
                return adjacents[0];

            double x = node.X;
            double y = node.Y;
            
            // Assign probability to selected party and distribute the rest equally
            var w = new List<double> { 0, 0, 0, 0 };
            w[(int)Party] = PartyProbability;
            for (int i = 0; i < 4; i++)
                if (i != (int)Party)
                    w[i] = (1 - PartyProbability) / 3.0;

            if (previousMethod != Method)
                weightsCache.Clear();
            previousMethod = Method;

            List<double> weights = null;
            var key = (node, adjacents);
            if (weightsCache.ContainsKey(key))
                weights = weightsCache[key];

            if (weights == null)
            {
                IEnumerable<double> vals = null;
                switch (Method)
                {
                    case SamplingMethod.UNIFORM:
                        return UniformSample(adjacents);
                    case SamplingMethod.DISTANCE:
                        vals = adjacents.Select(n => Math.Sqrt(Math.Pow(x - n.X, 2) + Math.Pow(y - n.Y, 2)));
                        break;
                    case SamplingMethod.POPULATION:
                        vals = adjacents.Select(n => (double)(n as AreaNode).Population);
                        break;
                    case SamplingMethod.PREFER_PARTY:
                        var partiesVotes = adjacents.Select(n => (n as AreaNode).Results.PartiesVotes());
                        var partyPriors = partiesVotes.Select(dict => Normalise(dict.Select(kv => kv.Value)));
                        if (partyPriors.ToList()[0][0] < partyPriors.ToList()[0][1])
                            Console.WriteLine();
                        var posterior = partyPriors.Select(adj => Normalise(adj.Select((z, i) => z * w[i])));
                        vals = posterior.Select(adj => adj[(int)Party]);
                        break;
                    case SamplingMethod.PREFER_OWN:
                        var largest = currentNode.Results.Max();
                        vals = adjacents.Select(n => (n as AreaNode).Results.PartiesVotes()).Select(kv => (double)kv[largest.Key]);
                        break;
                    default:
                        break;
                }
                weights = Normalise(vals, Invert);
                weightsCache.Add(key, weights);
            }
            return Sample(adjacents, weights);
        }
        private Node UniformSample(List<Node> adjacents)
        {
            return adjacents[r.Next(0, adjacents.Count)];
        }

        private T Sample<T>(List<T> vals, List<double> dist)
        {
            if (vals.Count > dist.Count)
                throw new Exception("Distribution is incomplete for sampling!");
            if (dist[0] != 0.0)
                dist.Insert(0, 0.0);

            double crit = r.NextDouble();
            var cumDist = CumSum(dist);
            for (int i = 0; i < vals.Count; i++)
            {
                if (cumDist[i] <= crit && crit < cumDist[i + 1])
                    return vals[i];
            }
            throw new Exception("Couldn't sample from adjacents with population criteria!");

        }
        private List<double> Normalise(IEnumerable<int> vals, bool invert = false)
        {
            return Normalise(vals.Select(x => (double)x), invert);
        }

        private List<double> CumSum(IEnumerable<double> vals)
        {
            double sum = 0;
            return vals.Select(w => sum += w).ToList();
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

        /// <summary>
        /// Calculate the number of occurences for each district in the walk
        /// </summary>
        /// <returns>Dictionary with key as the district number and value as the number of occurences</returns>
        public Dictionary<int, int> GetDistrictCounts()
        {
            var rtn = new Dictionary<int, int>();
            foreach (var node in Path.Skip(1))
            {
                AreaNode an = node as AreaNode;
                if (rtn.ContainsKey(an.ElectorialDistrict))
                    rtn[an.ElectorialDistrict] += 1;
                else
                    rtn.Add(an.ElectorialDistrict, 1);
            }
            return rtn;
        }

        public override string ToString()
        {
            return $"Count = {Path.Count}";
        }
    }
}
