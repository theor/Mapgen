﻿#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using MIConvexHull;

#endregion

//3d print several layer - either terrasses or smoothed
//paint them
//screw hole in corner, add plexisglass with names engraved
//plate number intruded behind

namespace Mapgen
{
    /// <summary>
    ///   Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Vertex> Vertices;
        private const int NumberOfVertices = 500;
        VoronoiMesh<Vertex, Cell, VoronoiEdge<Vertex, Cell>> voronoiMesh;

        public MainWindow()
        {
            InitializeComponent();
            this.Title += string.Format(" ({0} points)", NumberOfVertices);

            btnFindDelaunay.IsEnabled = false;
            btnFindVoronoi.IsEnabled = false;
        }

        void ShowVertices(List<Vertex> vertices)
        {
            for (var i = 0; i < vertices.Count; i++)
            {
                drawingCanvas.Children.Add(vertices[i]);
            }
        }

        void MakeRandom(int n, List<Vertex> vertices)
        {
            var r = new Random();
            var sizeX = drawingCanvas.ActualWidth;
            var sizeY = drawingCanvas.ActualHeight;
            for (var i = 0; i < n; i++)
            {
                var vi = new Vertex(sizeX * r.NextDouble(), sizeY * r.NextDouble());
                vertices.Add(vi);
            }
            btnFindDelaunay.IsEnabled = true;
            btnFindVoronoi.IsEnabled = true;
        }

        void Create(List<Vertex> vertices)
        {
            drawingCanvas.Children.Clear();
            ShowVertices(vertices);

            try
            {
                voronoiMesh = VoronoiMesh.Create<Vertex, Cell>(vertices);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }
            txtBlkTimer.Text = string.Format("{0} faces", voronoiMesh.Vertices.Count());

            Vertices = vertices;

            btnFindDelaunay.IsEnabled = true;
            btnFindVoronoi.IsEnabled = true;
        }

        private void btnMakePoints_Click(object sender, RoutedEventArgs e)
        {
            var vs = new List<Vertex>();
            MakeRandom(NumberOfVertices, vs);
            Create(vs);
        }

      
        private void btnFindDelaunay_Click(object sender, RoutedEventArgs e)
        {
            drawingCanvas.Children.Clear();

            btnFindDelaunay.IsEnabled = false;
            btnFindVoronoi.IsEnabled = true;

            foreach (var cell in voronoiMesh.Vertices)
            {
                drawingCanvas.Children.Add(cell.Visual);
            }

            ShowVertices(Vertices);
        }

        static int IsLeft(Point a, Point b, Point c)
        {
            return ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X)) > 0 ? 1 : -1;
        }

        static Point Center(Cell c)
        {
            var v1 = (Vector)c.Vertices[0].ToPoint();
            var v2 = (Vector)c.Vertices[1].ToPoint();
            var v3 = (Vector)c.Vertices[2].ToPoint();

            return (Point)((v1 + v2 + v3) / 3);
        }

        private void btnFindVoronoi_Click(object sender, RoutedEventArgs e)
        {
            drawingCanvas.Children.Clear();
            btnFindVoronoi.IsEnabled = false;
            btnFindDelaunay.IsEnabled = true;

            foreach (var edge in voronoiMesh.Edges)
            {
                var from = edge.Source.Circumcenter;
                var to = edge.Target.Circumcenter;
                drawingCanvas.Children.Add(new Line { X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y, Stroke = Brushes.Black });
            }

            foreach (var cell in voronoiMesh.Vertices)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (cell.Adjacency[i] == null)
                    {
                        var from = cell.Circumcenter;
                        var t = cell.Vertices.Where((_, j) => j != i).ToArray();
                        var factor = 100 * IsLeft(t[0].ToPoint(), t[1].ToPoint(), from) * IsLeft(t[0].ToPoint(), t[1].ToPoint(), Center(cell));
                        var dir = new Point(0.5 * (t[0].Position[0] + t[1].Position[0]), 0.5 * (t[0].Position[1] + t[1].Position[1])) - from;
                        var to = from + factor * dir;
                        drawingCanvas.Children.Add(new Line { X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y, Stroke = Brushes.Black });
                    }
                }
            }

            ShowVertices(Vertices);
            drawingCanvas.Children.Add(new Rectangle { Width = drawingCanvas.ActualWidth, Height = drawingCanvas.ActualHeight, Stroke = Brushes.Black, StrokeThickness = 3 });
        }
    }
}