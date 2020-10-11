using hugm.graph;
using hugm.map;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using System.Linq;
using Microsoft.Win32;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using ScottPlot;

namespace visualizer
{
    public partial class MainWindow : Window
    {
        public static readonly GraphUtility graphUtil = new GraphUtility();

        private List<object> associatedElems = new List<object>();
        private List<UndoAction> undoActions = new List<UndoAction>();
        private UIElement SelectedElement = null;
        private AreaNode SelectedNode = null;
        private Border SelectedBorder;
        private Ellipse ConnectingElement1 = null;
        private Ellipse ConnectingElement2 = null;
        private List<string> availableDistricts = new List<string>();
        private List<string> availableElectorials = new List<string>();

        #region Colors

        private Brush nodeBaseColor = Brushes.DarkOrange;
        private Brush nodeHighlightedColor = Brushes.Blue;
        private Brush lineBaseColor = Brushes.DarkSlateGray;
        private Brush selectionBorderBaseColor = Brushes.Black;

        private Brush[] nodeBrushes =
        {
            Brushes.Crimson,
            Brushes.Violet,
            Brushes.DarkOrange,
            Brushes.Black,
            Brushes.Brown,
            Brushes.Magenta,
            Brushes.Indigo,
            Brushes.DeepPink,
            Brushes.Firebrick,
            Brushes.BurlyWood,
            Brushes.Fuchsia,
            Brushes.Gold,
            Brushes.CadetBlue,
            Brushes.Sienna,
            Brushes.SeaGreen,
            Brushes.Tomato,
            Brushes.Tan,
            Brushes.SteelBlue,
            Brushes.MidnightBlue
        };

        Brush getColor(int i) { return nodeBrushes[i]; }

        Brush setDefaultColor(Ellipse e)
        {
            int index1 = canvas.Children.IndexOf(e);
            var ae1 = associatedElems[index1] as AreaNode;
            return getColor(ae1.Areas[0].ElectoralDistrict);
        }

        #endregion        

        private double VotingAreaRadius = 10;
        private double SelectionBorderThickness = 2;
        private double SelectionBorderMargin = 2;
        private double NeighbourhoodLineThickness = 2;
        private double ZoomScale = 10000;

        private Timer movementTimer;
        private double HorizontalMoveDirection = 0;
        private double VerticalMoveDirection = 0;
        private double CameraMoveSpeed = 500;
        private bool W = false, A = false, S = false, D = false, MouseLeft = false;
        private double MouseXOrig, MouseYOrig, MouseMoveSpeed = 0.1;

        public MainWindow()
        {
            InitializeComponent();

            SelectedBorder = new Border();
            SelectedBorder.BorderThickness = new Thickness(SelectionBorderThickness);
            SelectedBorder.BorderBrush = selectionBorderBaseColor;

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            InitKeyHandlers();
        }

        #region Input
        
