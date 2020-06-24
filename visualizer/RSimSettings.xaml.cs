using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace visualizer
{
    /// <summary>
    /// Interaction logic for RSimSettings.xaml
    /// </summary>
    public partial class RSimSettings : Window
    {
        public RSimSettings()
        {
            InitializeComponent();
            wndRSim.Width = 256;
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            int nsims = int.Parse(txtNSims.Text);
            int nloop = int.Parse(txtNLoop.Text);
            int ndists = int.Parse(txtNDists.Text);
            int seed = int.Parse(txtSeed.Text);
            double popcons = double.Parse(txtPopCons.Text);
            double beta = double.Parse(txtBeta.Text);
            double lambda = double.Parse(txtLambda.Text);
            double eprob = double.Parse(txtEprob.Text);
            string savePath = txtPath.Text;

            wndRSim.Width = 512;
            MainWindow.graphUtil.GenerateMarkovAnalysis(nsims, ndists, popcons, seed, nloop, beta, eprob, lambda, savePath);            
        }

        private void btnPath_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                txtPath.Text = result.ToString();
            }
        }
    }
}
