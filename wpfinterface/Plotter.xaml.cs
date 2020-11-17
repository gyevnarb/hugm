using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using core;
using core.map;

namespace wpfinterface
{
    /// <summary>
    /// Interaction logic for RSimSettings.xaml
    /// </summary>
    public partial class Plotter : Window
    {
        public Plotter()
        {
            InitializeComponent();
        }

        private static Dictionary<string, Action<Plotter, Stats>> plots = new Dictionary<string, Action<Plotter, Stats>>()
        {
            {"WinnerRates", (plot, stat) => plot.PlotWinnerRates(stat) },
            {"DistrictWinnerRates", (plot, stat) => plot.PlotDistrictWinnerRates(stat) },
            {"AvgWinnerRate", (plot, stat) => plot.PlotAverageWinnerRate(stat) },
            {"WinnerWins", (plot, stat) => plot.PlotWinnerWins(stat) },
            {"FideszWins", (plot, stat) => plot.PlotFideszWins(stat) },
            {"AvgWrongAreaRate", (plot, stat) => plot.PlotAverageWrongPlaces(stat) }
        };

        public static List<string> Plots
        {
            get
            {
                return plots.Keys.ToList();
            }
        }

        public void Plot(string plot, Stats stat)
        {
            plots[plot](this, stat);
        }
        private double CalculateStandardDeviation(IEnumerable<double> values)
        {
            double standardDeviation = 0;

            if (values.Any())
            {
                // Compute the average.     
                double avg = values.Average();

                // Perform the Sum of (value-avg)_2_2.      
                double sum = values.Sum(d => Math.Pow(d - avg, 2));

                // Put it all together.      
                standardDeviation = Math.Sqrt((sum) / (values.Count() - 1));
            }

            return standardDeviation;
        }

        public void PlotDistrictWinnerRates(Stats stats)
        {
            var districtList = new List<List<double>>();
            for (int i = 0; i < stats.baseResult.result.Count; ++i) districtList.Add(new List<double>());
            foreach (var s in stats.generationResults)
            {
                var ordered = s.result.OrderByDescending(x => x.results[s.winner]).ToList();
                for (int i = 0; i < ordered.Count; ++i)
                {
                    districtList[i].Add(ordered[i].results[s.winner]);
                }
            }

            var avgList = new List<double>();
            var stdList = new List<double>();

            for (int i = 0; i < districtList.Count; ++i)
            {
                var d = districtList[i];
                var avg = d.Average();
                avgList.Add(avg);
                var max = d.Max();
                var min = d.Min();
                var std = CalculateStandardDeviation(d);
                stdList.Add(std);

                plot1.plt.PlotLine(i + 1, min, i + 1, max, color: System.Drawing.Color.Black, lineWidth: 4);
            }

            var xaxis2 = Enumerable.Range(1, 18).Select(x => (double)x).ToArray();
            var yaxis2 = stats.baseResult.result.Select(x => x.results[stats.baseResult.winner]).OrderByDescending(x => x).ToArray();
            plot1.plt.PlotScatterHighlight(xaxis2, yaxis2, lineWidth: 6, color: System.Drawing.Color.Red);
            plot1.plt.PlotScatter(xaxis2, avgList.ToArray(), errorY: stdList.ToArray(), errorLineWidth: 5, lineWidth: 4, color: System.Drawing.Color.Blue);
            var labels = new List<string> { "" };
            var xlabel = stats.baseResult.result.Zip(Enumerable.Range(1, 18)).OrderBy(x => x.First.results[stats.baseResult.winner]).Select(x => x.Second.ToString()).ToList();
            labels.AddRange(xlabel);
            plot1.plt.XTicks(labels.ToArray());
        }

        public void PlotWinnerRates(Stats stats)
        {
            foreach (var s in stats.generationResults)
            {
                var xaxis = Enumerable.Range(1, 18).Select(x => (double)x).ToArray();
                var yaxis = s.result.Select(x => x.results[s.winner]).OrderByDescending(x => x).ToArray();

                plot1.plt.PlotScatter(xaxis, yaxis);
            }

            var xaxis2 = Enumerable.Range(1, 18).Select(x => (double)x).ToArray();
            var yaxis2 = stats.baseResult.result.Select(x => x.results[stats.baseResult.winner]).OrderByDescending(x => x).ToArray();

            plot1.plt.PlotScatterHighlight(xaxis2, yaxis2, lineWidth: 6, color: System.Drawing.Color.Red);

            plot1.Render();
        }

        public void PlotAverageWinnerRate(Stats stats)
        {
            var yaxis = stats.generationResults.Select(r => r.result.Average(x => x.results[r.winner])).OrderByDescending(x => x).ToArray();
            var xaxis = Enumerable.Range(1, yaxis.Length).Select(x => (double)x).ToArray();

            plot1.plt.PlotScatter(xaxis, yaxis);

            var hs = stats.baseResult.result.Average(x => x.results[stats.baseResult.winner]);

            for (int i = 0; i < yaxis.Length; ++i)
            {
                if (hs > yaxis[i])
                {
                    plot1.plt.PlotPoint(i + 1, hs, color: System.Drawing.Color.Red, markerSize: 8);
                    break;
                }
            }

            plot1.Render();
        }

        public void PlotAverageWrongPlaces(Stats stats)
        {
            var yaxis = stats.generationResults.Select(r => r.result.Average(x => x.wrongDistrictPercentage)).OrderByDescending(x => x).ToArray();
            var xaxis = Enumerable.Range(1, yaxis.Length).Select(x => (double)x).ToArray();

            plot1.plt.PlotScatter(xaxis, yaxis);

            var hs = stats.baseResult.result.Average(x => x.wrongDistrictPercentage);

            for (int i = 0; i < yaxis.Length; ++i)
            {
                if (hs > yaxis[i])
                {
                    plot1.plt.PlotPoint(i + 1, hs, color: System.Drawing.Color.Red, markerSize: 8);
                    break;
                }
            }

            plot1.Render();
        }

        public void PlotFideszWins(Stats stats)
        {
            var yaxis = new List<double>();
            yaxis.AddRange(new List<double>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

            foreach (var x in stats.generationResults)
            {
                var c = x.result.Count(y => y.results.ToList().FindIndex(z => z == y.results.Max()) == 0);
                yaxis[c] += 1.0;
            }

            var xaxis = Enumerable.Range(0, 19).Select(x => (double)x).ToArray();

            plot1.plt.PlotBar(xaxis, yaxis.ToArray());
            plot1.plt.PlotPoint(stats.baseResult.result.Count(y => y.results.ToList().FindIndex(z => z == y.results.Max()) == stats.baseResult.winner), 0, markerSize: 15);
        }

        public void PlotWinnerWins(Stats stats)
        {
            var yaxis = new List<double>();
            yaxis.AddRange(new List<double>(){0, 0, 0 ,0 ,0 ,0 ,0 ,0 ,0 ,0 ,0 ,0 ,0 ,0 ,0 ,0 , 0, 0, 0});

            foreach (var x in stats.generationResults)
            {
                var c = x.result.Count(y => y.results.ToList().FindIndex(z => z == y.results.Max()) == x.winner);
                yaxis[c] += 1.0;
            }
            
            var xaxis = Enumerable.Range(0, 19).Select(x => (double)x).ToArray();

            plot1.plt.PlotBar(xaxis, yaxis.ToArray());
            plot1.plt.PlotPoint(stats.baseResult.result.Count(y => y.results.ToList().FindIndex(z => z == y.results.Max()) == stats.baseResult.winner), 0, markerSize: 15);
        }
    }
}
