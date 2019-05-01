using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberVS
{
    public enum GerberOpcodes
    {
        NOP = 0,            // No operation.
        Push,               // Push the instruction onto the stack.
        PushParameter,      // Push parameter onto stack.
        PopParameter,       // Pop parameter from stack.
        Add,                // Mathmatical add operation.
        Subtract,           // Mathmatical subtract operation.
        Multiple,           // Mathmatical multiply operation.
        Divide,             // Mathmatical divide operation.
        Primative           // Draw macro primative.
    }

    // The different error message types used in GerberVS Library
    public enum GerberErrorType
    {
        GerberCritical = 0,    // File processing can not continue.
        GerberError,           // Something went wrong, but processing can still continue.
        GerberWarning,         // Something was encountered that may provide the wrong output.
        GerberNote             // An irregularity was encountered, but needs no intervention.
    }

    public enum GerberHIDType
    {
        Label,
        Integer,
        HID_Real,
        String,
        Boolean,
        Enumeration,
        Mixed,
        Path
    }

    /// <summary>
    /// Enumeration of aperture types.
    /// </summary>
    public enum GerberApertureType
    {
        None,             // no aperture used.
        Circle,           // a round aperture.
        Rectangle,        // a rectangular aperture.
        Oval,             // an ovular (obround) aperture.
        Polygon,          // a polygon aperture.
        Macro,            // a RS274X macro.
        MacroCircle,     // a RS274X circle macro.
        MacroOutline,    // a RS274X outline macro.
        MacroPolygon,    // a RS274X polygon macro.
        MacroMoire,      // a RS274X moire macro.
        MacroThermal,    // a RS274X thermal macro.
        MacroLine20,     // a RS274X line (code 20) macro.
        MarcoLine21,     // a RS274X line (code 21) macro.
        MacroLine22      // a RS274X line (code 22) macro.
    }

    // The current state of the aperture drawing tool.
    public enum GerberApertureState
    {
        Off,   // tool drawing is off, and nothing will be drawn.
        On,    // tool drawing is on, and something will be drawn.
        Flash  // tool is flashing, and will draw a single aperture.
    }

    // The current unit used.
    public enum GerberUnit
    {
        Inch,        // inches.
        Millimeter,          // mm.
        Unspecified  // use default units.
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

    // the coordinate system used.
    public enum GerberCoordinate
    {
        Absolute,    // all coordinates are absolute from a common origin.
        Incremental  // all coordinates are relative to the previous coordinate.
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
        /// Start polygon draw.
        /// </summary>
        PolygonAreaStart,
        /// <summary>
        /// End polygon draw.
        /// </summary>
        PolygonAreaEnd,        
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
        PickAndPlace   // the file is a CSV pick and place file.
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
    internal enum GerberCircleParameters : int
    {
        Exposure,
        Diameter,
        CentreX,
        CentreY,
    }

    /// <summary>
    /// Enumerates the outline aperture parameter indexes.
    /// </summary>
    internal enum GerberOutlineParameters : int
    {
        Exposure,
        NumberOfPoints,
        FirstX,
        FirstY,
        Rotation
    }

    internal enum GerberPolygonParameters : int
    {
        Exposure,
        NumberOfPoints,
        CenterX,
        CenterY,
        Diameter,
        Rotation
    }

    internal enum GerberMoireParameters : int
    {
        CenterX,
        CenterY,
        OutsideDiameter,
        CircleLineWidth,
        GapWidth,
        NumberOfCircles,
        CrosshairLineWidth,
        CrosshairLength,
        Rotation
    }

    internal enum GerberThermalParameters : int
    {
        CenterX,
        CenterY,
        OutsideDiameter,
        InsideDiameter,
        CrosshairLineWidth,
        Rotation
    }

    internal enum GerberLine20Parameters : int
    {
        Exposure,
        LineWidth,
        StartX,
        StartY,
        EndX,
        EndY,
        Rotation
    }

    internal enum GerberLine21Parameters : int
    {
        Exposure,
        LineWidth,
        LineHeight,
        CenterX,
        CenterY,
        Rotation
    }

    internal enum GerberLine22Parameters
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
        ImageOK = 0,
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


