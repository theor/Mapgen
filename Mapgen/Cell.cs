/*************************************************************************
 *     This file & class is part of the MIConvexHull Library Project. 
 *     Copyright 2010 Matthew Ira Campbell, PhD.
 *
 *     MIConvexHull is free software: you can redistribute it and/or modify
 *     it under the terms of the MIT License as published by
 *     the Free Software Foundation, either version 3 of the License, or
 *     (at your option) any later version.
 *  
 *     MIConvexHull is distributed in the hope that it will be useful,
 *     but WITHOUT ANY WARRANTY; without even the implied warranty of
 *     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *     MIT License for more details.
 *  
 *     You should have received a copy of the MIT License
 *     along with MIConvexHull.
 *     
 *     Please find further details and contact information on GraphSynth
 *     at https://designengrlab.github.io/MIConvexHull/
 *************************************************************************/

using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using MIConvexHull;
using SkiaSharp;

namespace Mapgen
{
    /// <summary>
    /// A vertex is a simple class that stores the postion of a point, node or vertex.
    /// </summary>
    public class Cell : TriangulationCell<Vertex, Cell>
    {
        private static Random s_Rnd = new Random();

        public Brush Brush { get; set; }

        public class FaceVisual : Shape
        {
            private Cell _f;

            private Geometry _geometry;
            protected override Geometry DefiningGeometry
            {
                get
                {
                    if (_geometry != null) return _geometry;

                    var myPathGeometry = new PathGeometry();
                    var pathFigure1 = new PathFigure
                    {
                        StartPoint = new Point(_f.Vertices[0].Position[0], _f.Vertices[0].Position[1])
                    };
                    for (int i = 1; i < 3; i++)
                    {
                        pathFigure1.Segments.Add(
                            new LineSegment(
                                new Point(_f.Vertices[i].Position[0],
                                          _f.Vertices[i].Position[1]), true)
                            { IsSmoothJoin = true });
                    }
                    pathFigure1.IsClosed = true;
                    myPathGeometry.Figures.Add(pathFigure1);

                    Fill = _f.Brush;
                    _geometry = myPathGeometry;
                    return _geometry;
                }
            }

            public FaceVisual(Cell f)
            {
                Stroke = Brushes.Cyan;
                StrokeThickness = 0.2;
                Opacity = 1.0;
                _f = f;

                var fill = new SolidColorBrush(Color.FromRgb((byte)s_Rnd.Next(255), (byte)s_Rnd.Next(255), (byte)s_Rnd.Next(255)));
                f.Brush = fill;
            }
        }

        private static double Det(double[,] m)
        {
            return m[0, 0] * (m[1, 1] * m[2, 2] - m[2, 1] * m[1, 2]) - m[0, 1] * (m[1, 0] * m[2, 2] - m[2, 0] * m[1, 2]) + m[0, 2] * (m[1, 0] * m[2, 1] - m[2, 0] * m[1, 1]);
        }

        private static double LengthSquared(double[] v)
        {
            double norm = 0;
            for (int i = 0; i < v.Length; i++)
            {
                var t = v[i];
                norm += t * t;
            }
            return norm;
        }

        private Point GetCircumcenter()
        {
            // From MathWorld: http://mathworld.wolfram.com/Circumcircle.html

            var points = Vertices;

            double[,] m = new double[3, 3];

            // x, y, 1
            for (int i = 0; i < 3; i++)
            {
                m[i, 0] = points[i].Position[0];
                m[i, 1] = points[i].Position[1];
                m[i, 2] = 1;
            }
            var a = Det(m);

            // size, y, 1
            for (int i = 0; i < 3; i++)
            {
                m[i, 0] = LengthSquared(points[i].Position);
            }
            var dx = -Det(m);

            // size, x, 1
            for (int i = 0; i < 3; i++)
            {
                m[i, 1] = points[i].Position[0];
            }
            var dy = Det(m);

            // size, x, y
            for (int i = 0; i < 3; i++)
            {
                m[i, 2] = points[i].Position[1];
            }
            //var c = -Det(m);

            var s = -1.0 / (2.0 * a);
            //var r = Math.Abs(s) * Math.Sqrt(dx * dx + dy * dy - 4 * a * c);
            return new Point(s * dx, s * dy);
        }

        private SKPoint GetCentroid()
        {
            return new SKPoint((float) Vertices.Select(v => v.Position[0]).Average(), (float) Vertices.Select(v => v.Position[1]).Average());
        }

        public Shape Visual { get; }
        private Point? _circumCenter;
        public Point Circumcenter
        {
            get
            {
                _circumCenter = _circumCenter ?? GetCircumcenter();
                return _circumCenter.Value;
            }
        }

        private SKPoint? _centroid;
        public SKPoint Centroid
        {
            get
            {
                _centroid = _centroid ?? GetCentroid();
                return _centroid.Value;
            }
        }

        public Cell()
        {
            Visual = new FaceVisual(this);
        }
    }
}
