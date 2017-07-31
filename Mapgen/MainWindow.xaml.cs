#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Media;
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

        private bool _panning;

        public float scale = 1;
        private Point initTranslation;
        public Vector translation;
        private Size _size = new Size();

        void repaint()
        {
            glhost.Child?.Invalidate();
        }
        public MainWindow()
        {
            InitializeComponent();

            //btnFindDelaunay.IsEnabled = false;
            //btnFindVoronoi.IsEnabled = false;

            _vm = new MainWindowViewModel(repaint);
            DataContext = _vm;

            Title += string.Format(" ({0} points)", _vm.NumberOfVertices);
            //skElement.PaintSurface += _vm.Render;

          
            this.MouseWheel += (_, a) =>
            {
                scale = Clamp(scale * (a.Delta > 0 ? 1.05f : 0.95f), 0.1f, 100);
                RefreshMatrix();
            };
        }

        private void OnGLControlHost(object esender, EventArgs e)
        {
            OpenTK.Toolkit.Init();
            var glControl = new SKGLControl();
            glControl.PaintSurface += (o, args) => _vm.Render(this, args);
            glControl.Dock = System.Windows.Forms.DockStyle.Fill;
            //glControl.Click += OnSampleClicked;
            _size = new Size(glControl.CanvasSize.Width, glControl.CanvasSize.Height);
            if(_size.Width == 0 || _size.Height == 0)
                _size = new Size(800,800);
            var host = (WindowsFormsHost)esender;
            glControl.MouseDown += (sender, args) =>
            {
                _panning = true;
                initTranslation = new Point(args.X, args.Y) - translation;
            };
            glControl.MouseMove += (sender, args) =>
            {
                if (!_panning)
                    return;
                translation = new Point(args.X, args.Y) - initTranslation;
                RefreshMatrix();
            };
            glControl.MouseUp += (sender, args) => _panning = false;
            glControl.MouseWheel += (o, a) =>
            {
                scale = Clamp(scale * (a.Delta > 0 ? 1.05f : 0.95f), 0.1f, 100);
                RefreshMatrix();
            };

            host.Child = glControl;
        }

        private void RefreshMatrix()
        {
            _vm.RefreshMatrix(scale, translation);
            repaint();
        }

        public static double Clamp(double v, double a, double b)
        {
            return v < a ? a : (v > b ? b : v);
        }

        public static float Clamp(float v, float a, float b)
        {
            return v < a ? a : (v > b ? b : v);
        }

        private void btnMakePoints_Click(object sender, RoutedEventArgs e)
        {
            _vm.MakeRandom(_size);
            //btnFindDelaunay.IsEnabled = true;
            //btnFindVoronoi.IsEnabled = true;
            RefreshMatrix();
        }

        private void btnFindDelaunay_Click(object sender, RoutedEventArgs e)
        {
            //btnFindDelaunay.IsEnabled = false;
            //btnFindVoronoi.IsEnabled = true;

            _vm.ComputeDelaunay();
            //_vm.ShowVertices(drawingCanvas.Children);

            //foreach (var cell in _vm.VoronoiMesh.Vertices)
            //{
            //    //drawingCanvas.Children.Add(new Vertex(cell.Centroid.X, cell.Centroid.Y, Brushes.Cyan));
            //}
            RefreshMatrix();
        }

        private void btnFindVoronoi_Click(object sender, RoutedEventArgs e)
        {
            //drawingCanvas.Children.Clear();
            //btnFindVoronoi.IsEnabled = false;
            //btnFindDelaunay.IsEnabled = true;

            //var drawingCanvasChildren = drawingCanvas.Children;
            //_vm.ShowVoronoi(drawingCanvasChildren);

            //_vm.ShowVertices(drawingCanvas.Children);
            //drawingCanvas.Children.Add(new Rectangle { Width = drawingCanvas.ActualWidth, Height = drawingCanvas.ActualHeight, Stroke = Brushes.Black, StrokeThickness = 3 });
        }

        private void btnGenerateElevationNoise_Click(object sender, RoutedEventArgs e)
        {
            _vm.SetDirty(EDirty.ElevationNoise);
            repaint();
        }
    }

    [Flags]
    public enum EDirty
    {
        None = 0,
        Vertices = 1,
        Delaunay = 2,
        ElevationNoise = 4,
        WaterLevel = 8,
    }

    static class Ext
    {
        public static SKPoint ToSkPoint(this MIConvexHull.IVertex v)
        {
            return new SKPoint((float)v.Position[0], (float)v.Position[1]);
        }

        public static bool AboutEqual(double[] a, double[] b)
        {
            return AboutEqual(a[0], b[0]) && AboutEqual(a[1], b[1]);
        }

        public static bool AboutEqual(double x, double y)
        {
            double epsilon = Math.Max(Math.Abs(x), Math.Abs(y)) * 1E-15;
            return Math.Abs(x - y) <= epsilon;
        }

        internal static bool AboutEqual(Vertex vertex1, Vertex vertex2)
        {
            return AboutEqual(vertex1.Position, vertex2.Position);
        }
    }
}