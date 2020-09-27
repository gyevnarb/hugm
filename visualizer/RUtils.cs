using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using hugm.graph;
using hugm.map;
using System.ComponentModel;
using RDotNet;
using RDotNet.Devices;
using RDotNet.Internals;
using System.Windows.Controls;

namespace visualizer
{
    class RUtils
    {
        public bool IsSimulationDone { get; private set; } = false;
        private REngine rEngine;
        private BackgroundWorker bgw;

        public RUtils() => rEngine = REngine.GetInstance();
        public RUtils(REngine eng) => rEngine = eng;

        /// <summary>
        /// Run MCMC redistributing simulator
        /// </summary>
        /// <remarks>
        ///  popcons = 0.05 means that any proposed swap that brings a district more than 5% away from population parity will be rejected.
        ///  Note that the total number of simulations run will be nsims* nloop.
        ///  The number of swaps each iteration is equal to Pois(lambda) + 1.
        /// </remarks>
        /// <see cref="https://cran.r-project.org/web/packages/redist/redist.pdf"/>
        /// <param name="nsims">The number of simulations run before a save point.</param>
        /// <param name="ndists">The numbe of congressional districts. The default is 18 for Budapest.</param>
        /// <param name="popcons">The strength of the hard population constraint.</param>
        /// <param name="seed">Random seed for reproducabilty</param>
        /// <param name="nloop">The total number of save points for the algorithm. The default is 1. </param>
        /// <param name="beta">The strength of the target strength in the MH ratio</param>
        /// <param name="eprob">The probability of keeping an edge connected. The default is 0.05.</param>
        /// <param name="lambda">The parameter detmerining the number of swaps to attempt each iteration fo the algoirhtm. The default is 0.</param>
        /// <param name="savePath">Save path of resulting partition.</param>
        public void GenerateMarkovAnalysis(Graph g, string selection, int nsims = 100, int ndists = 18, double popcons = 0.15, int seed = 1,
            int nloop = 1, double beta = 2500.0, double eprob = 0.01, double lambda = 0.0, string savePath = "data/partitions.csv")
        {
            string run_analysis = File.ReadAllText("data/markov_analysis.R");
            string sim_string = $"run_simulation({nsims}, {ndists}, {popcons:F4}, {seed}, {nloop}, {beta:F4}, {eprob:F4}, {lambda:F4}, \"{savePath}\")";
            run_analysis += sim_string;
            Console.WriteLine(sim_string);

            bgw = new BackgroundWorker();
            bgw.WorkerSupportsCancellation = true;
            bgw.WorkerReportsProgress = true;
            bgw.RunWorkerCompleted += (s, ee) => { IsSimulationDone = true; };

            bgw.DoWork += (s, ee) =>
            {
                rEngine.Evaluate(run_analysis);
                try
                {
                    string graphPath = savePath.Substring(0, savePath.LastIndexOf('/'));
                    graphPath += "/map_rsim.bin";
                    WriteNewPartitionGraph(g, selection, savePath, graphPath);
                    Graph gr = AreaUtils.Load(graphPath);
                    MainWindow.graphUtil.Load(gr);
                }
                catch (NullReferenceException e)
                {
                    Console.WriteLine(e.Message);
                }
            };
            bgw.RunWorkerAsync();
        }

        public void CancelSimRun()
        {
            Console.WriteLine("Cancelling simulation");
            if (bgw.IsBusy)
            {
                bgw.CancelAsync();
                rEngine.Dispose();
            }
        }

        public void MapToData(Graph m)
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
                foreach (AreaNode node in m.V)
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

        public Graph WriteNewPartitionGraph(Graph g, string selection, string partionPath, string savePath)
        {
            if (g == null)
                throw new NullReferenceException("Graph has not been assigned yet");
            Console.WriteLine("Writing new assignment");
            Random r = new Random();
            var file = File.ReadAllLines(partionPath).Skip(1).Select(x => x.Split(',')).Select(x => x.Select(y => int.Parse(y) + 1).ToList()).ToList();
            List<int> assignment;
            if (selection == "random")
                assignment = file[r.Next(file.Count)];
            else if (selection == "last")
                assignment = file[file.Count - 1];
            else if (selection == "first")
                assignment = file[0];
            else
                assignment = file[(int)Math.Floor(file.Count / 2.0)];
            int i = 0;
            foreach (AreaNode vert in g.V)
            {
                vert.Areas[0].ElectoralDistrict = assignment[i];
                i++;
            }
            AreaUtils.Save(savePath, g);
            Console.WriteLine("Created new electoral assignment under file map_new.bin");
            return g;
        }

        private static double GetDistance(Node n1, Node n2)
        {
            return Math.Pow(n1.X - n2.X, 2) + Math.Pow(n1.Y - n2.Y, 2);
        }
    }

    /// <summary>
    /// Writes to an arbitrary stream
    /// </summary>
    class TextBoxDevice : ICharacterDevice
    {
        private TextBoxOutputter txtOut;
        public TextBoxDevice(TextBoxOutputter txt) => txtOut = txt;
        public SymbolicExpression AddHistory(Language call, SymbolicExpression operation, Pairlist args, REnvironment environment) => environment.Engine.NilValue;
        public YesNoCancel Ask(string question) => YesNoCancel.Cancel;
        public void Busy(BusyType which) { }
        public void Callback() { }
        public string ChooseFile(bool create) => "";
        public void CleanUp(StartupSaveAction saveAction, int status, bool runLast) => Environment.Exit(status);
        public void ClearErrorConsole() { }
        public void EditFile(string file) { }
        public void FlushConsole() { }
        public SymbolicExpression LoadHistory(Language call, SymbolicExpression operation, Pairlist args, REnvironment environment) => environment.Engine.NilValue;
        public string ReadConsole(string prompt, int capacity, bool history) => "";
        public void ResetConsole() { }
        public SymbolicExpression SaveHistory(Language call, SymbolicExpression operation, Pairlist args, REnvironment environment) => environment.Engine.NilValue;
        public bool ShowFiles(string[] files, string[] headers, string title, bool delete, string pager) => false;
        public void ShowMessage(string message) => txtOut.Write(message);
        public void Suicide(string message) => CleanUp(StartupSaveAction.Suicide, 2, false);
        public void WriteConsole(string output, int length, ConsoleOutputType outputType) => txtOut.Write(output);
    }
}
