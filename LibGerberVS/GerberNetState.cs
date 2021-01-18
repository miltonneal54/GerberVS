using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberVS
{
    /// <summary>
    /// Class for defining the current net state.
    /// </summary>
    /// <remarks>
    /// If any of these values change during file parsing
    /// a new net state is created reflecting the changes
    /// and added to the net state list.
    /// </remarks>
    public class GerberNetState
    {
        public GerberAxisSelect AxisSelect { get; set; }    // The AB to XY coordinate mapping (refer to RS274X spec)
        public GerberMirrorState MirrorState { get; set; }  // Any mirroring around the X or Y axis.
        public GerberUnit Unit { get; set; }                // The current length unit.
        public double OffsetA { get; set; }                 // Offset along the A axis (usually this is the X axis)
        public double OffsetB { get; set; }                 // Offset along the B axis (usually this is the Y axis)
        public double ScaleA { get; set; }                  // Scale factor in the A axis (usually this is the X axis)
        public double ScaleB { get; set; }                  // Scale factor in the B axis (usually this is the Y axis)

        /// <summary>
        /// Creates a new gerber net state and intialize with defaults.
        /// </summary>
        public GerberNetState(GerberImage gerberImage)
        {
            Unit = GerberUnit.Inch;
            OffsetA = 0.0;
            OffsetB = 0.0;
            ScaleA = 1.0;
            ScaleB = 1.0;
            gerberImage.NetStateList.Add(this);
        }
    }
}
