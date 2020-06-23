using hugm;
using hugm.graph;
using hugm.map;
using RDotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace visualizer
{
    class ElectoralDistrict
    {
        public List<AreaNode> nodes;
        public HashSet<AreaNode> availableNodes;
        public int pop = 0;
        public int id;
    }

    public class GraphUtility
    {
        //Init R interoperability
        REngine rEngine = REngine.GetInstance();

        BackgroundWorker bgw;

        private static float atlag = 76818; // 2011

        private int _seed;
        private Graph myGraph;
        private double similarity = 1.0;
        List<int> origElectoralSettings = new List<int>();

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
               
        public void GenerateMarkovAnalysis()
        {
            rEngine.Evaluate("library(redist)");
            rEngine.Evaluate("source(\"../../../hugm/markov_analysis.R\")");
            MyGraph = RUtils.WriteNewPartitionGraph(MyGraph, "../../../hugm/partitions.csv", "../../data/map_new.bin");
        }

        public void GenerateRandomElectoralDistrictSystem(long seed)
        {
            // 1. 18 Random node kivalasztasa, minden keruletbol egyet
            // 2. Novesztes egyelore nepesseg korlat betartasa nelkul
            // 3. Tul kicsiket felnoveljuk hogy elerjek a hatart
            // 4. Túl nagyokat meg lecsokkentjuk
            if (MyGraph == null) return;
            int MAX_STEP = 10000; // Ha 3. vagy 4. lepes egyenként tul lepne a max step-et akkor megallitjuk

            _seed = (int)(seed % int.MaxValue);
            var rng = new Random(_seed);
            foreach (var v in MyGraph.V) v.Marked = false;
            List<AreaNode> points = new List<AreaNode>();
            for (int i = 1; i <= 18; ++i)
            {
                var ns = MyGraph.V.Where(x =>
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
                    pop = p.Pop,
                    id = p.ElectorialDistrict
                });
            }

            // TODO: +-20 at is figylemebe lehetne venni, akkora hiba meg torveny szeirnt belefer
            int z = 0;
            while (z < MyGraph.V.Count - 18)
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
                        ujlista[i].pop += chosenNode.Pop;
                        chosenNode.ElectorialDistrict = ujlista[i].id;
                        z++;
                    }
                }
            }

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
                            if (v.ElectorialDistrict == id && !MyGraph.IsCuttingNode(v))
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
                            ujlista[i].pop += n.Pop;
                            ujlista[j].pop -= n.Pop;
                            n.ElectorialDistrict = ujlista[i].id;
                            done = true;
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
                            if (v.ElectorialDistrict == id && !MyGraph.IsCuttingNode(v))
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
                            ujlista[j].pop += n.Pop;

                            ujlista[i].availableNodes.Clear();
                            ujlista[i].nodes.Remove(n);
                            foreach (var v in ujlista[i].nodes)
                            {
                                ujlista[i].availableNodes.UnionWith(v.Adjacents.Select(x => x as AreaNode));
                            }
                            ujlista[i].pop -= n.Pop;

                            n.ElectorialDistrict = ujlista[j].id;
                            done = true;
                        }
                        l++;
                    }
                }
                ujlista.Sort((a, b) => b.pop - a.pop);
                for (int k = 0; k < 18; ++k) if (ujlista[k].pop < atlag * 1.15) { h = k; break; }
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

        private void SaveAsStat(string filename)
        {
            if (MyGraph == null) return;

            List<VoteResult> results = new List<VoteResult>(18);
            for (int i = 0; i < 18; ++i) results.Add(new VoteResult());
            VoteResult glob = new VoteResult();

            int diffs = 0;
            for (int i = 0; i < origElectoralSettings.Count; ++i)
            {
                if ((MyGraph.V[i] as AreaNode).ElectorialDistrict != origElectoralSettings[i]) diffs++;
            }
            similarity = 1.0 - (double)diffs / (double)origElectoralSettings.Count;

            foreach (var n in MyGraph.V)
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

            string text = $"0.1;{_seed};{valid15};{valid20};{fideszkdnp};{osszefogas};{jobbik};{lmp};{similarity}";
            foreach (var se in results) text += $";{se.Gyoztes}";
            System.IO.File.WriteAllText(filename, text);
        }

        public string GetStatistics()
        {
            if (MyGraph == null) return "";

            Dictionary<int, VoteResult> results = new Dictionary<int, VoteResult>();
            VoteResult glob = new VoteResult();

            int diffs = 0;
            for (int i = 0; i < origElectoralSettings.Count; ++i)
            {
                if ((MyGraph.V[i] as AreaNode).ElectorialDistrict != origElectoralSettings[i]) diffs++;
            }
            similarity = 1.0 - (double)diffs / (double)origElectoralSettings.Count;

            foreach (var n in MyGraph.V)
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

        public void StartBatchedGeneration(string folder, int startSeed, int count, string originalGraph, ProgressChangedEventHandler workHandler, RunWorkerCompletedEventHandler completeHandler)
        {
            bgw = new BackgroundWorker();
            bgw = new BackgroundWorker();
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += workHandler;
            bgw.RunWorkerCompleted += completeHandler;

            var now = DateTime.Now;
            if (folder.Length == 0) folder = $"{now.Year}_{now.Month}_{now.Day}_{now.Minute}_{now.Second}";
            int end = startSeed + count;
            bgw.DoWork += (s, ee) =>
            {
                for (int i = startSeed; i < end; ++i)
                {
                    GenerateRandomElectoralDistrictSystem(i);
                    System.IO.Directory.CreateDirectory(folder);
                    SaveAsStat(System.IO.Path.Combine(folder, i + ".stat"));
                    var graph = AreaUtils.Load(originalGraph); // TODO: klonozni kene nem fajlbol vissza olvasni
                    if (graph != null)
                    {
                        MyGraph = graph;
                        origElectoralSettings.Clear();
                        foreach (AreaNode v in MyGraph.V) origElectoralSettings.Add(v.ElectorialDistrict);
                    }
                    bgw.ReportProgress((int)((double)(i - startSeed) / (double)(end - startSeed) * 100));
                }
            };
            bgw.RunWorkerAsync();
        }
    }
}
