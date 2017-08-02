using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MainWindowViewModel : ViewModel
    {
        private readonly Action _triggerRender;
        public MainWindowViewModel(Action triggerRender)
        {
            _triggerRender = triggerRender;
            RenderingOptions = new RenderingOptions(triggerRender);
            Elevation = new ElevationOptions(this, triggerRender);
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

        private int _numberOfVertices = 15;

        public int NumberOfVertices
        {
            get => _numberOfVertices;
            set { _numberOfVertices = value; OnPropertyChanged(); }
        }

        public int _seed = 42;

        public RenderingOptions RenderingOptions { get; }

        private VoronoiMesh<Vertex, Cell, VoronoiEdge<Vertex, Cell>> VoronoiMesh;
        private List<Vertex> _vertices;

        private RenderingData _renderData = new RenderingData();
        private (int, int) _size;
        public ElevationOptions Elevation { get; }

        public int Seed
        {
            get { return _seed; }
            set { _seed = value; OnPropertyChanged(); }
        }


        public void MakeRandom(Size size)
        {
            _size = ((int, int))(size.Width, size.Height);
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

        struct HalfEdge
        {
            public readonly SKPoint Point;
            public readonly int HalfId;

            public HalfEdge(int halfId, SKPoint point)
            {
                Point = point;
                HalfId = halfId;
            }
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
                _renderData.noiseBitmap = new SKBitmap(new SKImageInfo(_size.Item1, _size.Item2, SKColorType.Gray8, SKAlphaType.Opaque));
                byte[] pixels = new byte[_size.Item1 * _size.Item2];
                var p = new LibNoise.Perlin();
                p.Seed = Elevation.Seed;
                p.OctaveCount = Elevation.OctaveCount;
                p.Frequency = Elevation.Freq;
                Parallel.For(0, pixels.Length, i =>
                //for (int i = 0; i < pixels.Length; i++)
                {
                    (int x, int y) = (i % _size.Item2, i / _size.Item1);
                    double n = MainWindow.Clamp((p.GetValue(x, y, 0) + 1) / 2.0, 0, 1);
                    pixels[i] = (byte)(n * 255);
                    ;
                }
                );

                unsafe
                {
                    fixed (byte* pi = pixels)
                        _renderData.noiseBitmap.SetPixels((IntPtr)pi);
                }

                var cells = (List<Cell>)VoronoiMesh.Vertices;
                _renderData.noiseColors = new SKColor[cells.Count * 3];
                Parallel.For(0, cells.Count, i =>
                {
                    (double x, double y) = (cells[i].Centroid.X, cells[i].Centroid.Y);
                    double n = MainWindow.Clamp((p.GetValue(x, y, 0) + 1) / 2.0, 0, 1);
                    byte b = (byte)(n * 255);
                    _renderData.noiseColors[i * 3] = _renderData.noiseColors[i * 3 + 1] = _renderData.noiseColors[i * 3 + 2] = new SKColor(b, b, b);
                    cells[i].Elevation = (float)(n * 255);
                });
                SetDirty(EDirty.WaterLevel);
            }

            if (IsDirtyClear(EDirty.WaterLevel))
            {
                _renderData.SetupNoiseColorFilter(Elevation.WaterLevel);

                var voronoiMeshEdges = (List<VoronoiEdge<Vertex, Cell>>)VoronoiMesh?.Edges;
                List<SKPoint> outline = new List<SKPoint>();
                var dict = new Dictionary<int, HalfEdge>();
                for (var index = 0; index < voronoiMeshEdges?.Count; index++)
                {
                    VoronoiEdge<Vertex, Cell> edge = voronoiMeshEdges[index];

                    if ((edge.Source.Elevation <= Elevation.WaterLevel &&
                         edge.Target.Elevation > Elevation.WaterLevel) ||
                        (edge.Target.Elevation <= Elevation.WaterLevel &&
                         edge.Source.Elevation > Elevation.WaterLevel))
                    {
                        var c = outline.Count;
                        for (int i = 0; i < 3; i++)
                            for (int j = 0; j < 3; j++)
                                if (Ext.AboutEqual(edge.Source.Vertices[i], edge.Target.Vertices[j]))
                                    outline.Add(edge.Source.Vertices[i].ToSkPoint());
                        Debug.Assert(outline.Count == c + 2);
                        var nextId = dict.Count + 1;
                        dict.Add(nextId, new HalfEdge(nextId + 1, outline[outline.Count - 2]));
                        dict.Add(nextId + 1, new HalfEdge(nextId, outline[outline.Count - 1]));
                    }
                }
                _renderData.outlineVertices = outline.ToArray();

                _renderData.m_Paths.Clear();
                while (dict.Count > 0)
                {
                    HalfEdge a = dict.First().Value;
                    HalfEdge b = dict[a.HalfId];
                    dict.Remove(a.HalfId);
                    dict.Remove(b.HalfId);
                    List<SKPoint> points = new List<SKPoint>();
                    points.Add(a.Point);
                    points.Add(b.Point);
                    // in list: 1 segment
                    var newA = dict.FirstOrDefault(x => Ext.AboutEqual(x.Value.Point, a.Point));
                    while (newA.Key != default(int))
                    {
                        a = dict[newA.Value.HalfId];
                        dict.Remove(a.HalfId);
                        dict.Remove(newA.Value.HalfId);
                        points.Insert(0, a.Point);
                        newA = dict.FirstOrDefault(x => Ext.AboutEqual(x.Value.Point, a.Point));
                    }

                    var newB = dict.FirstOrDefault(x => Ext.AboutEqual(x.Value.Point, b.Point));
                    while (newB.Key != default(int))
                    {
                        b = dict[newB.Value.HalfId];
                        dict.Remove(b.HalfId);
                        dict.Remove(newB.Value.HalfId);
                        points.Add(b.Point);
                        newB = dict.FirstOrDefault(x => Ext.AboutEqual(x.Value.Point, b.Point));
                    }


                    SKPath p = new SKPath();
                    p.AddPoly(points.ToArray(), false);
                    _renderData.m_Paths.Add(p);
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

            _renderData.Render(sender, e, _size.Item1, _size.Item2, RenderingOptions);
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
            p.Frequency = Elevation.Freq;

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

    public class RenderingOptions : ViewModel
    {
        private bool _showVertices;
        private bool _showCentroids;
        private bool _fillPolygons;
        public bool ShowVertices
        {
            get { return _showVertices; }
            set
            {
                _showVertices = value;
                OnPropertyChanged();
                triggerRender();
            }
        }

        public bool ShowCentroids
        {
            get { return _showCentroids; }
            set
            {
                _showCentroids = value;
                OnPropertyChanged();
                triggerRender();
            }
        }

        public bool FillPolygons
        {
            get { return _fillPolygons; }
            set
            {
                _fillPolygons = value;
                OnPropertyChanged();
                triggerRender();
            }
        }

        public bool ShowOutline
        {
            get { return _showOutline; }
            set
            {
                _showOutline = value;
                OnPropertyChanged();
                triggerRender();
            }
        }

        public bool ShowNoiseTexture
        {
            get { return _showNoiseTexture; }
            set
            {
                _showNoiseTexture = value;
                OnPropertyChanged();
                triggerRender();
            }
        }

        public bool FillNoisePolygons { get; set; } = true;

        public bool FilterElevation
        {
            get { return _filterElevation; }
            set
            {
                _filterElevation = value;
                OnPropertyChanged();
                triggerRender();
            }
        }

        public bool ShowPolygonCoastline
        {
            get { return _showPolygonCoastline; }
            set
            {
                _showPolygonCoastline = value;
                OnPropertyChanged();
                triggerRender();
            }
        }

        public bool ShowPolygonCoastPath
        {
            get { return _showPolygonCoastPath; }
            set { _showPolygonCoastPath = value;
                OnPropertyChanged();
                triggerRender();
            }
        }

        public bool _showOutline = true;
        private bool _showNoiseTexture;
        private bool _filterElevation;

        private Action triggerRender;
        private bool _showPolygonCoastline;
        private bool _showPolygonCoastPath;

        public RenderingOptions(Action triggerRender)
        {
            this.triggerRender = triggerRender;
        }
    }
}