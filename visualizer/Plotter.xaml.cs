﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using hugm.map;

namespace visualizer
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
            {"AvgWinnerRate", (plot, stat) => plot.PlotAverageWinnerRate(stat) }
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
    }
}
