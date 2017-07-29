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

        public void Render(object a, SKPaintGLSurfaceEventArgs args, bool showVertices, bool showOutline, bool showCentroids, bool fillPolygons, bool showNoise)
        {
            (float w, float h) = (args.RenderTarget.Width, args.RenderTarget.Height);
            var c = args.Surface.Canvas;
            c.Clear(SKColors.Black);
            //using (var p = new SKPaint { Color = SKColors.Red })
            //    c.DrawCircle(w / 2, h / 2, Math.Min(w / 2, h / 2), p);
            var f = 1.0 / Math.Min(w / 2, h / 2);
            c.SetMatrix(matrix);

            using (var p = new SKPaint{Color = SKColors.DarkBlue,IsStroke = true,StrokeWidth = 0.7f,StrokeCap = SKStrokeCap.Round})
            {
                if (fillPolygons)
                    c.DrawVertices(SKVertexMode.Triangles, delaunayvertices, delaunayColors, p);
                else if (showNoise && noiseBitmap != null)
                {
                    c.DrawBitmap(noiseBitmap, args.RenderTarget.Rect);
                }
                if (showOutline)
                    c.DrawVertices(SKVertexMode.Triangles, delaunayvertices, null, p);
            }
            if (showVertices)
            {
                using (var p = new SKPaint{Color = SKColors.Cyan,IsStroke = true,StrokeWidth = 2,StrokeCap = SKStrokeCap.Round})
                {
                    c.DrawPoints(SKPointMode.Points, SkVertices, p);
                }
            }
            if (showCentroids)
            {
                using (var p = new SKPaint { Color = SKColors.Red, IsStroke = true, StrokeWidth = 2, StrokeCap = SKStrokeCap.Round })
                {
                    c.DrawPoints(SKPointMode.Points, centroids, p);
                }
            }
        }
    }
}