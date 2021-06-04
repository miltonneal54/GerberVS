/* DrillState.cs - Class type for holding the current drill state data. */

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
    internal class DrillState
    {
        public double CurrentX { get; set; }
        public double CurrentY { get; set; }
        public int CurrentTool { get; set; }
        public DrillFileSection CurrentSection { get; set; }
        public DrillCoordinateMode CoordinateMode { get; set; }
        public double OriginX { get; set; }
        public double OriginY { get; set; }
        public GerberUnit Unit { get; set; }
        public DrillNumberFormat DataNumberFormat { get; set; }
        public DrillNumberFormat HeaderNumberFormat { get; set; }
        public DrillNumberFormat BackupNumberFormat { get; set; }
        public bool AutoDetect { get; set; }
        public int DecimalPlaces { get; set; }

        // Constructor.
        public DrillState()
        {
            CurrentSection = DrillFileSection.None;
            CoordinateMode = DrillCoordinateMode.Absolute;
            OriginX = 0.0;
            OriginY = 0.0;
            Unit = GerberUnit.Unspecified;
            BackupNumberFormat = DrillNumberFormat.Format_000_000;                       // Only used for metric.
            DataNumberFormat = HeaderNumberFormat = DrillNumberFormat.Format_00_0000;    // Inch.
            AutoDetect = true;
            DecimalPlaces = 4;
        }
    }
}
