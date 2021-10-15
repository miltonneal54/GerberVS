/* GerberEnum.cs - Gerber enumations. */

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
    /// Op codes used when parsing aperture macros.
    /// </summary>
    internal enum GerberOpCode
    {
        Nop = 0,            // No operation.
        Push,               // Push the instruction onto the stack.
        PushParameter,      // Push parameter onto stack.
        PopParameter,       // Pop parameter from stack.
        Add,                // Mathmatical add operation.
        Subtract,           // Mathmatical subtract operation.
        Multiple,           // Mathmatical multiply operation.
        Divide,             // Mathmatical divide operation.
        Primitive           // Draw macro primative.
    }

    /// <summary>
    /// Error message types used in GerberVS Library.
    /// </summary>
    public enum GerberErrorType
    {
        GerberCritical = 0,    // File processing can not continue.
        GerberError,           // Something went wrong, but processing can still continue.
        GerberWarning,         // Something was encountered that may provide the wrong output.
        GerberNote             // An irregularity was encountered, but needs no intervention.
    }

    /// <summary>
    /// Enumeration of aperture types.
    /// </summary>
    public enum GerberApertureType
    {
        /// <summary>
        /// No aperture used.
        /// </summary>
        None,

        /// <summary>
        /// Round aperture.
        /// </summary>
        Circle,

        /// <summary>
        /// Rectangular aperture.
        /// </summary>
        Rectangle,

        Oval,             // an ovular (obround) aperture.
        Polygon,          // a polygon aperture.
        Macro,            // a RS274X macro.
        MacroCircle,     // a RS274X circle macro.
        MacroOutline,    // a RS274X outline macro.
        MacroPolygon,    // a RS274X polygon macro.
        MacroMoire,      // a RS274X moire macro.
        MacroThermal,    // a RS274X thermal macro.
        MacroLine20,     // a RS274X line (code 20) macro.
        MacroLine21,     // a RS274X line (code 21) macro.
        MacroLine22      // a RS274X line (code 22) macro.
    }

    // The current state of the aperture drawing tool.
    public enum GerberApertureState
    {
        Off,   // tool drawing is off, and nothing will be drawn.
        On,    // tool drawing is on, and something will be drawn.
        Flash,  // tool is flashing, and will draw a single aperture.
        Deleted
    }

    // The current unit used.
    public enum GerberUnit
    {
        /// <summary>
        /// Inches.
        /// </summary>
        Inch,

        /// <summary>
        /// Millimeters.
        /// </summary>
        Millimeter,

        /// <summary>
        /// Unspecified, use default units.
        /// </summary>
        Unspecified
    }

    // the different drawing polarities available.
    public enum GerberPolarity
    {
        Positive,   // draw "positive", using the current level's polarity.
        Negative,   // draw "negative", reversing the current level's polarity.
        Dark,       // add to the current rendering.
        Clear       // subtract from the current rendering.
    }

    /// <summary>
    /// Decimal point parsing style used.
    /// </summary>
    public enum GerberOmitZero
    {
        /// <summary>
        /// Omit extra zeros before the decimal point.
        /// </summary>
        OmitZerosLeading,
 
        /// <summary>
        /// Omit extra zeros after the decimal point.
        /// </summary>
        OmitZerosTrailing,

        /// <summary>
        /// Explicitly specify how many decimal places are used.
        /// </summary>
        OmitZerosExplicit,

        /// <summary>
        /// Use the default parsing style.
        /// </summary>
        OmitZerosUnspecified
    }

    /// <summary>
    /// The coordinate system used.
    /// </summary>
    public enum GerberCoordinate
    {
        /// <summary>
        /// All coordinates are absolute from a common origin.
        /// </summary>
        Absolute,

        /// <summary>
        /// All coordinates are relative to the previous coordinate.
        /// </summary>
        Incremental
    }

    // The interpolation methods available.
    public enum GerberInterpolation
    {
        LinearX1,                   // draw a line.
        LinearX10,                  // draw a line.
        LinearX01,                  // draw a line.
        LinearX001,                 // draw a line.
        ClockwiseCircular,          // draw an arc in the clockwise direction.
        CounterClockwiseCircular,   // draw an arc in the counter-clockwise direction.
        /// <summary>
        /// Start polygon region draw.
        /// </summary>
        RegionStart,
        /// <summary>
        /// End polygon region draw.
        /// </summary>
        RegionEnd,        
        Deleted                     // the net has been deleted by the user, and will not be drawn.
    }

    public enum GerberEncoding
    {
        None,
        ASCII,
        EBCDIC,
        BCD,
        ISOASCII,
        EIA
    }

    // The different file types used.
    public enum GerberFileType
    {
        RS274X,        // the file is a RS274X file.
        Drill,         // the file is an Excellon drill file.
       // PickAndPlace   // the file is a CSV pick and place file.
    }

    public enum GerberKnockoutType
    {
        NoKnockout,
        FixedKnockout,
        Border
    }

    public enum GerberMirrorState
    {
        None,
        FlipA,
        FlipB,
        FlipAB
    }

    public enum GerberAxisSelect
    {
        None,
        SwapAB
    }

    public enum GerberImageJustifyType
    {
        None,
        LowerLeft,
        Centre
    }

    // The different selection modes available.
    public enum GerberSelection
    {
        None,      // the selection buffer is empty.
        PointClick, // the user clicked on a single point.
        DragBox     // the user dragged a box to encompass one or more objects.
    }

    /// <summary>
    /// Enumerates the circlular aperture parameter indexes.
    /// </summary>
    internal enum CircleParameters : int
    {
        Exposure,
        Diameter,
        CentreX,
        CentreY,
        Rotation
    }

    /// <summary>
    /// Enumerates the outline aperture parameter indexes.
    /// </summary>
    internal enum OutlineParameters : int
    {
        Exposure,
        NumberOfPoints,
        FirstX,
        FirstY,
        Rotation
    }

    internal enum PolygonParameters : int
    {
        Exposure,
        NumberOfSides,
        CentreX,
        CentreY,
        Diameter,
        Rotation
    }

    internal enum MoireParameters : int
    {
        CentreX,
        CentreY,
        OutsideDiameter,
        CircleLineWidth,
        GapWidth,
        NumberOfCircles,
        CrosshairLineWidth,
        CrosshairLength,
        Rotation
    }

    internal enum ThermalParameters : int
    {
        CentreX,
        CentreY,
        OutsideDiameter,
        InsideDiameter,
        CrosshairLineWidth,
        Rotation
    }

    internal enum Line20Parameters : int
    {
        Exposure,
        LineWidth,
        StartX,
        StartY,
        EndX,
        EndY,
        Rotation
    }

    internal enum Line21Parameters : int
    {
        Exposure,
        LineWidth,
        LineHeight,
        CentreX,
        CentreY,
        Rotation
    }

    internal enum Line22Parameters
    {
        Exposure,
        LineWidth,
        LineHeight,
        LowerLeftX,
        LowerLeftY,
        Rotation
    }

    /*
     * Check that the parsed gerber image is complete.
     * Returned errorcodes are:
     * 0: No problems
     * 1: Missing netlist
     * 2: Missing format
     * 4: Missing apertures
     * 8: Missing info
     * It could be any of above or'ed together
     */
    [Flags]
    public enum GerberVerifyError : int
    {
        /// <summary>
        /// No error found in image.
        /// </summary>
        None = 0,
        MissingNetList = 1,
        MissingFormat = 2,
        MissingApertures = 4,
        MissingImageInfo = 8,
    }

    /*! The different rendering modes available to libgerbv */
    public enum GerberRenderQuality
    {
        Default, /*!< use the cairo library */
        HighSpeed, /*!< use the cairo library with the smoothest edges */
        HighQuality
    }

}


