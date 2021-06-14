using core.graph;
using core.map;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace core
{
    public static class EXT
    {
        public static T MaxObject<T, U>(this IEnumerable<T> source, Func<T, U> selector)
            where U : IComparable<U>
        {
            if (source == null) throw new ArgumentNullException("source");
            bool first = true;
            T maxObj = default(T);
            U maxKey = default(U);
            foreach (var item in source)
            {
                if (first)
                {
                    maxObj = item;
                    maxKey = selector(maxObj);
                    first = false;
                }
                else
                {
                    U currentKey = selector(item);
                    if (currentKey.CompareTo(maxKey) > 0)
                    {
                        maxKey = currentKey;
                        maxObj = item;
                    }
                }
            }
            if (first) throw new InvalidOperationException("Sequence is empty.");
            return maxObj;
        }
    }

    public delegate void DrawGraphEventHandler(object sender, DrawGraphEventArgs e);

    public class DrawGraphEventArgs : EventArgs { }

    public class ElectoralDistrict
    {
        public List<AreaNode> nodes;
        public HashSet<AreaNode> availableNodes;
        public int pop = 0;
        public int id;
    }

    public struct ElectDistrictResult
    {
        public int[] numVoters;
        public double[] fResults;
        public int megjelent;
        public int osszes;
        public int winner;
        public double wrongDistrictPercentage;
    }

    struct StatWritingStream : IDisposable
    {
        public Stream compressor, compressed;

        public void Dispose()
        {
            compressor?.Dispose();
            compressed?.Dispose();
        }
    }

    public class GenerationResult
    {
        public List<ElectDistrictResult> result = new List<ElectDistrictResult>();
        public int budapestWinner;
        public int seed;
        public double similarity;
        public int athelyezesCount;
    }

    public class Stats
    {
        public GenerationResult baseResult = new GenerationResult();
        public List<GenerationResult> generationResults = new List<GenerationResult>();
    }

    public struct RandomWalkParams
    {
        public int numRun;
        public int walkLen;
        public SamplingMethod method;
        public Parties party;
        public double partyProb;
        public bool excludeSelected;
        public bool invert;
    }

    public class BatchedGenerationProgress
    {
        public int done, all;
    }

    public class GraphUtility
    {

        BackgroundWorker bgw;

        private static float atlag = 76818; // 2011
        private static readonly double STAT_VERSION = 0.6;

        private int _seed;
        private Graph myGraph;
        private double similarity = 1.0;
        public List<int> origElectoralSettings = new List<int>();

        public Graph MyGraph
        {
            get { return myGraph; }
            private set
            {
                if (value != null)
                {
                    myGraph = value;
                }
                else
                {
                    Console.WriteLine("Null graph!");
                }
            }
        }

        public RandomWalkAnalysis PreviousRandomWalk { get; set; }

        public RUtils rutil = new RUtils();

        public bool ValidGraph()
        {
            return MyGraph != null;
        }

        public Stats MyStats { get; set; }

        public async Task<int> GenerateRandomElectoralDistrictSystem(long seed, Graph graph, Func<AreaNode, Task> nodeUpdatedHandler)
        {
            // 1. 18 Random node kivalasztasa, minden keruletbol egyet
            // 2. Novesztes egyelore nepesseg korlat betartasa nelkul
            // 3. Tul kicsiket felnoveljuk hogy elerjek a hatart
            // 4. Túl nagyokat meg lecsokkentjuk
            if (graph == null) return -1;
            int MAX_STEP = 10000; // Ha 3. vagy 4. lepes egyenként tul lepne a max step-et akkor megallitjuk

            _seed = (int)(seed % int.MaxValue);
            var rng = new Random(_seed);
            foreach (var v in graph.V) v.Marked = false;
            List<AreaNode> points = new List<AreaNode>();
            for (int i = 1; i <= 18; ++i)
            {
                var ns = graph.V.Where(x =>
                {
                    return (x as AreaNode).Areas[0].ElectoralDistrict == i;
                }).ToList();
                var p = ns[rng.Next(ns.Count)];
                points.Add(p as AreaNode);
            }

            var ujlista = new List<ElectoralDistrict>();
            foreach (var p in points)
            {
                p.Marked = true;
                p.ElectorialDistrict = p.Areas[0].ElectoralDistrict;
                ujlista.Add(new ElectoralDistrict
                {
                    nodes = new List<AreaNode> { p },
                    availableNodes = new HashSet<AreaNode>(p.Adjacents.Select(x => x as AreaNode)),
                    pop = p.Population,
                    id = p.ElectorialDistrict
                });
                if (nodeUpdatedHandler != null)
                    await nodeUpdatedHandler?.Invoke(p);
            }

            // TODO: +-20 at is figylemebe lehetne venni, akkora hiba torveny szerint meg belefer
            int z = 0;
            while (z < graph.V.Count - 18)
            {
                for (int i = 0; i < 18; ++i)
                {
                    var adnonamrked0 = ujlista[i].availableNodes.Where(x => !x.Marked).ToList();
                    if (adnonamrked0.Count != 0)
                    {
                        int j = rng.Next(adnonamrked0.Count);
                        var chosenNode = adnonamrked0[j];

                        chosenNode.Marked = true;
                        ujlista[i].availableNodes.UnionWith(chosenNode.Adjacents.Select(x => x as AreaNode));
                        ujlista[i].availableNodes.Remove(chosenNode);
                        ujlista[i].nodes.Add(chosenNode);
                        ujlista[i].pop += chosenNode.Population;
                        chosenNode.ElectorialDistrict = ujlista[i].id;
                        z++;

                        if (nodeUpdatedHandler != null)
                            await nodeUpdatedHandler?.Invoke(chosenNode);
                    }
                }
            }

            int athelyezesCount = 0;

            int l = 0;
            int h = 0;
            ujlista.Sort((a, b) => a.pop - b.pop);
            for (int k = 0; k < 18; ++k) if (ujlista[k].pop > atlag * 0.85) { h = k; break; }
            while (ujlista[0].pop < atlag * 0.85 && l < MAX_STEP)
            {
                bool done = false;
                for (int i = 0; i < h && !done; ++i)
                {
                    for (int j = 17; j >= h && !done; --j)
                    {
                        int id = ujlista[j].id;
                        AreaNode n = null;
                        foreach (var v in ujlista[i].availableNodes)
                        {
                            if (v.ElectorialDistrict == id && !graph.IsCuttingNode(v))
                            {
                                n = v;
                                break;
                            }
                        }
                        if (n != null)
                        {
                            ujlista[i].availableNodes.UnionWith(n.Adjacents.Select(x => x as AreaNode));
                            ujlista[i].availableNodes.Remove(n);

                            ujlista[j].availableNodes.Clear();
                            ujlista[j].nodes.Remove(n);
                            foreach (var v in ujlista[j].nodes)
                            {
                                ujlista[j].availableNodes.UnionWith(v.Adjacents.Select(x => x as AreaNode));
                            }
                            ujlista[i].nodes.Add(n);
                            ujlista[i].pop += n.Population;
                            ujlista[j].pop -= n.Population;
                            n.ElectorialDistrict = ujlista[i].id;
                            done = true;

                            athelyezesCount++;

                            if (nodeUpdatedHandler != null)
                                await nodeUpdatedHandler?.Invoke(n);
                        }
                        l++;
                    }
                }
                ujlista.Sort((a, b) => a.pop - b.pop);
                for (int k = 0; k < 18; ++k) if (ujlista[k].pop > atlag * 0.85) { h = k; break; }
            }

            l = 0;
            ujlista.Sort((a, b) => b.pop - a.pop);
            for (int k = 0; k < 18; ++k) if (ujlista[k].pop < atlag * 1.15) { h = k; break; }
            while (ujlista[0].pop > atlag * 1.15 && l < MAX_STEP)
            {
                bool done = false;
                for (int i = 0; i < h && !done; ++i)
                {
                    for (int j = 17; j >= h && !done; --j)
                    {
                        int id = ujlista[i].id;
                        AreaNode n = null;
                        foreach (var v in ujlista[j].availableNodes)
                        {
                            if (v.ElectorialDistrict == id && !graph.IsCuttingNode(v))
                            {
                                n = v;
                                break;
                            }
                        }
                        if (n != null)
                        {
                            ujlista[j].nodes.Add(n);
                            ujlista[j].availableNodes.UnionWith(n.Adjacents.Select(x => x as AreaNode));
                            ujlista[j].availableNodes.Remove(n);
                            ujlista[j].pop += n.Population;

                            ujlista[i].availableNodes.Clear();
                            ujlista[i].nodes.Remove(n);
                            foreach (var v in ujlista[i].nodes)
                            {
                                ujlista[i].availableNodes.UnionWith(v.Adjacents.Select(x => x as AreaNode));
                            }
                            ujlista[i].pop -= n.Population;

                            n.ElectorialDistrict = ujlista[j].id;
                            done = true;

                            athelyezesCount++;

                            if (nodeUpdatedHandler != null)
                                await nodeUpdatedHandler?.Invoke(n);
                        }
                        l++;
                    }
                }
                ujlista.Sort((a, b) => b.pop - a.pop);
                for (int k = 0; k < 18; ++k) if (ujlista[k].pop < atlag * 1.15) { h = k; break; }
            }

            return athelyezesCount;
        }

        public async Task GenerateRandomElectoralDistrictSystem2(long seed, Graph graph, Func<AreaNode, Task> nodeUpdatedHandler)
        {
            // 1. 18 Random node kivalasztasa, minden keruletbol egyet
            // 2. Novesztes népesség alapján
            if (graph == null) return;
            int MAX_STEP = 10000; // Ha 3. vagy 4. lepes egyenként tul lepne a max step-et akkor megallitjuk

            _seed = (int)(seed % int.MaxValue);
            var rng = new Random(_seed);

            foreach (var v in graph.V) v.Marked = false;

            List<AreaNode> points = new List<AreaNode>();
            List<bool> alreadyMoved = new List<bool>(); alreadyMoved.AddRange(Enumerable.Repeat(false, graph.V.Count));
            List<bool> alreadySelected = new List<bool>(); alreadySelected.AddRange(Enumerable.Repeat(false, graph.V.Count));
            var ujlista = new List<ElectoralDistrict>();

            for (int i = 1; i <= 18; ++i)
            {
                var ns = graph.V.Where(x =>
                {
                    return (x as AreaNode).Areas[0].ElectoralDistrict == i;
                }).ToList();
                var p = ns[rng.Next(ns.Count)] as AreaNode;

                alreadySelected[p.ID] = true;
                p.ElectorialDistrict = p.Areas[0].ElectoralDistrict;
                ujlista.Add(new ElectoralDistrict
                {
                    nodes = new List<AreaNode> { p },
                    availableNodes = new HashSet<AreaNode>(p.Adjacents.Select(x => x as AreaNode)),
                    pop = p.Population,
                    id = p.ElectorialDistrict
                });

                alreadyMoved[p.ID] = true;

                points.Add(p);

                await nodeUpdatedHandler?.Invoke(p);
            }

            ujlista.Sort((a, b) => a.pop - b.pop);
            int z = 0, w = 0;
            while (z < graph.V.Count - 18 || ((ujlista.First().pop < 0.85 * atlag || ujlista.Last().pop > 1.15 * atlag) && w < MAX_STEP))
            {
                var s = ujlista[0];

                var adnonamrked0 = s.availableNodes.Where(x => !alreadySelected[x.ID]).ToList();
                if (adnonamrked0.Count != 0) // van meg szabad area
                {
                    int j = rng.Next(adnonamrked0.Count);
                    var chosenNode = adnonamrked0[j];

                    alreadySelected[chosenNode.ID] = true;
                    s.availableNodes.UnionWith(chosenNode.Adjacents.Select(x => x as AreaNode));
                    s.availableNodes.Remove(chosenNode);
                    s.nodes.Add(chosenNode);
                    s.pop += chosenNode.Population;
                    chosenNode.ElectorialDistrict = s.id;
                    z++;

                    await nodeUpdatedHandler?.Invoke(chosenNode);
                }
                else // elfogytak a szabad area-k -> elvesszuk masoket
                {
                    // ezek amiket meg nem mozgattunk és nem rontjak el az osszefuggoseget ha athelyezzuk
                    var admarked = s.availableNodes.Where(x => !alreadyMoved[x.ID] && !graph.IsCuttingNode(x)).ToList();
                    if (admarked.Count == 0)
                    {
                        w = MAX_STEP;
                        continue;
                    }

                    // TODO: ezt lehetne veletlenszeruen, vagy "szepseg" figyelembe vetelevel
                    int besti = 0;
                    for (int i = 1; i < admarked.Count; ++i)
                    {
                        var best = admarked[besti];
                        var act = admarked[i];

                        if (ujlista.First(x => x.id == act.ElectorialDistrict).pop > ujlista.First(x => x.id == best.ElectorialDistrict).pop) besti = i;
                        else if (ujlista.First(x => x.id == act.ElectorialDistrict).pop == ujlista.First(x => x.id == best.ElectorialDistrict).pop)
                        {
                            if (act.Population > best.Population) besti = i;
                        }
                    }

                    var chosenNode = admarked[besti];
                    var t = ujlista.First(x => x.id == chosenNode.ElectorialDistrict);

                    s.availableNodes.UnionWith(chosenNode.Adjacents.Select(x => x as AreaNode));
                    s.availableNodes.Remove(chosenNode);

                    t.availableNodes.Clear();
                    t.nodes.Remove(chosenNode);
                    foreach (var v in t.nodes)
                    {
                        t.availableNodes.UnionWith(v.Adjacents.Select(x => x as AreaNode));
                    }
                    s.nodes.Add(chosenNode);
                    s.pop += chosenNode.Population;
                    t.pop -= chosenNode.Population;
                    chosenNode.ElectorialDistrict = s.id;

                    alreadyMoved[chosenNode.ID] = true;
                    w++;

                    await nodeUpdatedHandler?.Invoke(chosenNode);
                }

                ujlista.Sort((a, b) => a.pop - b.pop);
            }
        }

        public void Load(Graph graph)
        {
            if (graph != null)
            {
                MyGraph = graph;
                origElectoralSettings.Clear();
                foreach (AreaNode v in MyGraph.V) origElectoralSettings.Add(v.ElectorialDistrict);
            }
        }

        private StatWritingStream PrepareStatWriting()
        {
            var mem = new MemoryStream();
            return new StatWritingStream
            {
                compressor = new BrotliStream(mem, CompressionLevel.Fastest),
                compressed = mem
            };
        }

        public string ToStatString(Graph graph, RandomWalkParams rwp, int athelyezesCount)
        {
            if (graph == null) return "";

            // random walk
            var rr = new RandomWalkSimulation(graph, rwp.method, rwp.walkLen, rwp.numRun, rwp.excludeSelected, rwp.invert);
            if (rwp.method == SamplingMethod.PREFER_PARTY)
            {
                rr.PartyPreference = rwp.party;
                rr.PartyProbability = rwp.partyProb;
            }
            rr.Simulate();
            var ra = new RandomWalkAnalysis(rr, DistCalcMethod.OCCURENCE_CNT, 18);
            var wrongDistrictNum = ra.NumWrongDistrict(PlotCalculationMethod.MAP);

            string text = $"{STAT_VERSION};{_seed};{athelyezesCount}";
            for (int i = 0; i < 18; ++i)
            {
                var vn = wrongDistrictNum[i].Item1;
                text += $";{vn}";
            }

            foreach (AreaNode v in graph.V)
            {
                text += $";{v.ElectorialDistrict}";
            }
            text += "\n";
            return text;
        }

        public async Task StreamStat(Stream s, string stat)
        {
            await s.WriteAsync(System.Text.Encoding.ASCII.GetBytes(stat));
        }

        public async Task FinishStatWriting(string filename, Stream s)
        {
            var file = new FileStream(filename, FileMode.Create, FileAccess.Write);
            s.Position = 0;
            await s.CopyToAsync(file);
            await file.FlushAsync();
        }

        public async Task<Stats> ReadStat(Graph originalGraph, string folder, bool useValid)
        {
            if (!Directory.Exists(folder)) return null;

            Stats s = new Stats();

            foreach (var file in Directory.GetFiles(folder))
            {
                if (!file.EndsWith(".stat")) continue;

                byte[] decompressedBytes;
                using (var decompressorStream = new BrotliStream(new FileStream(file, FileMode.Open, FileAccess.Read), CompressionMode.Decompress))
                {
                    using (var decompressedStream = new MemoryStream())
                    {
                        await decompressorStream.CopyToAsync(decompressedStream);

                        decompressedBytes = decompressedStream.ToArray();
                    }
                }
                var stringStream = new StringReader(Encoding.ASCII.GetString(decompressedBytes));

                string text = null;
                while ((text = stringStream.ReadLine()) != null)
                {
                    if (text == "") continue;

                    var result = new GenerationResult();
                    var splitted = text.Split(';');

                    var ver = splitted[0];
                    if (double.Parse(ver) != STAT_VERSION) throw new Exception($"bad stats version, expected {STAT_VERSION}, received {ver}");

                    result.seed = int.Parse(splitted[1]);
                    result.athelyezesCount = int.Parse(splitted[2]);

                    List<VoteResult> results = new List<VoteResult>(18);
                    for (int i = 0; i < 18; ++i) results.Add(new VoteResult());
                    VoteResult glob = new VoteResult();

                    int diffs = 0;
                    for (int i = 0; i < originalGraph.V.Count; ++i)
                    {
                        int district = int.Parse(splitted[21 + i]);
                        if ((originalGraph.V[i] as AreaNode).ElectorialDistrict != district) diffs++;
                        foreach (var a in (originalGraph.V[i] as AreaNode).Areas)
                        {
                            results[district - 1].Add(a.Results);
                            glob.Add(a.Results);
                        }
                    }
                    result.similarity = 1.0 - (double)diffs / (double)originalGraph.V.Count;

                    for (int i = 0; i < 18; ++i)
                    {
                        var eres = new ElectDistrictResult();
                        eres.wrongDistrictPercentage = double.Parse(splitted[3 + i]);
                        eres.numVoters = new int[4] {
                            results[i].FideszKDNP,
                            results[i].Osszefogas,
                            results[i].Jobbik,
                            results[i].LMP
                        };
                        eres.megjelent = results[i].Megjelent;
                        eres.osszes = results[i].Osszes;

                        eres.fResults = new double[4] {  (double)eres.numVoters[0] / eres.megjelent,
                                                         (double)eres.numVoters[1] / eres.megjelent,
                                                         (double)eres.numVoters[2] / eres.megjelent,
                                                         (double)eres.numVoters[3] / eres.megjelent};

                        eres.winner = 0; // maximum index a winnerbe
                        for (int j = 0; j < eres.fResults.Length; ++j)
                        {
                            if (eres.fResults[j] > eres.fResults[eres.winner])
                            {
                                eres.winner = j;
                            }
                        }
                        result.result.Add(eres);
                    }

                    bool valid15 = true;
                    bool valid20 = true;
                    foreach (var se in results)
                    {
                        if (se.Osszes > atlag * 1.15 || se.Osszes < atlag * 0.85)
                            valid15 = false;
                        if (se.Osszes > atlag * 1.2 || se.Osszes < atlag * 0.8)
                            valid20 = false;
                    }

                    // TODO: interface to decide 15 20 or none
                    if (useValid && (!valid15 || !valid20)) continue;                    

                    float fidesz = results.Count(x => x.Gyoztes == "FideszKDNP");
                    float osszefogas = results.Count(x => x.Gyoztes == "Osszefogas");
                    float jobbik = results.Count(x => x.Gyoztes == "Jobbik");
                    float lmp = results.Count(x => x.Gyoztes == "LMP");

                    // TODO: egyenloseg?
                    if (fidesz >= osszefogas && fidesz >= jobbik && fidesz >= lmp)
                        result.budapestWinner = 0;
                    else if (osszefogas >= fidesz && osszefogas >= jobbik && osszefogas >= lmp)
                        result.budapestWinner = 1;
                    else if (jobbik >= fidesz && jobbik >= osszefogas && jobbik >= lmp)
                        result.budapestWinner = 2;
                    else if (lmp >= fidesz && lmp >= jobbik && lmp >= osszefogas)
                        result.budapestWinner = 3;

                    if (file.Contains("base.stat"))
                    {
                        s.baseResult = result;
                    }
                    else
                    {
                        s.generationResults.Add(result);
                    }
                }
            }
            return s;
        }

        public async Task SaveAsStat(string filename, Graph g, RandomWalkParams rwp, int athelyezesCount)
        {
            using (var s = PrepareStatWriting())
            {
                await StreamStat(s.compressor, ToStatString(g, rwp, athelyezesCount));
                await FinishStatWriting(filename, s.compressed);
            }
        }

        public void LoadStats(string folder, bool useValid)
        {
            if (!Directory.Exists(folder)) return;

            Stats s = new Stats();
            foreach (var file in Directory.GetFiles(folder))
            {
                if (!file.EndsWith(".stat")) continue;
                var fileStream = new System.IO.StreamReader(file);
                string text;
                while ((text = fileStream.ReadLine()) != null)
                {
                    if (text == "") continue;

                    var result = new GenerationResult();
                    var splitted = text.Split(';');

                    var ver = splitted[0];
                    if (double.Parse(ver) != STAT_VERSION) throw new Exception($"bad stats version, expected {STAT_VERSION}, received {ver}");

                    result.seed = int.Parse(splitted[1]);

                    bool valid15 = int.Parse(splitted[2]) == 1;
                    bool valid20 = int.Parse(splitted[3]) == 1;
                    if (useValid && (!valid15 || !valid20)) continue;

                    result.athelyezesCount = int.Parse(splitted[4]);

                    var fidesz = double.Parse(splitted[5]);
                    var osszefogas = double.Parse(splitted[6]);
                    var jobbik = double.Parse(splitted[7]);
                    var lmp = double.Parse(splitted[8]);

                    result.similarity = double.Parse(splitted[9]);

                    // TODO: egyenloseg?
                    if (fidesz >= osszefogas && fidesz >= jobbik && fidesz >= lmp)
                        result.budapestWinner = 0;
                    else if (osszefogas >= fidesz && osszefogas >= jobbik && osszefogas >= lmp)
                        result.budapestWinner = 1;
                    else if (jobbik >= fidesz && jobbik >= osszefogas && jobbik >= lmp)
                        result.budapestWinner = 2;
                    else if (lmp >= fidesz && lmp >= jobbik && lmp >= osszefogas)
                        result.budapestWinner = 3;

                    int offset = 10, stride = 8;
                    for (int i = 0; i < 18; ++i)
                    {
                        var eres = new ElectDistrictResult();
                        eres.wrongDistrictPercentage = double.Parse(splitted[offset + stride * i + 1]);
                        eres.numVoters = new int[4] {  int.Parse(splitted[offset + stride * i + 2]),
                                                        int.Parse(splitted[offset + stride * i + 3]),
                                                        int.Parse(splitted[offset + stride * i + 4]),
                                                        int.Parse(splitted[offset + stride * i + 5])};
                        eres.megjelent = int.Parse(splitted[offset + stride * i + 6]);
                        eres.osszes = int.Parse(splitted[offset + stride * i + 7]);

                        eres.fResults = new double[4] {  (double)eres.numVoters[0] / eres.megjelent,
                                                         (double)eres.numVoters[1] / eres.megjelent,
                                                         (double)eres.numVoters[2] / eres.megjelent,
                                                         (double)eres.numVoters[3] / eres.megjelent};


                        eres.winner = 0; // maximum index a winnerbe
                        for (int j = 0; j < eres.fResults.Length; ++j)
                        {
                            if (eres.fResults[j] > eres.fResults[eres.winner])
                            {
                                eres.winner = j;
                            }
                        }
                        result.result.Add(eres);
                    }

                    if (file.Contains("base.stat"))
                    {
                        s.baseResult = result;
                    }
                    else
                    {
                        s.generationResults.Add(result);
                    }
                }
            }
            MyStats = s;
        }

        public string ToStat(Graph graph, List<int> origElectSettings, RandomWalkParams rwp, int athelyezesCount)
        {
            if (graph == null) return "";

            List<VoteResult> results = new List<VoteResult>(18);
            for (int i = 0; i < 18; ++i) results.Add(new VoteResult());
            VoteResult glob = new VoteResult();

            int diffs = 0;
            for (int i = 0; i < origElectSettings.Count; ++i)
            {
                if ((graph.V[i] as AreaNode).ElectorialDistrict != origElectSettings[i]) diffs++;
            }
            similarity = 1.0 - (double)diffs / (double)origElectSettings.Count;

            foreach (var n in graph.V)
            {
                foreach (var a in (n as AreaNode).Areas)
                {
                    results[a.ElectoralDistrict - 1].Add(a.Results);
                    glob.Add(a.Results);
                }
            }

            int valid15 = 1;
            int valid20 = 1;
            foreach (var se in results)
            {
                if (se.Osszes > atlag * 1.15 || se.Osszes < atlag * 0.85)
                    valid15 = 0;
                if (se.Osszes > atlag * 1.2 || se.Osszes < atlag * 0.8)
                    valid20 = 0;
            }

            float fideszkdnp = results.Count(x => x.Gyoztes == "FideszKDNP");
            float osszefogas = results.Count(x => x.Gyoztes == "Osszefogas");
            float jobbik = results.Count(x => x.Gyoztes == "Jobbik");
            float lmp = results.Count(x => x.Gyoztes == "LMP");

            // random walk
            var rr = new RandomWalkSimulation(graph, rwp.method, rwp.walkLen, rwp.numRun, rwp.excludeSelected, rwp.invert);
            if (rwp.method == SamplingMethod.PREFER_PARTY)
            {
                rr.PartyPreference = rwp.party;
                rr.PartyProbability = rwp.partyProb;
            }
            rr.Simulate();
            var ra = new RandomWalkAnalysis(rr, DistCalcMethod.OCCURENCE_CNT, 18);
            var wrongDistrictNum = ra.NumWrongDistrict(PlotCalculationMethod.MAP);

            string text = $"{STAT_VERSION};{_seed};{valid15};{valid20};{athelyezesCount};{fideszkdnp};{osszefogas};{jobbik};{lmp};{similarity}";
            for (int i = 0; i < 18; ++i)
            {
                var se = results[i];
                var vn = wrongDistrictNum[i].Item1;

                text += $";{se.Gyoztes};{vn};{se.FideszKDNP};{se.Osszefogas};{se.Jobbik};{se.LMP};{se.Megjelent};{se.Osszes}";
            }
            return text;
        }

        public void SaveAsStat(string filename, Graph graph, List<int> origElectSettings, RandomWalkParams rwp, int athelyezesCount)
        {
            //System.IO.File.WriteAllText(filename, ToStat(graph, origElectSettings, rwp, athelyezesCount));
        }

        public string GetStatistics(Graph graph)
        {
            Dictionary<int, VoteResult> results = new Dictionary<int, VoteResult>();
            VoteResult glob = new VoteResult();

            int diffs = 0;
            for (int i = 0; i < origElectoralSettings.Count; ++i)
            {
                if ((graph.V[i] as AreaNode).ElectorialDistrict != origElectoralSettings[i]) diffs++;
            }
            similarity = 1.0 - (double)diffs / (double)origElectoralSettings.Count;

            foreach (var n in graph.V)
            {
                foreach (var a in (n as AreaNode).Areas)
                {
                    if (!results.ContainsKey(a.ElectoralDistrict)) results.Add(a.ElectoralDistrict, a.Results.Clone());
                    else results[a.ElectoralDistrict].Add(a.Results);
                    glob.Add(a.Results);
                }
            }

            string sss = "";
            bool valid15 = true;
            bool valid20 = true;
            foreach (var se in results)
            {
                sss += se.Key.ToString() + ": " + se.Value.Gyoztes + '\n';
                if (se.Value.Osszes > atlag * 1.15 || se.Value.Osszes < atlag * 0.85)
                    valid15 = false;
                if (se.Value.Osszes > atlag * 1.2 || se.Value.Osszes < atlag * 0.8)
                    valid20 = false;
            }

            float fideszkdnp = results.Count(x => x.Value.Gyoztes == "FideszKDNP");
            float osszefogas = results.Count(x => x.Value.Gyoztes == "Osszefogas");
            float jobbik = results.Count(x => x.Value.Gyoztes == "Jobbik");
            float lmp = results.Count(x => x.Value.Gyoztes == "LMP");
            float sumsum = fideszkdnp + osszefogas + jobbik + lmp;

            float sum = glob.FideszKDNP + glob.LMP + glob.Jobbik + glob.Osszefogas;
            sss += "\n Tenyleges:\n";
            sss += $"FideszKDNP: {fideszkdnp / sumsum}, Osszefogas: {osszefogas / sumsum}, Jobbik: {jobbik / sumsum}, LMP: {lmp / sumsum}\n";
            sss += "Kellett volna:\n";
            sss += $"FideszKDNP: {(float)glob.FideszKDNP / sum}, Osszefogas: {(float)glob.Osszefogas / sum}, Jobbik: {(float)glob.Jobbik / sum}, LMP: {(float)glob.LMP / sum}\n";
            sss += $"Valid 15: {valid15}, Valid 20: {valid20}\n";
            sss += $"Similarity: {similarity}";

            return sss;
        }

        public void StartBatchedGeneration(string folder, string generation_type, int startSeed, int count, Graph originalGraph, RandomWalkParams rwp, int maxParalell, ProgressChangedEventHandler workHandler, RunWorkerCompletedEventHandler completeHandler)
        {
            bgw = new BackgroundWorker();
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += workHandler;
            bgw.RunWorkerCompleted += completeHandler;
            bgw.DoWork += (s, ee) =>
            {
                var now = DateTime.Now;
                if (folder.Length == 0) folder = $"{now.Year}_{now.Month}_{now.Day}_{now.Minute}_{now.Second}";
                Directory.CreateDirectory(folder);

                int end = startSeed + count;
                int cc = 0;

                ThreadLocal<StatWritingStream> perThreadStat = new ThreadLocal<StatWritingStream>(PrepareStatWriting, true);

                SaveAsStat(Path.Combine(folder, "base.stat"), originalGraph, rwp, 0).Wait();

                if (generation_type == "random")
                {
                    Parallel.For(startSeed, end, new ParallelOptions() { MaxDegreeOfParallelism = maxParalell }, async (i) =>
                    {
                        var graph = ObjectCopier.Clone(originalGraph);
                        var t = GenerateRandomElectoralDistrictSystem(i, graph, null);
                        await t;

                        await StreamStat(perThreadStat.Value.compressor, ToStatString(graph, rwp, t.Result));

                        Interlocked.Increment(ref cc);
                        if (cc % 1 == 0)
                        {
                            bgw.ReportProgress((int)((double)(cc) / (double)(count) * 100), new BatchedGenerationProgress() { done = cc, all = count });
                        }
                    });
                }
                else if (generation_type == "mcmc")
                {
                    for (int i = startSeed; i < end; i++)
                    {
                        var graphTask = rutil.GenerateMarkovAnalysis(originalGraph, "last", 100, 18, 0.15, i, 1, 0.0, 0.05, 2.0);
                        graphTask.Wait();
                        var graph = graphTask.Result;
                        if (graph == null) continue;

                        _seed = i; // TODO: ez miert kell?

                        StreamStat(perThreadStat.Value.compressor, ToStatString(graph, rwp, 0)).Wait();

                        bgw.ReportProgress((int)((double)(i + 1) / (double)(end) * 100), new BatchedGenerationProgress() { done = i+1, all = end });
                    }
                }

                Parallel.ForEach(perThreadStat.Values, async (v, state, i) =>
                {
                    await FinishStatWriting(Path.Combine(folder, $"generated{i}.stat"), v.compressed);
                    v.Dispose();
                });
            };
            bgw.RunWorkerAsync();
        }
    }
}
