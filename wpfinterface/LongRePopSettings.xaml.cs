using core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace wpfinterface
{
    /// <summary>
    /// Interaction logic for LongRePopSettings.xaml
    /// </summary>
    public partial class LongRePopSettings : Window
    {
        public LongRePopSettings()
        {
            InitializeComponent();
        }

        public double from, to, step;
        public string ex = null;

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                from = double.Parse(txFrom.Text);
                to = double.Parse(txTo.Text);
                step = double.Parse(txStep.Text);
            }
            catch (Exception exe)
            {
                ex = exe.Message;
            }
            finally
            {
                Close();
            }
        }
    }
}
