using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using RDotNet;
using RDotNet.Internals;
using RDotNet.Devices;
using core;

namespace wpfinterface
{
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

        private async void btnStart_Click(object sender, RoutedEventArgs e)
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

            await r.GenerateMarkovAnalysis(MainWindow.graphUtil.MyGraph, selection, nsims, ndists, popcons, seed, nloop, beta, eprob, lambda, savePath);
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
            // TODO
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
