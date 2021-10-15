using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberVS
{
    internal class GerberState
    {
        // Auto properties.
        /// <summary>
        /// Current X location.
        /// </summary>
        public int CurrentX { get; set; }

        /// <summary>
        /// Current Y location.
        /// </summary>
        public int CurrentY { get; set; }

        /// <summary>
        /// Previous X location
        /// </summary>
        public int PreviousX { get; set; }

        /// <summary>
        /// Previous Y location.
        /// </summary>
        public int PreviousY { get; set; }


        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public int CurrentAperture { get; set; }
        public bool ChangedState { get; set; }
        public GerberApertureState ApertureState { get; set; }
        public GerberInterpolation Interpolation { get; set; }
        public GerberInterpolation PreviousInterpolation { get; set; }

        /// <summary>
        /// Start node (net) of a fill region.
        /// </summary>
        public GerberNet RegionStartNode { get; set; }

        public GerberLevel Level { get; set; }
        public GerberNetState NetState { get; set; }

        /// <summary>
        /// Determines if the state is a region fill.
        /// </summary>
        public bool IsRegionFill { get; set; }

        /// <summary>
        /// Multi quadrant state if true, else single quadrant.
        /// </summary>
        public bool MultiQuadrant { get; set; }          // Set true if multi quadrant else single quadrant.

        /// <summary>
        /// Creates a new instance of the gerber state.
        /// </summary>
        public GerberState()
        { }
    }

   
}
