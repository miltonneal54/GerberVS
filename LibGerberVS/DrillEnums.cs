// DrillEnums.cs - Enumerations for Excellon drill file processing.

/*  Copyright (C) 2015-2019 Milton Neal <milton200954@gmail.com>
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
    internal enum DrillFileSection
    {
        None,
        Header,
        Data
    }

    internal enum DrillCoordinateMode
    {
        Absolute,
        Incremental
    }

    internal enum DrillMCode
    {
        NotImplemented,
        End,
        EndRewind,
        Message,
        LongMessage,
        Header,
        EndHeader,
        Metric,
        Imperial,
        BeginPattern,
        EndPattern,
        CannedText,
        TipCheck,
        Unknown
    }

    internal enum HeaderScale
    {
        MetricHeader,
        ImperialHeader
    }

    internal enum DrillGCode
    {
        Absolute,
        Incrementle,
        ZeroSet,
        Rout,
        Drill,
        LinearMove,
        ClockwiseMove,
        CounterClockwiseMove,
        Slot,
        Unknown
    }

    internal enum DrillNumberFormat
    {
        Format_00_0000,     // Inch.
        Format_000_000,     // 6 digit metric (1uM).
        Format_000_00,      // 5 digit metric (10uM).
        Format_0000_00,     // 6 digit metric (10uM).
        UserDefined
    }

    internal enum ZeroSuppression
    {
        None = 0,
        Leading,
        Trailing
    }

    internal enum Units
    {
        Inch = 0,
        Millimeters
    }

    internal enum HA
    {
        AutoDetect = 0,
        ZeroSuppression,
        XYUnits,
        Digits,
        ToolUnits
    }
}
