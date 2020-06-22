using hugm.graph;
using hugm.map;
using hugm;
using createmap;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Linq;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using RDotNet;
using System.Linq.Expressions;
using RDotNet.NativeLibrary;

namespace visualizer
{
    public partial class MainWindow : Window
    {
        private static float atlag = 76818; // 2011

        private int _seed;
        private Graph myGraph;
        private double similarity = 1.0;
        List<int> origElectoralSettings = new List<int>();

        BackgroundWorker bgw;

        //Init R interoperability
        REngine rEngine = REngine.GetInstance();

        public Graph MyGraph
        {
            private get { return myGraph; }
            set
            {
                if (value != null)
                {
                    myGraph = value;
                    undoActions.Clear();
                }
                else
                {
                    Console.WriteLine("Null graph!");
                }
            }
        }

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
        private Brush lineBaseColor = Brushes.Black;
        private Brush selectionBorderBaseColor = Brushes.White;

        private Brush[] nodeBrushes =
        {
            Brushes.Wheat,
            Brushes.Violet,
            Brushes.DarkOrange,
            Brushes.Black,
            Brushes.Brown,
            Brushes.AliceBlue,
            Brushes.AntiqueWhite,
            Brushes.DeepPink,
            Brushes.Firebrick,
            Brushes.Gainsboro,
            Brushes.Fuchsia,
            Brushes.Gold,
            Brushes.Silver,
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
        private double CameraMoveSpeed = 5;
        private double ZoomScale = 10000;

        private string CsvPath = @"../../data/korok.csv";

        public MainWindow()
        {
            InitializeComponent();

            SelectedBorder = new Border();
            SelectedBorder.BorderThickness = new Thickness(SelectionBorderThickness);
            SelectedBorder.BorderBrush = selectionBorderBaseColor;

            InitKeyHandlers();
            //MyGraph = PopulateGraph.BuildGraph(CsvPath, false, 500.0);
        }

        private void ShowGraph()
        {
            if (MyGraph == null || chkDisableui.IsChecked.Value) return;

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

        class ElectoralDistrict
        {
            public List<AreaNode> nodes;
            public HashSet<AreaNode> availableNodes;
            public int pop = 0;
            public int id;
        }

        private void ModifyRandomlyThyElectoralDsitrictSystem(long seed)
        {
            if (MyGraph == null) return;
            int MAX_STEP = 1000;

            _seed = (int)(seed % int.MaxValue);
            var rng = new Random(_seed);

            List<int> pops = new List<int>(18);
            for (int i = 0; i < 18; ++i) pops.Add(0);
            foreach (AreaNode v in MyGraph.V) pops[v.ElectorialDistrict - 1] += v.Pop;

            double MIN_DISTRICT_SIZ = atlag * 0.85;
            double MAX_DISTRICT_SIZ = atlag * 1.15;

            for (int i = 0; i < MAX_STEP; ++i)
            {
                var possibleNodes = MyGraph.V.Where(n =>
                {
                    AreaNode a = n as AreaNode;
                    if (pops[a.ElectorialDistrict - 1] - a.Pop < MIN_DISTRICT_SIZ) return false;

                    bool hasGoodAdjacent = false;
                    foreach (AreaNode v in a.Adjacents)
                    {
                        if (pops[v.ElectorialDistrict - 1] + a.Pop < MAX_DISTRICT_SIZ)
                        {
                            hasGoodAdjacent = true;
                            break;
                        }
                    }
                    if (!hasGoodAdjacent) return false;

                    if (MyGraph.IsCuttingNode(a)) return false;

                    return true;
                }).ToList();

                AreaNode f = possibleNodes[rng.Next(possibleNodes.Count)] as AreaNode;
                var possibleTargets = f.Adjacents.Where(x =>
                {
                    var a = x as AreaNode;
                    return pops[a.ElectorialDistrict - 1] + f.Pop < MAX_DISTRICT_SIZ;
                }).ToList(); // Nagyobb valoszinuseggel megy oda, amibol tobb szomszedja van
                AreaNode t = possibleTargets[rng.Next(possibleTargets.Count)] as AreaNode;

                f.ElectorialDistrict = t.ElectorialDistrict;
                pops[t.ElectorialDistrict - 1] += f.Pop;
                pops[f.ElectorialDistrict - 1] -= f.Pop;
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
            foreach(var se in results) text += $";{se.Gyoztes}";            
            System.IO.File.WriteAllText(filename, text);
        }

        private void GenerateRandomElectoralDsitrictSystem(long seed)
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

        private void InitKeyHandlers()
        {
            KeyUp += (s, e) =>
            {
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
                if (e.Key == Key.W)
                {
                    canvasTranslate.Y += CameraMoveSpeed;
                }
                if (e.Key == Key.A)
                {
                    canvasTranslate.X += CameraMoveSpeed;
                }
                if (e.Key == Key.S)
                {
                    canvasTranslate.Y -= CameraMoveSpeed;
                }
                if (e.Key == Key.D)
                {
                    canvasTranslate.X -= CameraMoveSpeed;
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

        private void RemoveElement(UIElement selectedElement)
        {
            int index = canvas.Children.IndexOf(selectedElement);
            var ae = associatedElems[index];
            if (ae is Node)
            {
                MyGraph.RemoveNode(ae as Node);
                undoActions.Add(new UndoAction(myGraph, ae as Node));
                ShowGraph();
            }
            else if (ae is Edge)
            {
                MyGraph.RemoveEdge(ae as Edge);
                undoActions.Add(new UndoAction(myGraph, ae as Edge));
                canvas.Children.RemoveAt(index);
                associatedElems.RemoveAt(index);
            }
        }

        private void CreateConnection(Ellipse e1, Ellipse e2)
        {
            if (e1 == e2) return;

            int index1 = canvas.Children.IndexOf(e1);
            int index2 = canvas.Children.IndexOf(e2);
            var ae1 = associatedElems[index1];
            var ae2 = associatedElems[index2];

            if (!MyGraph.Adjacent(ae1 as Node, ae2 as Node))
            {
                MyGraph.AddEdge(ae1 as Node, ae2 as Node);
                undoActions.Add(new UndoAction(myGraph, ae1 as Node, ae2 as Node));
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AreaUtils.Save(txbox.Text, myGraph);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var graph = AreaUtils.Load(txbox.Text);
            if (graph != null)
            {
                MyGraph = graph;
                origElectoralSettings.Clear();
                foreach (AreaNode v in MyGraph.V) origElectoralSettings.Add(v.ElectorialDistrict);
                ShowGraph();
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(NumberTextBox.Text, out int thresh))
            {
                MyGraph = PopulateGraph.BuildGraph(CsvPath, false, thresh);
            }
        }

        private void Save(string path)
        {
            if (path == null || path.Length == 0) path = $"default_{DateTime.Now.Ticks}.png";

            Rect bounds = VisualTreeHelper.GetDescendantBounds(canv);
            double dpi = 96d;

            RenderTargetBitmap rtb = new RenderTargetBitmap((int)bounds.Width, (int)bounds.Height, dpi, dpi, System.Windows.Media.PixelFormats.Default);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(canvas);
                dc.DrawRectangle(Brushes.Green, null, new Rect(new Point(), bounds.Size));
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

        private void Button_Click_Do(object sender, RoutedEventArgs e)
        {
            if (MyGraph == null) return;

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

            MessageBox.Show(sss);
        }

        private void Button_Click_Do2(object sender, RoutedEventArgs e)
        {
            GenerateRandomElectoralDsitrictSystem(DateTime.Now.Ticks);
            //ModifyRandomlyThyElectoralDsitrictSystem(DateTime.Now.Ticks);
            ShowGraph();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectedNode(comboo.SelectedIndex);
        }

        private void FilterDistrict_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateFiltering();
        }

        private void FilterElectorialIze_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateFiltering();
        }

        private void updateFiltering()
        {
           // var myavailabledistricts = new List<string>();
           // var myavailableelectorials = new List<string>();

           // myavailabledistricts.Add("All");
           // myavailableelectorials.Add("All");

            if (filterElectorialIze.SelectedIndex == 0 && filterDistrict.SelectedIndex == 0)
            {
                foreach (var c in canvas.Children) if (c is Shape) (c as Shape).Visibility = Visibility.Visible;
           //     filterDistrict.ItemsSource = availableDistricts;
            //    filterElectorialIze.ItemsSource = availableElectorials;
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
                             //   mya.Add(e.ElectoralDistrict.ToString());
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
                             //   mya.Add(e.CityDistrict.ToString());
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

      /*      if (filterElectorialIze.SelectedIndex != 0)
            {
                filterDistrict.Items.Clear();
                filterDistrict.ItemsSource = myavailabledistricts;
            }

            if (filterDistrict.SelectedIndex != 0)
            {
                filterElectorialIze.Items.Clear();
                filterElectorialIze.ItemsSource = myavailableelectorials;
            }*/
        }

        private void ScrnBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Save(scrnTxb.Text);
                MessageBox.Show("Image saved!");
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            ShowGraph();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            bgw = new BackgroundWorker();
            bgw = new BackgroundWorker();
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += (s, ee) => progressbar.Value = ee.ProgressPercentage;
            bgw.RunWorkerCompleted += (s, ee) => progressbar.Value = 0;

            var folder = txFolder.Text;
            var now = DateTime.Now;
            if (folder.Length == 0) folder = $"{now.Year}_{now.Month}_{now.Day}_{now.Minute}_{now.Second}";
            int start = int.Parse(txSeed.Text);
            int end = start + int.Parse(txCount.Text);
            var orig = txbox.Text;
            bgw.DoWork += (s, ee) =>
            {
                for (int i = start; i < end; ++i)
                {
                    GenerateRandomElectoralDsitrictSystem(i);
                    System.IO.Directory.CreateDirectory(folder);
                    SaveAsStat(System.IO.Path.Combine(folder, i + ".stat"));
                    var graph = AreaUtils.Load(orig); // TODO: klonozni kene nem fajlbol vissza olvasni
                    if (graph != null)
                    {
                        MyGraph = graph;
                        origElectoralSettings.Clear();
                        foreach (AreaNode v in MyGraph.V) origElectoralSettings.Add(v.ElectorialDistrict);
                    }
                    bgw.ReportProgress((int)((double)(i - start) / (double)(end - start) * 100));
                }
            };
            bgw.RunWorkerAsync();
        }

        private static void PrintPaths()
        {
            string rHome = "";
            string rPath = "";

            NativeUtility util = new NativeUtility();
            var logInfo = util.FindRPaths(ref rPath, ref rHome);

            Console.WriteLine("Is this process 64 bits? {0}", System.Environment.Is64BitProcess);
            Console.WriteLine(logInfo);
        }

        private void btnR_Click(object sender, RoutedEventArgs e)
        {
            rEngine.Evaluate("library(redist)");
            rEngine.Evaluate("source(\"../../../hugm/markov_analysis.R\")");
            Graph g = RUtils.WriteNewPartitionGraph(myGraph, "../../../hugm/partitions.csv", "../../data/map_new.bin");
            MyGraph = g;
            ShowGraph();
        }
    }
}