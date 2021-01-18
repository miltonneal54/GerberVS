using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberVS
{
    /// <summary>
    /// Class for defining the gerber net.
    /// </summary>
    public class GerberNet
    {
        #region Auto properties.
        /// <summary>
        /// X coordinate of the start point.
        /// </summary>
        public double StartX { get; set; }
        /// <summary>
        /// Y coordinate of the start point.
        /// </summary>
        public double StartY { get; set; }
        /// <summary>
        /// X coordinate of the end point.
        /// </summary>
        public double StopX { get; set; }
        /// <summary>
        /// Y coordinate of the end point.
        /// </summary>
        public double StopY { get; set; }
        /// <summary>
        /// Bounding size containing this net.
        /// </summary>
        /// <remarks>
        /// Used for rendering optimizations.
        /// </remarks>
        public BoundingBox BoundingBox { get; set; }
        /// <summary>
        /// Index of the aperture used for this entity.
        /// </summary>
        public int Aperture { get; set; }
        /// <summary>
        /// State of the aperture tool (on/off/etc).
        /// </summary>
        public GerberApertureState ApertureState { get; set; }
        /// <summary>
        /// Path interpolation method (linear/etc).
        /// </summary>
        public GerberInterpolation Interpolation { get; set; }
        /// <summary>
        /// Information for arc nets.
        /// </summary>
        public CircleSegment CircleSegment { get; set; }
        /// <summary>
        /// Label for this net.
        /// </summary>
        public string Label { get; set; }
        /// <summary>
        /// RS274X level this net belongs to.
        /// </summary>
        public GerberLevel Level { get; set; }
        /// <summary>
        /// RS274X state this net belongs to.
        /// </summary>
        public GerberNetState NetState { get; set; }

        #endregion

        /// <summary>
        /// Creates a new instance of a gerber net and appends it to the net list.
        /// </summary>
        /// <param name="gerberImage">image containing the net</param>
        public GerberNet(GerberImage gerberImage)
        {
            Level = new GerberLevel(gerberImage);         // Create the first level filled with some default values.
            NetState = new GerberNetState(gerberImage);   // Create the first netState.
            Label = String.Empty;
            gerberImage.GerberNetList.Add(this);
        }

        /// <summary>
        /// Creates a new instance of a gerber net and appends it to the net list.
        /// </summary>
        /// <param name="gerberImage">image containing the net</param>
        /// <param name="currentNet">the current gerber net</param>
        /// <param name="level">level infomation for the new net </param>
        /// <param name="netState">state information for the new net</param>
        public GerberNet(GerberImage gerberImage, GerberNet currentNet, GerberLevel level, GerberNetState netState)
        {
            if (level != null)
                Level = level;

            else
                Level = currentNet.Level;

            if (netState != null)
                NetState = netState;

            else
                NetState = currentNet.NetState;

            Label = String.Empty;
            gerberImage.GerberNetList.Add(this);
        }
    }
}
