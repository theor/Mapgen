#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using MIConvexHull;
using SkiaSharp;

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

        public MainWindow()
        {
            InitializeComponent();

            //btnFindDelaunay.IsEnabled = false;
            //btnFindVoronoi.IsEnabled = false;
            
            _vm = new MainWindowViewModel(() => skElement.InvalidateVisual());
            DataContext = _vm;

            Title += string.Format(" ({0} points)", _vm.NumberOfVertices);
            skElement.PaintSurface += _vm.Render;

            this.MouseDown += (sender, args) =>
            {
                _panning = true;
                initTranslation = args.GetPosition(this) - translation;

                skElement.InvalidateVisual();
            };
            this.MouseMove += (sender, args) =>
            {
                if (!_panning)
                    return;
                translation = args.GetPosition(this) - initTranslation;
                RefreshMatrix();
            };
            this.MouseUp += (sender, args) => _panning = false;
            this.MouseWheel += (_, a) =>
            {
                scale = Clamp(scale * (a.Delta > 0 ? 1.05f : 0.95f), 0.1f, 100);
                RefreshMatrix();
            };
        }

        private void RefreshMatrix()
        {
            _vm.RefreshMatrix(scale, translation);
            skElement.InvalidateVisual();
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
            _vm.MakeRandom(new Size(skElement.CanvasSize.Width, skElement.CanvasSize.Height));
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
            skElement.InvalidateVisual();
        }
    }

    [Flags]
    public enum EDirty
    {
        None = 0,
        Vertices = 1,
        Delaunay = 2,
        ElevationNoise = 4,
    }

    static class Ext
    {
        public static SKPoint ToSkPoint(this MIConvexHull.IVertex v)
        {
            return new SKPoint((float)v.Position[0], (float)v.Position[1]);
        }
    }
}