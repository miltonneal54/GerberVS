/* BoundingBox.cs - Class for creating a bounding area. */

/*  Copyright (C) 2015-2021 Milton Neal <milton200954@gmail.com>

    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions
    are met:

    1. Redistributions of source code must retain the above copyright
       notice, this list of conditions and the following disclaimer.
    2. Redistributions in binary form must reproduce the above copyright
       notice, this list of conditions and the following disclaimer in the
       documentation and/or other materials provided with the distribution.
    3. Neither the name of the project nor the names of its contributors
       may be used to endorse or promote products derived from this software
       without specific prior written permission.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
    ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
    IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
    ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
    FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
    DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
    OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
    HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
    LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
    OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
    SUCH DAMAGE.
 */

using System;
using System.Drawing;

namespace GerberVS
{
    /// <summary>
    /// Defines the bounding size of Gerber Net objects.
    /// </summary>
    /// <remarks>
    /// A bounding box with any values consisting of double "MinValue" and "MaxValue" is deemed invalid.
    /// </remarks>
    public class BoundingBox
    {
        // Auto properties.
        /// <summary>
        /// The X coordinate of the left side of the bounds.
        /// </summary>
        public double Left { get; set; } 

        /// <summary>
        /// The Y coordinate of the top of the bounds.
        /// </summary>
        public double Top { get; set; }
 
        /// <summary>
        /// The X coordinate of the right side of the bounds.
        /// </summary>
        public double Right { get; set; }

        /// <summary>
        /// The Y coordinate of the bottom of the bounds.
        /// </summary>
        public double Bottom { get; set; }

        /// <summary>
        /// Intializes a new instance of BoundingBox with empty parameters.
        /// </summary>
        public BoundingBox()
         {
            Left = double.MaxValue;
            Top = double.MinValue;
            Right = double.MinValue;
            Bottom = double.MaxValue;
        }

        /// <summary>
        /// Intializes a new instance of BoundingBox with specified parameters.
        /// </summary>
        /// <param name="left">left value</param>
        /// <param name="right">right value</param>
        /// <param name="bottom">botton value</param>
        /// <param name="top">top value</param>
        public BoundingBox(double left, double top, double right, double bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>
        /// Determines if the bounding box is a valid size.
        /// </summary>
        /// <returns>true if all bounding box values are valid</returns>
        public bool IsValid()
        {
            if (Left == double.MaxValue || Top == double.MinValue || Right == double.MinValue || Bottom == double.MaxValue)
                return false;

            return true;
        }

        /// <summary>
        /// Determines if the specified point is contained within the bounds of this bounding box structure.
        /// </summary>
        /// <param name="point">the point to test</param>
        /// <returns>true if within the bounds</returns>
        public bool Contains(PointD point)
        {
            if (point.X >= this.Left && point.X <= this.Right && point.Y >= this.Bottom && point.Y <= this.Top)
                return true;

            return false;
        }

        /// <summary>
        /// Determines if the specified bounding box is contained within the bounds of this bounding box structure.
        /// </summary>
        /// <param name="boundingBox">the bounding box to test</param>
        /// <returns>true if within the bounds</returns>
        public bool Contains(BoundingBox boundingBox)
        {
            if (Contains(new PointD(boundingBox.Left, boundingBox.Bottom)) && Contains(new PointD(boundingBox.Right, boundingBox.Top)))
                return true;

            return false;
        }

        /// <summary>
        /// Creates RectangleF structure from the bounding box parameters.
        /// </summary>
        /// <returns>returns the constructed rectangle</returns>
        public RectangleF ToRectangle()
        {
            return new RectangleF((float)Left, (float)Bottom, (float)(Math.Abs(Right - Left)), (float)(Math.Abs(Bottom - Top)));
        }
    }
}
