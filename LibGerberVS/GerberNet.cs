using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberVS
{
    public class GerberNet
    {
        // Auto properties.
        public double StartX { get; set; }                      // X coordinate of the start point.
        public double StartY { get; set; }                      // Y coordinate of the start point.
        public double StopX { get; set; }                       // X coordinate of the end point.
        public double StopY { get; set; }                       // Y coordinate of the end point.
        public BoundingBox BoundingBox { get; set; }            // Bounding size containing this net (used for rendering optimizations)
        //public bool IsSelected { get; set; }                    // True if this net is in a selection.
        public int Aperture { get; set; }                       // Index of the aperture used for this entity.
        public GerberApertureState ApertureState { get; set; }  // State of the aperture tool (on/off/etc)
        public GerberInterpolation Interpolation { get; set; }  // Path interpolation method (linear/etc)
        public CircleSegment CircleSegment { get; set; }        // Information for arc nets.
        public string Label { get; set; }                       // Label for this net.
        public GerberLevel Level { get; set; }                  // RS274X level this net belongs to.
        public GerberNetState NetState { get; set; }            // RS274X state this net belongs to.

        public GerberNet()
        { }

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
            //IsSelected = false;
            gerberImage.GerberNetList.Add(this);
        }
    }
}
