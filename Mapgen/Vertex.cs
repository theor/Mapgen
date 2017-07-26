using System.Windows;
using System.Windows.Shapes;

namespace Mapgen
{
    using MIConvexHull;
    using System.Windows.Media;
    /// <summary>
    /// A vertex is a simple class that stores the postion of a point, node or vertex.
    /// </summary>
    public class Vertex : Shape, IVertex
    {

        protected override Geometry DefiningGeometry => new EllipseGeometry
        {
            Center = new Point(Position[0], Position[1]),
            RadiusX = 1.5,
            RadiusY = 1.5
        };

        public Vertex(Brush fill = null)
        {
            Fill = fill ?? Brushes.Red;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vertex"/> class.
        /// </summary>
        /// <param name="x">The x position.</param>
        /// <param name="y">The y position.</param>
        /// <param name="fill"></param>
        public Vertex(double x, double y, Brush fill = null)
            : this(fill)
        {
            Position = new[] { x, y };
        }

        public Point ToPoint()
        {
            return new Point(Position[0], Position[1]);
        }

        /// <summary>
        /// Gets or sets the Z. Not used by MIConvexHull2D.
        /// </summary>
        /// <value>The Z position.</value>
        // private double Z { get; set; }

        /// <summary>
        /// Gets or sets the coordinates.
        /// </summary>
        /// <value>The coordinates.</value>
        public double[] Position { get; set; }
    }
}
