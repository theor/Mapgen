using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using SkiaSharp.Views.WPF;

namespace Mapgen
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly Action _triggerRender;
        public MainWindowViewModel(Action triggerRender)
        {
            _triggerRender = triggerRender;
        }

        private EDirty Dirty = EDirty.None;

        public bool IsDirtyClear(EDirty d)
        {
            if (Dirty.HasFlag(d))
            {
                Dirty ^= d;
                return true;
            }
            return false;
        }
        public void SetDirty(EDirty d)
        {
            Dirty |= d;
        }

        private void ClearDirty(EDirty d)
        {
            Dirty ^= d;
        }

        public List<Vertex> vertices
        {
            get => _vertices;
            set
            {
                _vertices = value;
                SetDirty(EDirty.Vertices);
            }
        }

        private int _numberOfVertices = 1500;
        public int NumberOfVertices
        {
            get => _numberOfVertices;
            set { _numberOfVertices = value; OnPropertyChanged(); }
        }

        private double _freq = 1;
        public double Freq
        {
            get { return _freq; }
            set { _freq = value; OnPropertyChanged(); }
        }

        private int _seed = 42;
        public int Seed
        {
            get { return _seed; }
            set
            {
                _seed = value;
                OnPropertyChanged();
            }
        }

        public bool ShowVertices
        {
            get { return _showVertices; }
            set
            {
                _showVertices = value;
                OnPropertyChanged();
                _triggerRender();
            }
        }

        public bool ShowCentroids
        {
            get { return _showCentroids; }
            set
            {
                _showCentroids = value;
                OnPropertyChanged();
                _triggerRender();
            }
        }

        public bool FillPolygons
        {
            get { return _fillPolygons; }
            set
            {
                _fillPolygons = value;
                OnPropertyChanged();
                _triggerRender();
            }
        }

        public bool ShowOutline
        {
            get { return _showOutline; }
            set
            {
                _showOutline = value;
                OnPropertyChanged();
                _triggerRender();
            }
        }

        public bool _showOutline = true;


        private VoronoiMesh<Vertex, Cell, VoronoiEdge<Vertex, Cell>> VoronoiMesh;
        private List<Vertex> _vertices;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private RenderingData _renderData = new RenderingData();
        private bool _showVertices;
        private bool _showCentroids;
        private bool _fillPolygons;

        public void MakeRandom(Size size)
        {
            var r = new Random(Seed);

            vertices = new List<Vertex>(NumberOfVertices);
            for (var i = 0; i < NumberOfVertices; i++)
            {
                var vi = new Vertex(size.Width * r.NextDouble(), size.Height * r.NextDouble());
                vertices.Add(vi);
            }


            try
            {
                VoronoiMesh = MIConvexHull.VoronoiMesh.Create<Vertex, Cell>(vertices);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }

            SetDirty(EDirty.Vertices);
            SetDirty(EDirty.Delaunay);
            //txtBlkTimer.Text = string.Format("{0} faces", _vm.VoronoiMesh.Vertices.Count());
        }

        public void ShowVoronoi(UIElementCollection drawingCanvasChildren)
        {
            foreach (VoronoiEdge<Vertex, Cell> edge in VoronoiMesh.Edges)
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

        public void Render(object sender, SKPaintGLSurfaceEventArgs e)
        {
            if (IsDirtyClear(EDirty.Vertices))
            {
                _renderData.SkVertices = vertices.Select(v => new SKPoint((float)v.Position[0], (float)v.Position[1])).ToArray();
                ClearDirty(EDirty.Vertices);
            }
            if (IsDirtyClear(EDirty.ElevationNoise))
            {
                _renderData.noiseBitmap = new SKBitmap(new SKImageInfo(e.RenderTarget.Width, e.RenderTarget.Height, SKColorType.Gray8, SKAlphaType.Opaque));
                byte[] pixels = new byte[e.RenderTarget.Width * e.RenderTarget.Height];
                var p = new LibNoise.Perlin();
                p.Seed = 43;
                p.OctaveCount = 4;
                p.Frequency = Freq;
                for (int i = 0; i < pixels.Length; i++)
                {
                    (int x, int y) = (i % e.RenderTarget.Width, i / e.RenderTarget.Width);
                    double n = MainWindow.Clamp((p.GetValue(x, y, 0) + 1) / 2.0, 0, 1);
                    pixels[i] = (byte)(n * 255); ;
                }
                unsafe
                {
                    fixed (byte* pi = pixels)
                        _renderData.noiseBitmap.SetPixels((IntPtr) pi);
                }
            }
            if (IsDirtyClear(EDirty.Delaunay))
            {
                var cells = (List<Cell>)VoronoiMesh.Vertices;
                _renderData.delaunayvertices = new SKPoint[cells.Count * 3];

                _renderData.delaunayColors = new SKColor[cells.Count * 3];
                _renderData.centroids = new SKPoint[cells.Count];

                Random r = new Random(42);
                for (var index = 0; index < cells.Count; index++)
                {
                    var cell = cells[index];
                    _renderData.delaunayvertices[index * 3] = cell.Vertices[0].ToSkPoint();
                    _renderData.delaunayvertices[index * 3 + 1] = cell.Vertices[1].ToSkPoint();
                    _renderData.delaunayvertices[index * 3 + 2] = cell.Vertices[2].ToSkPoint();
                    _renderData.delaunayColors[index * 3] = SKColor.FromHsl(r.Next(360), 75, 75);
                    _renderData.delaunayColors[index * 3 + 1] = _renderData.delaunayColors[index * 3];
                    _renderData.delaunayColors[index * 3 + 2] = _renderData.delaunayColors[index * 3];
                    _renderData.centroids[index] = cell.Centroid;
                }
            }
            _renderData.Render(sender, e, ShowVertices, ShowOutline, ShowCentroids, FillPolygons, true);
        }

        internal void RefreshMatrix(float scale, Vector translation)
        {
            _renderData.matrix.SetScaleTranslate(scale, scale, (float)translation.X, (float)translation.Y);
        }

        public void ComputeDelaunay()
        {
            var p = new LibNoise.Perlin();
            p.Seed = 43;
            p.OctaveCount = 4;
            p.Frequency = Freq;

            //double min = 1;
            //double max = 0;
            foreach (var cell in VoronoiMesh.Vertices.ToList())
            {
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
                //cell.Brush = new SolidColorBrush(Color.FromRgb(b, b, b));
            }
            SetDirty(EDirty.Delaunay);
        }
    }
}