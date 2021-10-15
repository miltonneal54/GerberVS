/* CircleSegment.cs - Class type for defining a circle segment. */

/*  Copyright (C) 2015-2021 Milton Neal <milton200954@gmail.com>
    *** Acknowledgments to Gerbv Authors and Contributors. ***

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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberVS
{
    /// <summary>
    /// Type class for defining gerber circle segments.
    /// </summary>
    public class CircleSegment
    {
        /// <summary>
        /// Center X coordinate.
        /// </summary>
        public double CenterX { get; set; }

        /// <summary>
        /// Center Y coordinate.
        /// </summary>
        public double CenterY { get; set; }

        /// <summary>
        /// Rectangular width of the segment.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Rectangular height of the segment.
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Start angle of the circle segment in degrees.
        /// </summary>
        public double StartAngle { get; set; }

        /// <summary>
        /// End angle of the circle segment in degrees.
        /// </summary>
        public double EndAngle { get; set; }

        /// <summary>
        /// Gets the sweep angle based on the start and end angles.
        /// </summary>
        public double SweepAngle
        {
            get
            {
                if (EndAngle == StartAngle)
                    return 360;

                return (EndAngle - StartAngle);
            }
        }

        /// <summary>
        /// Create a new instance of the circle segment type class.
        /// </summary>
        public CircleSegment()
        { }

        /// <summary>
        /// Create a new instance of the circle segment type class with parameters.
        /// </summary>
        /// <param name="centreX">center x coordinate</param>
        /// <param name="centreY">center y coordinate</param>
        /// <param name="width">rectanglar width</param>
        /// <param name="height">retangular height</param>
        /// <param name="startAngle">circle segment start angle in degrees</param>
        /// <param name="endAngle">circle segment end angle in degrees</param>
        public CircleSegment(double centreX, double centreY, double width, double height, double startAngle, double endAngle)
        {
            CenterX = centreX;
            CenterY = centreY;
            Width = width;
            Height = height;
            StartAngle = startAngle;
            EndAngle = endAngle;
        }
    }
}
