using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using hugm;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using hugm.map;
using System.IO;
using hugm.graph;
using System.ComponentModel;
using RDotNet;

namespace visualizer
{
    /// <summary>
    /// Interaction logic for RSimSettings.xaml
    /// </summary>
    public partial class RSimSettings : Window
    {
        private TextWriter cls_out;

        RUtils r;
        REngine rEngine;

        public RSimSettings()
        {
            InitializeComponent();

            TextBoxOutputter outputter = new TextBoxOutputter(txtOut);  // Setup writing to text box output
            cls_out = Console.Out;  // Store for resetting later
            Console.SetOut(outputter);  // Redirect all console writes to textbox
            TextBoxDevice device = new TextBoxDevice(outputter);

            rEngine = REngine.GetInstance(device: device);
            r = new RUtils(rEngine);
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
            string savePath = txtPath.Text.Replace('\\', '/');

            string[] selectionList = new string[] { "last", "first", "random" };
            string selection = selectionList[cmbSelection.SelectedIndex];

            r.GenerateMarkovAnalysis(MainWindow.graphUtil.MyGraph, selection, nsims, ndists, popcons, seed, nloop, beta, eprob, lambda, savePath);
        }

        private void btnPath_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result.ToString() == "OK")
                    txtPath.Text = dialog.SelectedPath;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            r.CancelSimRun();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Exiting R simulation.");
            Console.SetOut(cls_out);
            Close();
        }
    }

    public class TextBoxOutputter : TextWriter
    {
        TextBox textBox = null;

        public TextBoxOutputter(TextBox output)
        {
            textBox = output;
        }

        public override void Write(char value)
        {
            base.Write(value);
            textBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                textBox.AppendText(value.ToString());
                textBox.ScrollToEnd();
            }));
        }

        public override Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }
    }
}