        private void InitKeyHandlers()
        {
            movementTimer = new Timer(1000f / 60f);
            movementTimer.Elapsed += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    canvasTranslate.X += HorizontalMoveDirection * CameraMoveSpeed * 1f / 60f;
                    canvasTranslate.Y += VerticalMoveDirection * CameraMoveSpeed * 1f / 60f;
                });
            };
            movementTimer.AutoReset = true;
            movementTimer.Start();

            KeyUp += (s, e) =>
            {
                if (e.Key == Key.W && W)
                {
                    VerticalMoveDirection -= 1.0;
                    W = false;
                }
                if (e.Key == Key.A && A)
                {
                    HorizontalMoveDirection -= 1.0;
                    A = false;
                }
                if (e.Key == Key.S && S)
                {
                    VerticalMoveDirection += 1.0;
                    S = false;
                }
                if (e.Key == Key.D && D)
                {
                    HorizontalMoveDirection += 1.0;
                    D = false;
                }

                if (e.Key == Key.Delete)
                {
                    if (SelectedElement != null)
                    {
                        RemoveElement(SelectedElement);
                        SelectedElement = null;
                        UpdateSelection();
                    }
                }
            };

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.W && !W)
                {
                    VerticalMoveDirection += 1.0;
                    W = true;
                }
                if (e.Key == Key.A && !A)
                {
                    HorizontalMoveDirection += 1.0;
                    A = true;
                }
                if (e.Key == Key.S && !S)
                {
                    VerticalMoveDirection -= 1.0;
                    S = true;
                }
                if (e.Key == Key.D && !D)
                {
                    HorizontalMoveDirection -= 1.0;
                    D = true;
                }

                if (e.Key == Key.U)
                {
                    if (undoActions.Count != 0)
                    {
                        undoActions[undoActions.Count - 1].Undo();
                        undoActions.RemoveAt(undoActions.Count - 1);
                        ShowGraph();
                    }
                }
            };

            MouseWheel += (s, e) =>
            {
                canvasScale.ScaleX += (double)e.Delta / ZoomScale;
                canvasScale.ScaleY += (double)e.Delta / ZoomScale;
            };
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (MouseLeft)
            {
                if (e.LeftButton == MouseButtonState.Released)
                {
                    MouseLeft = false;
                    return;
                }

                var X = e.GetPosition(sender as Canvas).X;
                var Y = e.GetPosition(sender as Canvas).Y;

                Dispatcher.Invoke(() =>
                {
                    canvasTranslate.X += (X - MouseXOrig) * MouseMoveSpeed;
                    canvasTranslate.Y += (Y - MouseYOrig) * MouseMoveSpeed;
                });
            }

            if (!MouseLeft && e.LeftButton == MouseButtonState.Pressed)
            {
                MouseLeft = true;
                MouseXOrig = e.GetPosition(canvas).X;
                MouseYOrig = e.GetPosition(canvas).Y;
            }
        }

        private void Canvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var canvas = sender as Canvas;
            if (canvas == null) return;
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(canvas, e.GetPosition(canvas));

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                SelectedElement = hitTestResult.VisualHit as UIElement;
                if (SelectedElement == SelectedBorder) return;
                UpdateSelection();
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                if (hitTestResult.VisualHit is Ellipse)
                {
                    if (ConnectingElement1 == null)
                    {
                        ConnectingElement1 = hitTestResult.VisualHit as Ellipse;
                        ConnectingElement1.Fill = nodeHighlightedColor;
                    }
                    else if (ConnectingElement1 == hitTestResult.VisualHit as Ellipse)
                    {
                        ConnectingElement1.Fill = setDefaultColor(ConnectingElement1);
                        ConnectingElement1 = null;
                    }
                    else if (ConnectingElement2 == null)
                    {
                        ConnectingElement1.Fill = setDefaultColor(ConnectingElement1); ;
                        ConnectingElement2 = hitTestResult.VisualHit as Ellipse;
                        CreateConnection(ConnectingElement1, ConnectingElement2);
                        ConnectingElement1 = ConnectingElement2 = null;
                    }
                }
            }
            else if (e.MiddleButton == MouseButtonState.Pressed)
            {
                if (ConnectingElement1 != null && hitTestResult.VisualHit is Ellipse)
                {
                    ConnectingElement2 = hitTestResult.VisualHit as Ellipse;
                    var n1 = associatedElems[canvas.Children.IndexOf(ConnectingElement1)] as Node;
                    var n2 = associatedElems[canvas.Children.IndexOf(ConnectingElement2)] as Node;
                    var index = associatedElems.IndexOf(new Edge(n1, n2));
                    if (index >= 0)
                    {
                        RemoveElement(canvas.Children[index]);
                        ConnectingElement1.Fill = setDefaultColor(ConnectingElement1); ;
                        ConnectingElement1 = null;
                    }
                    ConnectingElement2 = null;
                }
            }
        }

        #endregion

        #region Drawing
        
        public void ShowGraph(bool force = false)
        {
            if (!graphUtil.ValidGraph()) return;
            if (!force && !autoUiRefresh.IsChecked) return;

            var MyGraph = graphUtil.MyGraph;

            canvas.Children.Clear();
            associatedElems.Clear();
            for (int i = 0; i < MyGraph.V.Count; ++i)
            {
                var v = MyGraph.V[i];
                DrawVotingArea(v);

                for (int j = i + 1; j < MyGraph.V.Count; ++j)
                {
                    var v2 = MyGraph.V[j];
                    if (MyGraph.Adjacent(v, v2))
                        DrawNeighbourhood(new Edge(v, v2));
                }
            }
            SelectedBorder.Visibility = Visibility.Collapsed;
            canvas.Children.Add(SelectedBorder);
            associatedElems.Add(new object()); // placeholder

            filterDistrict.Items.Clear();
            filterElectorialIze.Items.Clear();
            filterDistrict.Items.Add("All");
            filterElectorialIze.Items.Add("All");
            availableDistricts.Add("All");
            availableElectorials.Add("All");
            foreach (var e in associatedElems)
            {
                if (!(e is AreaNode)) continue;
                foreach (var ee in (e as AreaNode).Areas)
                {
                    if (!filterDistrict.Items.Contains(ee.CityDistrict.ToString()))
                    {
                        filterDistrict.Items.Add(ee.CityDistrict.ToString());
                        availableDistricts.Add(ee.CityDistrict.ToString());
                    }

                    if (!filterElectorialIze.Items.Contains(ee.ElectoralDistrict.ToString()))
                    {
                        filterElectorialIze.Items.Add(ee.ElectoralDistrict.ToString());
                        availableElectorials.Add(ee.ElectoralDistrict.ToString());
                    }

                }
            }
            filterDistrict.SelectedIndex = 0;
            filterElectorialIze.SelectedIndex = 0;
        }

        private void DrawVotingArea(Node v)
        {
            canvas.Children.Add(CreateVotingArea(v.X, v.Y, (v as AreaNode).Areas[0].ElectoralDistrict));
            associatedElems.Add(v);
        }

        private void DrawNeighbourhood(Edge e)
        {
            canvas.Children.Add(CreateNeighbourhood(e.N1.X, e.N1.Y, e.N2.X, e.N2.Y));
            associatedElems.Add(e);
        }

        private void RemoveElement(UIElement selectedElement)
        {
            var MyGraph = graphUtil.MyGraph;

            int index = canvas.Children.IndexOf(selectedElement);
            var ae = associatedElems[index];
            if (ae is Node)
            {
                MyGraph.RemoveNode(ae as Node);
                undoActions.Add(new UndoAction(MyGraph, ae as Node));
                ShowGraph();
            }
            else if (ae is Edge)
            {
                MyGraph.RemoveEdge(ae as Edge);
                undoActions.Add(new UndoAction(MyGraph, ae as Edge));
                canvas.Children.RemoveAt(index);
                associatedElems.RemoveAt(index);
            }
        }

        private void CreateConnection(Ellipse e1, Ellipse e2)
        {
            var MyGraph = graphUtil.MyGraph;

            if (e1 == e2) return;

            int index1 = canvas.Children.IndexOf(e1);
            int index2 = canvas.Children.IndexOf(e2);
            var ae1 = associatedElems[index1];
            var ae2 = associatedElems[index2];

            if (!MyGraph.Adjacent(ae1 as Node, ae2 as Node))
            {
                MyGraph.AddEdge(ae1 as Node, ae2 as Node);
                undoActions.Add(new UndoAction(MyGraph, ae1 as Node, ae2 as Node));
                DrawNeighbourhood(new Edge(ae1 as Node, ae2 as Node));
            }
        }

        private UIElement CreateVotingArea(Vector position, int electoraldistrict)
        {
            return CreateVotingArea(position.X, position.Y, electoraldistrict);
        }

        private UIElement CreateVotingArea(double X, double Y, int electoraldistrict)
        {
            var sign = new Ellipse();
            sign.Width = VotingAreaRadius * 2;
            sign.Height = VotingAreaRadius * 2;
            Canvas.SetTop(sign, Y - VotingAreaRadius);
            Canvas.SetLeft(sign, X - VotingAreaRadius);
            Canvas.SetZIndex(sign, 2);
            sign.Fill = getColor(electoraldistrict);
            return sign;
        }

        private UIElement CreateNeighbourhood(Vector p1, Vector p2)
        {
            return CreateNeighbourhood(p1.X, p1.Y, p2.X, p2.Y);
        }

        private UIElement CreateNeighbourhood(double X1, double Y1, double X2, double Y2)
        {
            var nh = new Line();
            nh.X1 = X1;
            nh.X2 = X2;
            nh.Y1 = Y1;
            nh.Y2 = Y2;
            nh.Visibility = Visibility.Visible;
            nh.Stroke = lineBaseColor;
            nh.StrokeThickness = NeighbourhoodLineThickness;
            Canvas.SetZIndex(nh, 1);
            return nh;
        }

        private void UpdateSelection()
        {
            if (SelectedElement == null)
            {
                SelectedBorder.Visibility = Visibility.Collapsed;
                return;
            }
            else if (SelectedElement is Line)
            {
                var l = SelectedElement as Line;
                Canvas.SetTop(SelectedBorder, Math.Min(l.Y1 - SelectionBorderMargin, l.Y2 - SelectionBorderMargin));
                Canvas.SetLeft(SelectedBorder, Math.Min(l.X1 - SelectionBorderMargin, l.X2 - SelectionBorderMargin));
                SelectedBorder.Width = Math.Abs(l.X1 - l.X2 + SelectionBorderMargin * 2);
                SelectedBorder.Height = Math.Abs(l.Y1 - l.Y2 + SelectionBorderMargin * 2);
                SelectedBorder.Visibility = Visibility.Visible;
            }
            else if (SelectedElement is Ellipse)
            {
                Canvas.SetTop(SelectedBorder, Canvas.GetTop(SelectedElement) - SelectionBorderMargin);
                Canvas.SetLeft(SelectedBorder, Canvas.GetLeft(SelectedElement) - SelectionBorderMargin);
                SelectedBorder.Width = SelectedBorder.Height = VotingAreaRadius * 2 + SelectionBorderMargin * 2;
                SelectedBorder.Visibility = Visibility.Visible;

                SelectedNode = associatedElems[canvas.Children.IndexOf(SelectedElement)] as AreaNode;
                comboo.Items.Clear();
                foreach (var s in SelectedNode.Areas)
                {
                    comboo.Items.Add(s.AreaID.ToString());
                }
                comboo.SelectedIndex = 0;
                UpdateSelectedNode(0);
            }
        }

        private void UpdateSelectedNode(int i)
        {
            if (i >= 0)
            {
                electDistrict.Content = SelectedNode.Areas[i].ElectoralDistrict.ToString();
                cityDistrict.Content = SelectedNode.Areas[i].CityDistrict;
                areaNumber.Content = SelectedNode.Areas[i].AreaNo.ToString();
                adress.Content = SelectedNode.Areas[i].FormattedAddress;
                atjelentkezes.IsChecked = SelectedNode.Areas[i].Atjelentkezes;
                fideszKdnp.Content = SelectedNode.Areas[i].Results.FideszKDNP.ToString();
                osszefogas.Content = SelectedNode.Areas[i].Results.Osszefogas.ToString();
                jobbik.Content = SelectedNode.Areas[i].Results.Jobbik.ToString();
                lmp.Content = SelectedNode.Areas[i].Results.LMP.ToString();
                megjelent.Content = SelectedNode.Areas[i].Results.Megjelent.ToString();
                osszes.Content = SelectedNode.Areas[i].Results.Osszes.ToString();
            }
        }

        #endregion

        #region Event Handlers

        private void FileLoadHandler(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Graph (*.bin)|*.bin";

            if (Directory.Exists(Environment.CurrentDirectory + @"\data\") && File.Exists(Environment.CurrentDirectory + @"\data\map.bin"))
            {
                openFileDialog.InitialDirectory = Environment.CurrentDirectory + @"\data";
                openFileDialog.FileName = "map.bin";
            }

            if (openFileDialog.ShowDialog() == true)
            {
                lblLoadedGraphPath.Text = openFileDialog.FileName;
                graphUtil.Load(AreaUtils.Load(openFileDialog.FileName));
                undoActions.Clear();
                ShowGraph();
            }
        }

        private void FileSaveHandler(object sender, RoutedEventArgs e)
        {
            if (!graphUtil.ValidGraph()) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Graph (*.bin)|*.bin";
            if (Directory.Exists(Environment.CurrentDirectory + @"\data\"))
            {
                saveFileDialog.InitialDirectory = Environment.CurrentDirectory + @"\data";
            }
            saveFileDialog.FileName = @"map.bin";
            if (saveFileDialog.ShowDialog() == true)
            {
                AreaUtils.Save(saveFileDialog.FileName, graphUtil.MyGraph);
            }
        }

        private void RunMCRedistrictingHandler(object sender, RoutedEventArgs e)
        {
            if (!graphUtil.ValidGraph()) return;

            RSimSettings settings = new RSimSettings();
            settings.ShowDialog();
            undoActions.Clear();
            ShowGraph();
        }

        private void RunRandomDistrictGrowthHandler(object sender, RoutedEventArgs e)
        {
            if (!graphUtil.ValidGraph()) return;

            graphUtil.GenerateRandomElectoralDistrictSystem(DateTime.Now.Ticks, graphUtil.MyGraph);
            undoActions.Clear();
            ShowGraph();
        }

        private void InfoStatisticsHandler(object sender, RoutedEventArgs e)
        {
            if (!graphUtil.ValidGraph()) return;

            string stat = graphUtil.GetStatistics(graphUtil.MyGraph);
            MessageBox.Show(stat);
        }

        private void ToolsSaveImageHandler(object sender, RoutedEventArgs e)
        {
            if (!graphUtil.ValidGraph()) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG Image (*.png)|*.png";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    SaveImage(saveFileDialog.FileName);
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ToolsRefreshHandler(object sender, RoutedEventArgs e)
        {
            ShowGraph(true);
        }

        private void GenerationStartHandler(object sender, RoutedEventArgs e)
        {
            if (!graphUtil.ValidGraph()) return;

            var rwp = new RandomWalkParams();
            rwp.numRun = int.Parse(txtNumRuns.Text);
            rwp.walkLen = int.Parse(txtLenWalk.Text);
            rwp.method = (SamplingMethod)cmbMethod.SelectedItem;
            rwp.party = (Parties)cmbParty.SelectedItem;
            rwp.partyProb = double.Parse(txtPartyProb.Text);
            rwp.excludeSelected = chkSelected.IsChecked.Value;
            rwp.invert = chkInvert.IsChecked.Value;

            graphUtil.StartBatchedGeneration(txFolder.Text, int.Parse(txSeed.Text), int.Parse(txCount.Text), ObjectCopier.Clone(graphUtil.MyGraph), rwp,
                (s, ee) => progressbar.Value = ee.ProgressPercentage,
                (s, ee) => progressbar.Value = 0);
        }

        private void Button_Click_LoadStats(object sender, RoutedEventArgs e)
        {
            if (txStatFolder.Text == "")
            {
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dialog.SelectedPath = Environment.CurrentDirectory;
                    dialog.ShowNewFolderButton = true;
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        txStatFolder.Text = dialog.SelectedPath;
                    }
                }
            }

            graphUtil.LoadStats(txStatFolder.Text);
            if (graphUtil.MyStats == null) return;

            plotCombo.IsEnabled = true;
            plotBtn.IsEnabled = true;
            plotCombo.ItemsSource = Plotter.Plots;
            plotCombo.SelectedIndex = 0;
        }

        private void btnRunRandomWalk_Click(object sender, RoutedEventArgs e)
        {
            if (!graphUtil.ValidGraph()) return;

            int numRun = int.Parse(txtNumRuns.Text);
            int walkLen = int.Parse(txtLenWalk.Text);
            SamplingMethod method = (SamplingMethod)cmbMethod.SelectedItem;
            DistCalcMethod distMethod = (DistCalcMethod)cmbDist.SelectedItem;
            Parties party = (Parties)cmbParty.SelectedItem;
            double partyProb = double.Parse(txtPartyProb.Text);
            bool excludeSelected = chkSelected.IsChecked.Value;
            bool invert = chkInvert.IsChecked.Value;

            RandomWalkSimulation simulation = new RandomWalkSimulation(graphUtil.MyGraph, method, walkLen, numRun, excludeSelected, invert);
            if (method == SamplingMethod.PREFER_PARTY)
            {
                simulation.PartyPreference = party;
                simulation.PartyProbability = partyProb;
            }
            simulation.Simulate();
            RandomWalkAnalysis analysis = new RandomWalkAnalysis(simulation, distMethod);
            graphUtil.PreviousRandomWalk = analysis;
        }

        private void Button_Click_PlotGraph(object sender, RoutedEventArgs e)
        {
            if (graphUtil.MyStats == null) return;

            var plt = new Plotter();
            plt.Plot(plotCombo.Text, graphUtil.MyStats);
            plt.Title = plotCombo.Text;
            plt.ShowDialog();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cmbMethod.ItemsSource = Enum.GetValues(typeof(SamplingMethod)).Cast<SamplingMethod>();
            cmbMethod.SelectedItem = SamplingMethod.UNIFORM;
            cmbDist.ItemsSource = Enum.GetValues(typeof(DistCalcMethod)).Cast<DistCalcMethod>();
            cmbDist.SelectedItem = DistCalcMethod.OCCURENCE_CNT;
            cmbDistrict.ItemsSource = Enum.GetValues(typeof(PlotCalculationMethod)).Cast<PlotCalculationMethod>();
            cmbDistrict.SelectedItem = PlotCalculationMethod.EXPECTED;
            cmbParty.ItemsSource = Enum.GetValues(typeof(Parties)).Cast<Parties>();
            cmbParty.SelectedItem = Parties.FIDESZ;
        }

        private void btnPlotErrors_Click(object sender, RoutedEventArgs e)
        {
            if (graphUtil.PreviousRandomWalk == null)
                return;

            PlotCalculationMethod method = (PlotCalculationMethod)cmbDistrict.SelectedItem;

            var plt = new ScottPlot.Plot();
            var districts = Enumerable.Range(1, graphUtil.PreviousRandomWalk.NumElectoralDistricts).ToList().ConvertAll(x => (double)x).ToArray();
            switch (method)
            {
                case PlotCalculationMethod.EXPECTED:
                    var errorsAndStds = graphUtil.PreviousRandomWalk.NumWrongDistrict(method);
                    var sorted = errorsAndStds.Select((x, i) => new KeyValuePair<(double, double), int>(x, i)).OrderByDescending(x => x.Key.Item1).ToList();
                    plt.PlotBar(districts, sorted.Select(x => x.Key.Item1).ToArray(), sorted.Select(x => x.Key.Item2).ToArray());
                    plt.Grid(enableVertical: false, lineStyle: ScottPlot.LineStyle.Dot);
                    plt.Title("Mean Error and Standard Deviation");
                    plt.XTicks(districts, sorted.Select(x => (x.Value + 1).ToString()).ToArray());
                    plt.SaveFig("expected_errors.png");
                    break;
                case PlotCalculationMethod.MAP:
                    var errorsAndStds1 = graphUtil.PreviousRandomWalk.NumWrongDistrict(method);
                    var sorted1 = errorsAndStds1.Select((x, i) => new KeyValuePair<(double, double), int>(x, i)).OrderByDescending(x => x.Key.Item1).ToList();
                    plt.PlotBar(districts, sorted1.Select(x => x.Key.Item1).ToArray());
                    plt.Title("MAP Error");
                    plt.XTicks(districts, sorted1.Select(x => (x.Value + 1).ToString()).ToArray());
                    plt.Grid(enableVertical: false, lineStyle: ScottPlot.LineStyle.Dot);
                    plt.SaveFig("map_errors.png");
                    break;
                default:
                    break;
            }
        }

        private void btnPlotDistributions_Click(object sender, RoutedEventArgs e)
        {
            if (graphUtil.PreviousRandomWalk == null)
                return;

            var districts = Enumerable.Range(1, graphUtil.PreviousRandomWalk.NumElectoralDistricts).ToList().ConvertAll(x => (double)x).ToArray();

            var mp = new MultiPlot(rows: 3, cols: 6);
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    int district = 6 * row + col;
                    var subplot = mp.GetSubplot(row, col);
                    var meanDistribution = graphUtil.PreviousRandomWalk.AverageDistributionForDistrict(district + 1);
                    subplot.PlotBar(districts, meanDistribution.ToArray(), horizontal: true);
                    subplot.XTicks(Enumerable.Repeat("", 18).ToArray());
                    mp.GetSubplot(row, col).XLabel($"District {district + 1}");
                }
            }
            mp.SaveFig("dist_plot.png");
        }

        private void cmbMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((SamplingMethod)cmbMethod.SelectedItem == SamplingMethod.PREFER_PARTY)
                stkParty.Visibility = Visibility.Visible;
        }

        private void FilterDistrict_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateFiltering();
        }

        private void FilterElectorialIze_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateFiltering();
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectedNode(comboo.SelectedIndex);
        }

        #endregion

        #region Utilites

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void SaveImage(string path)
        {
            if (path == null || path.Length == 0) path = $"default_{DateTime.Now.Ticks}.png";

            Rect bounds = VisualTreeHelper.GetDescendantBounds(canv);
            double dpi = 96d;

            RenderTargetBitmap rtb = new RenderTargetBitmap((int)bounds.Width, (int)bounds.Height, dpi, dpi, System.Windows.Media.PixelFormats.Default);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(canvas);
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(242, 243, 244)), null, new Rect(new Point(), bounds.Size));
                dc.DrawRectangle(vb, null, new Rect(new Point(), bounds.Size));
            }

            rtb.Render(dv);

            BitmapEncoder pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(rtb));

            System.IO.MemoryStream ms = new System.IO.MemoryStream();

            pngEncoder.Save(ms);
            ms.Close();

            System.IO.File.WriteAllBytes(path, ms.ToArray());
        }

        private void updateFiltering()
        {
            if (filterElectorialIze.SelectedIndex == 0 && filterDistrict.SelectedIndex == 0)
            {
                foreach (var c in canvas.Children)
                {
                    if (c is Shape)
                    {
                        (c as Shape).Visibility = Visibility.Visible;
                    }
                }
                return;
            }

            for (int i = 0; i < canvas.Children.Count; ++i)
            {
                bool toHide = true;
                if (associatedElems[i] is AreaNode)
                {
                    if (filterDistrict.SelectedIndex != 0)
                    {
                        foreach (var e in (associatedElems[i] as AreaNode).Areas)
                        {
                            if ((string)filterDistrict.SelectedItem == e.CityDistrict)
                            {
                                toHide = false;
                                break;
                            }
                        }
                    }
                    else toHide = false;

                    if (filterElectorialIze.SelectedIndex != 0 && !toHide)
                    {
                        foreach (var e in (associatedElems[i] as AreaNode).Areas)
                        {
                            bool b = false;
                            if ((string)filterElectorialIze.SelectedItem == e.ElectoralDistrict.ToString())
                            {
                                b = true;
                                break;
                            }
                            if (!b) toHide = true;
                        }
                    }
                }

                if (associatedElems[i] is Edge)
                {
                    var ee = associatedElems[i] as Edge;
                    if (filterDistrict.SelectedIndex != 0)
                    {
                        bool isFiltered1 = false;
                        bool isFiltered2 = false;
                        foreach (var e in (ee.N1 as AreaNode).Areas)
                        {
                            if ((string)filterDistrict.SelectedItem == e.CityDistrict)
                            {
                                isFiltered1 = true;
                                break;
                            }
                        }
                        foreach (var e in (ee.N2 as AreaNode).Areas)
                        {
                            if ((string)filterDistrict.SelectedItem == e.CityDistrict)
                            {
                                isFiltered2 = true;
                                break;
                            }
                        }
                        if (isFiltered1 && isFiltered2)
                        {
                            toHide = false;
                        }
                    }
                    else toHide = false;

                    if (filterElectorialIze.SelectedIndex != 0 && !toHide)
                    {
                        bool isFiltered1 = false;
                        bool isFiltered2 = false;
                        foreach (var e in (ee.N1 as AreaNode).Areas)
                        {
                            if ((string)filterElectorialIze.SelectedItem == e.ElectoralDistrict.ToString())
                            {
                                isFiltered1 = true;
                                break;
                            }
                        }
                        foreach (var e in (ee.N2 as AreaNode).Areas)
                        {
                            if ((string)filterElectorialIze.SelectedItem == e.ElectoralDistrict.ToString())
                            {
                                isFiltered2 = true;
                                break;
                            }
                        }
                        if (!isFiltered1 || !isFiltered2)
                        {
                            toHide = true;
                        }
                    }
                }
                if (canvas.Children[i] is Shape)
                {
                    if (toHide) (canvas.Children[i] as Shape).Visibility = Visibility.Hidden;
                    else (canvas.Children[i] as Shape).Visibility = Visibility.Visible;

                    if (!toHide)
                    {

                    }
                }
            }
        }

        #endregion
    }
}