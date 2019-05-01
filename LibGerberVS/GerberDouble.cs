using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberVS
{
    /// <summary>
    /// Represents an ordered pair of double precision coordinates.
    /// </summary>
    public struct PointD
    {
        private double x;
        private double y;

        public double X
        {
            get { return x; }
            set { x = value; }
        }

        public double Y
        {
            get { return y; }
            set { y = value; }
        }

        public PointD(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
    }

    /// <summary>
    /// Represents an order pair of double precission numbers typically for height and width of a rectangle.
    /// </summary>
    public struct SizeD
    {
        private double width;
        private double height;


        public double Width
        {
            get { return width; }
            set { width = value; }
        }

        public double Height
        {
            get { return height; }
            set { height = value; }
        }

        public SizeD(double width, double height)
        {
            this.width = width;
            this.height = height;
        }

        public static SizeD operator / (SizeD size, double value)
        {
            if (value == 0) // Catch divide by zero.
                return new SizeD(0.0, 0.0);

            return new SizeD(size.Width / value, size.Height / value);
        }

    }
}
