using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberVS
{
    public class GerberFormat
    {
        // Auto Properties
        public GerberOmitZero OmitZeros { get; set; }
        public GerberCoordinate Coordinate { get; set; }
        public int IntegralPartX { get; set; }
        public int DecimalPartX { get; set; }
        public int IntegralPartY { get; set; }
        public int DecimalPartY { get; set; }
        public int SequenceNumberLimit { get; set; }    // Length limit for codes of sequence number.
        public int GeneralFunctionLimit { get; set; }   // Length limit for codes of general function.
        public int PlotFunctionLimit { get; set; }      // Length limit for codes of plot function.
        public int MiscFunctionLimit { get; set; }      // Length limit for codes of miscellaneous function.

        public GerberFormat()
        { }
    }
}
