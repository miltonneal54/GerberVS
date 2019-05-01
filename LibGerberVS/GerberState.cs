using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberVS
{
    public class GerberState
    {
        // Auto properties.
        public int CurrentX { get; set; }
        public int CurrentY { get; set; }
        public int PreviousX { get; set; }
        public int PreviousY { get; set; }
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public int CurrentAperture { get; set; }
        public bool ChangedState { get; set; }
        public GerberApertureState ApertureState { get; set; }
        public GerberInterpolation Interpolation { get; set; }
        public GerberInterpolation PreviousInterpolation { get; set; }
        public GerberNet PolygonAreaStartNode { get; set; }
        public GerberLevel Level { get; set; }
        public GerberNetState NetState { get; set; }
        public bool IsPolygonAreaFill { get; set; }
        public bool MultiQuadrant { get; set; }          // Set true if multi quadrant else single quadrant.

        public GerberState()
        {}
    }

   
}
