using hugm.graph;
using hugm.map;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using RDotNet.NativeLibrary;

namespace visualizer
{
    public partial class MainWindow : Window
    {
        private readonly GraphUtility graph = new GraphUtility();

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

        public MainWindow()
        {
            InitializeComponent();

            SelectedBorder = new Border();
            SelectedBorder.BorderThickness = new Thickness(SelectionBorderThickness);
            SelectedBorder.BorderBrush = selectionBorderBaseColor;

            InitKeyHandlers();
        }

        private void ShowGraph()
        {
            var MyGraph = graph.MyGraph;

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
            var MyGraph = graph.MyGraph;

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
            var MyGraph = graph.MyGraph;

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
            AreaUtils.Save(txbox.Text, graph.MyGraph);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            graph.Load(AreaUtils.Load(txbox.Text));
            undoActions.Clear();
            ShowGraph();
        }

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
            string stat = graph.GetStatistics();
            if (stat != null && stat != "") MessageBox.Show(stat);
        }

        private void Button_Click_Do2(object sender, RoutedEventArgs e)
        {
            graph.GenerateRandomElectoralDistrictSystem(DateTime.Now.Ticks);
            undoActions.Clear();
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

        private void ScrnBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveImage(scrnTxb.Text);
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
            graph.StartBatchedGeneration(txFolder.Text, int.Parse(txSeed.Text), int.Parse(txCount.Text), txbox.Text,
                (s, ee) => progressbar.Value = ee.ProgressPercentage,
                (s, ee) => progressbar.Value = 0);
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
            graph.GenerateMarkovAnalysis();
            undoActions.Clear();
            ShowGraph();
        }
    }
}