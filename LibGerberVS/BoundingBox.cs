/* BoundingBox.cs - Class for creating a bounding area */

/*  Copyright (C) 2015-2018 Milton Neal <milton200954@gmail.com>

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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberVS
{
    /// <summary>
    /// Defines the bounding size of Gerber Net objects.
    /// </summary>
    /// <remarks>
    /// A bounding box consisting of double "min" and "max" values is deemed empty.
    /// If any of the the values are a double "min" or "max" is it also deemed to be invalid.
    /// </remarks>
    public class BoundingBox
    {
        // Auto properties.
        public double Left { get; set; }     // The X coordinate of the left side.
        public double Top { get; set; }      // The Y coordinate of the top side.
        public double Right { get; set; }    // The X coordinate of the right side.
        public double Bottom { get; set; }   // The Y coordinate of the bottom side.

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
        /// Intializes a new instance of BoundingBbox with specified parameters.
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
        /// Determines if the bounding box contains all valid values.
        /// </summary>
        /// <returns>true if all bounding box values are valid</returns>
        public bool IsValid()
        {
            if (Left == double.MaxValue || Top == double.MinValue || Right == double.MinValue || Bottom == double.MaxValue)
                return false;

            return true;
        }

        public bool Contains(PointD point)
        {
            if (point.X >= this.Left && point.X <= this.Right && point.Y >= this.Bottom && point.Y <= this.Top)
                return true;

            return false;
        }

        public bool Contains(BoundingBox box)
        {
            if (Contains(new PointD(box.Left, box.Bottom)) && Contains(new PointD(box.Right, box.Top)))
                return true;

            return false;
        }
    }
}
