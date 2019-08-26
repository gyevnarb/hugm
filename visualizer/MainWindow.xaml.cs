using hugm.graph;
using hugm.map;
using createmap;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace visualizer
{
    public partial class MainWindow : Window
    {
        private Graph myGraph;
        private List<object> associatedElems = new List<object>();
        private List<UndoAction> undoActions = new List<UndoAction>();

        public Graph MyGraph
        {
            private get { return myGraph; }
            set
            {
                if (value != null)
                {
                    myGraph = value;
                    ShowGraph();
                }
                else
                {
                    Console.WriteLine("Null graph!");
                }
            }
        }

        private double VotingAreaRadius = 10;
        private UIElement SelectedElement = null;
        private Border SelectedBorder;
        private Ellipse ConnectingElement1 = null;
        private Ellipse ConnectingElement2 = null;

        public MainWindow()
        {
            InitializeComponent();

            SelectedBorder = new Border();
            SelectedBorder.BorderThickness = new Thickness(1);
            SelectedBorder.BorderBrush = Brushes.Black;

            InitKeyHandlers();
            BuildGraph();
            ShowGraph();
        }

        private void BuildGraph()
        {
            myGraph = PopulateGraph.BuildGraph(@"../../korok_new.csv", false) as AreaGraph;
            //myGraph = new Graph();
            //
            //MyGraph.AddNode();
            //MyGraph.AddNode();
            //MyGraph.AddNode();
            //
            //MyGraph.AddEdge(0, 1);
            //MyGraph.AddEdge(1, 2);
            //
            //MyGraph.V[0].X = 100; MyGraph.V[0].Y = 100;
            //MyGraph.V[1].X = 200; MyGraph.V[1].Y = 200;
            //MyGraph.V[2].X = 170; MyGraph.V[2].Y = 120;
        }

        private void ShowGraph()
        {
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
        }

        private void DrawVotingArea(Node v)
        {
            canvas.Children.Add(CreateVotingArea(v.X, v.Y));
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
                    RemoveElement(SelectedElement);
                    SelectedElement = null;
                    UpdateSelection();
                }

            };

            KeyDown += (s, e) =>
            {
                int speed = 5;

                if (e.Key == Key.W)
                {
                    canvasTranslate.Y += speed;
                }
                if (e.Key == Key.A)
                {
                    canvasTranslate.X += speed;
                }
                if (e.Key == Key.S)
                {
                    canvasTranslate.Y -= speed;
                }
                if (e.Key == Key.D)
                {
                    canvasTranslate.X -= speed;
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
                canvasScale.ScaleX += (double)e.Delta / 10000;
                canvasScale.ScaleY += (double)e.Delta / 10000;
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
            }
            else if (ae is Edge)
            {
                MyGraph.RemoveEdge(ae as Edge);
                undoActions.Add(new UndoAction(myGraph, ae as Edge));
            }
            ShowGraph();
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
            }
            ShowGraph();
        }

        private UIElement CreateVotingArea(Vector position)
        {
            return CreateVotingArea(position.X, position.Y);
        }
        
        private UIElement CreateVotingArea(double X, double Y)
        {
            var sign = new Ellipse();
            sign.Width = VotingAreaRadius * 2;
            sign.Height = VotingAreaRadius * 2;
            Canvas.SetTop(sign, Y - VotingAreaRadius);
            Canvas.SetLeft(sign, X - VotingAreaRadius);
            Canvas.SetZIndex(sign, 2);
            sign.Fill = Brushes.DarkOrange;

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
            nh.Stroke = Brushes.Aquamarine;
            nh.StrokeThickness = 2;
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
                        ConnectingElement1.Fill = Brushes.Blue;
                    } 
                    else if (ConnectingElement1 == hitTestResult.VisualHit as Ellipse)
                    {
                        ConnectingElement1.Fill = Brushes.DarkOrange;
                        ConnectingElement1 = null;
                    }
                    else if (ConnectingElement2 == null)
                    {
                        ConnectingElement1.Fill = Brushes.DarkOrange;
                        ConnectingElement2 = hitTestResult.VisualHit as Ellipse;
                        CreateConnection(ConnectingElement1, ConnectingElement2);
                        ConnectingElement1 = ConnectingElement2 = null;
                    }
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
                Canvas.SetTop(SelectedBorder, Math.Min(l.Y1, l.Y2));
                Canvas.SetLeft(SelectedBorder, Math.Min(l.X1, l.X2));
                SelectedBorder.Width = Math.Abs(l.X1 - l.X2);
                SelectedBorder.Height = Math.Abs(l.Y1 - l.Y2);
                SelectedBorder.Visibility = Visibility.Visible;
            } 
            else if (SelectedElement is Ellipse)
            {
                Canvas.SetTop(SelectedBorder, Canvas.GetTop(SelectedElement));
                Canvas.SetLeft(SelectedBorder, Canvas.GetLeft(SelectedElement));
                SelectedBorder.Width = SelectedBorder.Height = VotingAreaRadius * 2;
                SelectedBorder.Visibility = Visibility.Visible;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            myGraph.Save(txbox.Text);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var graph = Graph.Load(txbox.Text);
            if (graph != null)
            {
                MyGraph = graph;
                undoActions.Clear();
            }
        }
    }
}