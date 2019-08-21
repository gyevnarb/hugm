using hugm.graph;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace visualizer
{
    public partial class MainWindow : Window
    {
        private Graph myGraph;
        private List<object> associatedElems;

        public Graph MyGraph
        {
            private get { return myGraph; }
            set
            {
                myGraph = value;
                ShowGraph();
            }
        }

        private double VotingAreaRadius = 20;
        private UIElement SelectedElement = null;
        private Border SelectedBorder;

        public MainWindow()
        {
            InitializeComponent();

            SelectedBorder = new Border();
            SelectedBorder.BorderThickness = new Thickness(1);
            SelectedBorder.BorderBrush = Brushes.Black;

            InitKeyHandlers();
            BuildGraph();
          //  ShowGraph();
        }

        private void ShowGraph()
        {
            canvas.Children.Clear();
            foreach (var v in MyGraph.V)
            {
                DrawVotingArea(v);
                foreach (var v2 in MyGraph.V)
                {
                    if (MyGraph.Adjacent(v.ID, v2.ID))
                        DrawNeighbourhood(new Edge(v, v2));
                }
            }
            SelectedBorder.Visibility = Visibility.Collapsed;
            canvas.Children.Add(SelectedBorder);
        }

        private void DrawVotingArea(Node v)
        {

            associatedElems.Add(v);
        }

        private void DrawNeighbourhood(Edge e)
        {

            associatedElems.Add(e);
        }

        private void InitKeyHandlers()
        {
            KeyUp += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Delete)
                {
                    RemoveElement(SelectedElement);
                    SelectedElement = null;
                    UpdateSelection();
                }
            };
        }

        private void RemoveElement(UIElement selectedElement)
        {
            int index = canvas.Children.IndexOf(selectedElement);
            var ae = associatedElems[index];
            if (ae is Node)
            {
                // remove node
            }
            else if (ae is Edge)
            {
                // remove edge
            }
            associatedElems.RemoveAt(index);
            canvas.Children.RemoveAt(index);
        }

        private void BuildGraph()
        {
            canvas.Children.Add(CreateVotingArea(200, 30));
            canvas.Children.Add(CreateVotingArea(60, 60));
            canvas.Children.Add(CreateNeighbourhood(60, 60, 30, 30));

            SelectedBorder.Visibility = Visibility.Collapsed;
            canvas.Children.Add(SelectedBorder);
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
            Canvas.SetTop(sign, Y - VotingAreaRadius * 2);
            Canvas.SetLeft(sign, X - VotingAreaRadius * 2);

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
            nh.StrokeThickness = 10;

            return nh;
        }

        private void Canvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var canvas = sender as Canvas;
            if (canvas == null) return;
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(canvas, e.GetPosition(canvas));
            SelectedElement = hitTestResult.VisualHit as UIElement;
            if (SelectedElement == SelectedBorder) return;
            UpdateSelection();
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

            } 
            else if (SelectedElement is Ellipse)
            {
                Canvas.SetTop(SelectedBorder, Canvas.GetTop(SelectedElement));
                Canvas.SetLeft(SelectedBorder, Canvas.GetLeft(SelectedElement));
                SelectedBorder.Width = SelectedBorder.Height = VotingAreaRadius * 2;
                SelectedBorder.Visibility = Visibility.Visible;
            }
        }
    }
}