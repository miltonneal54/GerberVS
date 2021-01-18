using System.ComponentModel;
using System.Globalization;

namespace GerberVS
{
    /// <summary>
    /// Represents an ordered pair of double precision coordinates.
    /// </summary>
    public struct PointD 
    {
        //public static readonly PointD Empty = new PointD();
        private double x;
        private double y;

        /// <summary>
        /// Gets or sets the value of the X coordinate.
        /// </summary>
        public double X
        {
            get { return x; }
            set { x = value; }
        }

        /// <summary>
        /// Gets or sets the value of the Y coordinate.
        /// </summary>
        public double Y
        {
            get { return y; }
            set { y = value; }
        }

        /// <summary>
        /// Initialises a new instance of the PointD class with the specified values.
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinal</param>
        public PointD(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Test if the PointD has an x and y value of 0.
        /// </summary>
        [Browsable(false)]
        public bool IsEmpty
        {
            get { return x == 0.0 && y == 0.0; }
        } 

        /// <summary>
        /// Determines whether the coordinates of two points are equal.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns>true if equal</returns>
        public static bool operator == (PointD point1, PointD point2)
        {
            return point1.Equals(point2);
        }

        /// <summary>
        /// Determines whether the coordinates of two points are not equal.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns>true if not equal</returns>
        public static bool operator != (PointD point1, PointD point2)
        {
            return !point1.Equals(point2);
        }

        /// <summary>
        /// Get a hash code for this PointD structure.
        /// </summary>
        /// <returns>has code</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Test to see if object PointD is the same type and dimension as this.
        /// </summary>
        /// <param name="obj">obj to test</param>
        /// <returns>true if equal</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is PointD))
                return false;

            PointD comp = (PointD)obj;
            return comp.x == this.x && comp.y == this.y && comp.GetType().Equals(this.GetType());
        }

        /// <summary>
        /// Gets a string representation of the coordinates.
        /// </summary>
        /// <returns>coordinates as a string value</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{{X = {0}, Y = {1}} }", x, y);
        } 
    }

    /// <summary>
    /// Represents an order pair of double precission numbers typically for height and width of a rectangle.
    /// </summary>
    public struct SizeD
    {
        //public static readonly SizeD Empty = new SizeD();
        private double width;
        private double height;

        /// <summary>
        /// Gets or sets the value of the width coordinate.
        /// </summary>
        public double Width
        {
            get { return width; }
            set { width = value; }
        }

        /// <summary>
        /// Gets or sets the value of the height coordinate.
        /// </summary>
        public double Height
        {
            get { return height; }
            set { height = value; }
        }

        /// <summary>
        /// Initialises a new instance of the SizeD class with the specified dimensions.
        /// </summary>
        /// <param name="width">width</param>
        /// <param name="height">height</param>
        public SizeD(double width, double height)
        {
            this.width = width;
            this.height = height;
        }

        /// <summary>
        /// Test if the SizeD has a width and height value of 0.
        /// </summary>
        [Browsable(false)]
        public bool IsEmpty
        {
            get { return width == 0.0 && height == 0.0; }
        } 

        /// <summary>
        /// Determines whether the coordinates of two sizes are equal.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static bool operator == (SizeD value1, SizeD value2)
        {
            return value1.Equals(value2);
        }

        /// <summary>
        /// Determines whether the coordinates of two sizes are not equal.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static bool operator != (SizeD value1, SizeD value2)
        {
            return !value1.Equals(value2);
        }

        /*public static SizeD Divide(SizeD size, double value)
        {
            return size / value;
        }*/
        /// <summary>
        /// Divide operator.
        /// </summary>
        /// <param name="size">size</param>
        /// <param name="value">divisor</param>
        /// <returns></returns>
        public static SizeD operator / (SizeD size, double value)
        {
            if (value == 0) // Catch divide by zero.
                return new SizeD(0.0, 0.0);

            return new SizeD(size.Width / value, size.Height / value);
        }

        /// <summary>
        /// Get a hash code for this SizeD structure.
        /// </summary>
        /// <returns>has code</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Test to see if object SizeD is the same type and dimension as this.
        /// </summary>
        /// <param name="obj">obj to test</param>
        /// <returns>true if equal</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is SizeD))
                return false;

            SizeD comp = (SizeD)obj;
            return comp.width == this.width && comp.height == this.height && comp.GetType().Equals(this.GetType());
        }

        /// <summary>
        /// Gets a string representation of the coordinates.
        /// </summary>
        /// <returns>coordinates as a string value</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{{Width = {0}, Height = {1}} }", width, height);
        } 
    }
}
