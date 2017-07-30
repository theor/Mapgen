using System;
using System.Linq;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace Mapgen
{
    public class RenderingData
    {
        public SKMatrix matrix = SKMatrix.MakeIdentity();

        public SKPoint[] SkVertices = new SKPoint[0];
        public SKPoint[] delaunayvertices = new SKPoint[0];
        public SKColor[] delaunayColors = new SKColor[0];
        public SKPoint[] centroids = new SKPoint[0];
        public SKBitmap noiseBitmap;
        public SKColor[] noiseColors = new SKColor[0];
        private SKColorFilter _noiseColorFilter;

        public void Render(object a, SKPaintGLSurfaceEventArgs args, int w, int h,  RenderingOptions options)
        {
            var c = args.Surface.Canvas;
            c.Clear(SKColors.Black);
            //using (var p = new SKPaint { Color = SKColors.Red })
            //    c.DrawCircle(w / 2, h / 2, Math.Min(w / 2, h / 2), p);
            var f = 1.0 / Math.Min(w / 2, h / 2);
            c.SetMatrix(matrix);
            using (var p = new SKPaint{Color = SKColors.DarkBlue,IsStroke = true,StrokeWidth = 0.7f,StrokeCap = SKStrokeCap.Round})
            {
                if (options.FillPolygons)
                    c.DrawVertices(SKVertexMode.Triangles, delaunayvertices, delaunayColors, p);
                else if (options.ShowNoiseTexture && noiseBitmap != null)
                {

                    using (var p2 = new SKPaint { ColorFilter = _noiseColorFilter })
                        c.DrawBitmap(noiseBitmap, new SKRect(0, 0, w, h), _noiseColorFilter == null || !options.FilterElevation ? null : p2);
                }
                else if(options.FillNoisePolygons && noiseColors.Length == delaunayvertices.Length)
                    c.DrawVertices(SKVertexMode.Triangles, delaunayvertices, noiseColors, p);
                if (options.ShowOutline)
                    c.DrawVertices(SKVertexMode.Triangles, delaunayvertices, null, p);
            }
            if (options.ShowVertices)
            {
                using (var p = new SKPaint{Color = SKColors.Cyan,IsStroke = true,StrokeWidth = 2})
                {
                    c.DrawPoints(SKPointMode.Points, SkVertices, p);

                }
            }
            if (options.ShowCentroids)
            {
                using (var p = new SKPaint { Color = SKColors.Red, IsStroke = true, StrokeWidth = 2 })
                {
                    c.DrawPoints(SKPointMode.Points, centroids, p);
                }
            }
        }

        public void SetupNoiseColorFilter(int waterLevel)
        {
            byte[] bb = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                bb[i] = (byte) (i < waterLevel ? 0 : 255);
            }
            if (_noiseColorFilter != null)
                _noiseColorFilter.Dispose();
            _noiseColorFilter = SKColorFilter.CreateTable(bb);
        }
    }
}