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

    // NOTE: keep DrillMCode in actual M code order.
    internal enum DrillMCode
    {
        Unknown = -1,

        End = 0,
        EndPattern,
        RepeatPatternOffset,

        OptionalStop = 6,

        StepAndRepeatEnd = 8,	/* Step and repeat */
        StopInspection,

        ZAxisRoutePostionDepth = 14,
        ZAxisRoutePosition,
        RetractClamping,
        RetractNoClamping,
        ToolTipCheck,

        BeginPattern = 25,
        EndRewind = 30,
        LongMessage = 45,

        Message = 47,
        Header,

        VisualStepAndRepeatPattern = 50,	/* Visual step and repeat */
        VisualPatternRepeat,
        VisualStepAndRepeatPatternOffset,

        ReferenceScaling = 60,	/* Reference scaling */
        ReferenceScalingEnd,
        PeckDrilling,
        PeckDrillingEnd,

        SwapAxis = 70,
        Metric,
        Imperial,

        MirrorX = 80,
        MirrorY = 90,
        EndHeader = 95,

        CannedTextX = 97,
        CannedTextY,
        UserDefinedPattern,
    }

  /*  internal enum DrillMCode
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
    }*/

    internal enum HeaderScale
    {
        MetricHeader,
        ImperialHeader
    }

    // NOTE: keep DrillGCode in actual G code order.
    internal enum DrillGCode
    {
        /// <summary>
        /// Unknown drill G code.
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// Route mode
        /// </summary>
        Rout = 0,

        /// <summary>
        /// Linear (straight line) mode.
        /// </summary>
        LinearMove,

        /// <summary>
        /// Circular clockwise mode.
        /// </summary>
        ClockwiseMove,

        /// <summary>
        /// Circular counter clockwise mode.
        /// </summary>
        CounterClockwiseMove,

        /// <summary>
        /// Variable dwell.
        /// </summary>
        VariableDwell,

        /// <summary>
        /// Drill mode.
        /// </summary>
        Drill,

        /// <summary>
        /// Override current tool feed or speed.
        /// </summary>
        OverrideDrillSpeed = 7,

        /// <summary>
        /// Routed circle canned cycle clockwise
        /// </summary>
        RoutCircleClockwise = 32,

        /// <summary>
        /// Routed circle canned cycle counter clockwise.
        /// </summary>
        RoutCircleCounterClockwise,

        /// <summary>
        /// Select vision tool.
        /// </summary>
        VisionTool,

        /// <summary>
        /// Single point vision offset.
        /// </summary>
        VisionSinglePointOffset,

        /// <summary>
        /// Multipoint vision translation.
        /// </summary>
        VisionMultiPointTranslate,

        /// <summary>
        /// Cancel vision translation or offset.
        /// </summary>
        VisionCancel,

        /// <summary>
        /// Vision corrected single hole drilling.
        /// </summary>
        VisionCorrectHoleDrill,

        /// <summary>
        /// Vision system autocalibration.
        /// </summary>
        VisionAutoCalibrate,

        /// <summary>
        /// Cutter compensation off.
        /// </summary>
        CutterCompensateOff,

        /// <summary>
        /// Cutter compensation left.
        /// </summary>
        CutterCompensateLeft,

        /// <summary>
        /// Cutter compensation right.
        /// </summary>
        CutterCompensateRight,

        /// <summary>
        /// Single point vision offset relative to G35 or G36.
        /// </summary>
        VisionSinglePointOffsetRelative = 45,

        /// <summary>
        /// Multipoint vision translation relative to G35 or G36
        /// </summary>
        VisionMultiPointTranslateRelative,

        /// <summary>
        /// Cancel vision translation or offset from G45 or G46
        /// </summary>
        VisionCancelRelative,

        /// <summary>
        /// Vision corrected single hole drilling relative to G35 or G36
        /// </summary>
        VisionCorrectHoleDrillRelative,

        /// <summary>
        /// Dual in line package, same to G82 in Format 2.
        /// </summary>
        PackDip2 = 81,

        /// <summary>
        /// Dual in line package.
        /// </summary>
        PackDip,

        /// <summary>
        /// Eight pin L pack.
        /// </summary>
        Pack8PinL,

        /// <summary>
        /// Canned circle.
        /// </summary>
        Circle,

        /// <summary>
        /// Canned slot.
        /// </summary>
        Slot,

        /// <summary>
        /// Routed slot canned cycle.
        /// </summary>
        RoutSlot = 87,

        /// <summary>
        /// Absolute input mode.
        /// </summary>
        Absolute = 90,

        /// <summary>
        /// Incremental input mode.
        /// </summary>
        Incrementle,

        /// <summary>
        /// Sets work zero relative to absolute zero.
        /// </summary>
        ZeroSet = 93,
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
