#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Mapgen.Annotations;
using MIConvexHull;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

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
        private MainWindowViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            btnFindDelaunay.IsEnabled = false;
            btnFindVoronoi.IsEnabled = false;

            _vm = new MainWindowViewModel();
            DataContext = _vm;

            Title += string.Format(" ({0} points)", _vm.NumberOfVertices);

            skElement.PaintSurface += SkElementOnPaintSurface;

            this.MouseDown += (sender, args) => skElement.InvalidateVisual();

            this.MouseWheel += (_, a) =>
            {
                scale = Clamp(scale * (a.Delta > 0 ? 1.05f : 0.95f), 0.1f, 100);
                RefreshMatrix();
            };
        }

        private void RefreshMatrix()
        {
            matrix.SetScaleTranslate(scale, scale, 0, 0);
            skElement.InvalidateVisual();
        }

        public float scale;
        private SKMatrix matrix = SKMatrix.MakeIdentity();

        static float Clamp(float v, float a, float b)
        {
            return v < a ? a : (v > b ? b : v);
        }

        private void SkElementOnPaintSurface(object sender, SKPaintSurfaceEventArgs args)
        {
            (float w, float h) = (args.Info.Width, args.Info.Height);
            var c = args.Surface.Canvas;
            c.Clear(SKColors.Black);
            //using (var p = new SKPaint { Color = SKColors.Red })
            //    c.DrawCircle(w / 2, h / 2, Math.Min(w / 2, h / 2), p);
            var f = 1.0 / Math.Min(w / 2, h / 2);
            c.SetMatrix(matrix);
            using (var p = new SKPaint { Color = SKColors.DarkBlue, IsStroke = true, StrokeWidth = 0.7f, StrokeCap = SKStrokeCap.Round })
            {
                c.DrawPoints(SKPointMode.Polygon, delaunayvertices, p);
            }
            using (var p = new SKPaint { Color = SKColors.Cyan, IsStroke = true, StrokeWidth = 2, StrokeCap = SKStrokeCap.Round})
            {
                c.DrawPoints(SKPointMode.Points, _vm.SkVertices, p);
            }
        }

        private void Create(List<Vertex> vertices)
        {
            //drawingCanvas.Children.Clear();

            try
            {
                _vm.VoronoiMesh = VoronoiMesh.Create<Vertex, Cell>(vertices);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }
            txtBlkTimer.Text = string.Format("{0} faces", _vm.VoronoiMesh.Vertices.Count());

            _vm.vertices = vertices;
            
            //_vm.ShowVertices(drawingCanvas.Children);

            btnFindDelaunay.IsEnabled = true;
            btnFindVoronoi.IsEnabled = true;
        }

        private void btnMakePoints_Click(object sender, RoutedEventArgs e)
        {
            var vs = new List<Vertex>();
            _vm.MakeRandom(vs, new Size(skElement.CanvasSize.Width, skElement.CanvasSize.Height));
            Create(vs);
        }

        //public Canvas drawingCanvas { get; private set; }

        private SKPoint[] delaunayvertices = new SKPoint[0];
        private void btnFindDelaunay_Click(object sender, RoutedEventArgs e)
        {
            //drawingCanvas.Children.Clear();

            btnFindDelaunay.IsEnabled = false;
            btnFindVoronoi.IsEnabled = true;

            var p = new LibNoise.Perlin();
            p.Seed = 43;
            p.OctaveCount = 4;
            p.Frequency = _vm.Freq;

            //double min = 1;
            //double max = 0;
            delaunayvertices = new SKPoint[_vm.VoronoiMesh.Vertices.Count() * 3];
            for (var index = 0; index < _vm.VoronoiMesh.Vertices.ToList().Count; index++)
            {
                var cell = _vm.VoronoiMesh.Vertices.ToList()[index];
                double value = (p.GetValue(cell.Centroid.X, cell.Centroid.Y, 0) + 1) / 2.0;

                //if (value < min)
                //{
                //    min = value;
                //    Trace.WriteLine($"min: {min}");
                //}
                //if (value > max)
                //{
                //    max = value;
                //    Trace.WriteLine($"max: {max}");
                //}
                if (value < 0)
                    value = 0;
                if (value > 1)
                    value = 1;
                byte b = (byte)(value * 255);
                cell.Brush = new SolidColorBrush(Color.FromRgb(b, b, b));
                delaunayvertices[index * 3] = cell.Vertices[0].ToSkPoint();
                delaunayvertices[index * 3 + 1] = cell.Vertices[1].ToSkPoint();
                delaunayvertices[index * 3 + 2] = cell.Vertices[2].ToSkPoint();

                //drawingCanvas.Children.Add(cell.Visual);
            }

            //_vm.ShowVertices(drawingCanvas.Children);

            foreach (var cell in _vm.VoronoiMesh.Vertices)
            {
                //drawingCanvas.Children.Add(new Vertex(cell.Centroid.X, cell.Centroid.Y, Brushes.Cyan));
            }

        }

        private void btnFindVoronoi_Click(object sender, RoutedEventArgs e)
        {
            //drawingCanvas.Children.Clear();
            btnFindVoronoi.IsEnabled = false;
            btnFindDelaunay.IsEnabled = true;

            //var drawingCanvasChildren = drawingCanvas.Children;
            //_vm.ShowVoronoi(drawingCanvasChildren);

            //_vm.ShowVertices(drawingCanvas.Children);
            //drawingCanvas.Children.Add(new Rectangle { Width = drawingCanvas.ActualWidth, Height = drawingCanvas.ActualHeight, Stroke = Brushes.Black, StrokeThickness = 3 });
        }
    }

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public List<Vertex> vertices
        {
            get { return _vertices; }
            set { _vertices = value;
                SkVertices = value.Select(v => new SKPoint((float)v.Position[0], (float)v.Position[1])).ToArray();
            }
        }

        private int _numberOfVertices = 1500;
        public int NumberOfVertices
        {
            get => _numberOfVertices;
            set { _numberOfVertices = value; OnPropertyChanged(); }
        }

        public double Freq
        {
            get { return _freq; }
            set { _freq = value; OnPropertyChanged(); }
        }

        private double _freq = 1;

        public VoronoiMesh<Vertex, Cell, VoronoiEdge<Vertex, Cell>> VoronoiMesh;
        public SKPoint[] SkVertices = new SKPoint[0];
        private List<Vertex> _vertices;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ShowVoronoi(UIElementCollection drawingCanvasChildren)
        {
            foreach (var edge in VoronoiMesh.Edges)
            {
                var from = edge.Source.Circumcenter;
                var to = edge.Target.Circumcenter;
                drawingCanvasChildren.Add(new Line { X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y, Stroke = Brushes.Black });
            }

            foreach (var cell in VoronoiMesh.Vertices)
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
                        drawingCanvasChildren.Add(new Line { X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y, Stroke = Brushes.Black });
                    }
                }
            }
        }

        public static int IsLeft(Point a, Point b, Point c)
        {
            return ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X)) > 0 ? 1 : -1;
        }

        public static Point Center(Cell c)
        {
            var v1 = (Vector)c.Vertices[0].ToPoint();
            var v2 = (Vector)c.Vertices[1].ToPoint();
            var v3 = (Vector)c.Vertices[2].ToPoint();

            return (Point)((v1 + v2 + v3) / 3);
        }

        public void ShowVertices(UIElementCollection drawingCanvasChildren)
        {
            for (var i = 0; i < vertices.Count; i++)
            {
                drawingCanvasChildren.Add(vertices[i]);
            }
        }

        public void MakeRandom(List<Vertex> vertices, Size size)
        {
            var r = new Random(42);

            for (var i = 0; i < NumberOfVertices; i++)
            {
                var vi = new Vertex(size.Width * r.NextDouble(), size.Height * r.NextDouble());
                vertices.Add(vi);
            }
        }
    }

    static class Ext
    {
        public static SKPoint ToSkPoint(this MIConvexHull.IVertex v)
        {
            return new SKPoint((float)v.Position[0], (float)v.Position[1]);
        }
    }
}