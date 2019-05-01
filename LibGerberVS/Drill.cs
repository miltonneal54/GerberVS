/* Drill.cs - Class for processing Excellon Drill files. */

/*  Copyright (C) 2015-2018 Milton Neal <milton200954@gmail.com>
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
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace GerberVS
{
    internal static class Drill
    {
        private const int MaxDoubleSize = 32;
        private const int ToolMin = 1;          // T00 code is reserved for unload tool command.
        private const int ToolMax = 9999;

        private static string[] supressionList = new string[] { "None", "Leading", "Trailing" };
        private static string[] unitsList = new string[] { "Inch", "MM" };
        private static List<GerberHIDAttribute> drillAttributeList = new List<GerberHIDAttribute>(); /* <---- NOT SURE IF WE NEED THIS.*/

        public static GerberImage ParseDrillFile(string drillFileName, List<GerberHIDAttribute> attributeList, int numberOfAttributes, bool reload)
        {
            bool foundEOF = false;
            string errorMessage = String.Empty;
            DrillState drillState = new DrillState();
            GerberImage image = new GerberImage("Excellon Drill File");
            CreateDefaultAttributeList();
            if (reload & attributeList != null)
            {
                image.ImageInfo.NumberOfAttribute = numberOfAttributes;
                image.ImageInfo.AttributeList = new List<GerberHIDAttribute>();
                for (int i = 0; i < numberOfAttributes; i++)
                {
                    GerberHIDAttribute attribute = new GerberHIDAttribute(attributeList[i]);
                    image.ImageInfo.AttributeList.Add(attribute);
                }
            }

            else
            {
                // Load default attributes.
                image.ImageInfo.NumberOfAttribute = drillAttributeList.Count;
                image.ImageInfo.AttributeList = new List<GerberHIDAttribute>();
                for (int i = 0; i < image.ImageInfo.NumberOfAttribute; i++)
                {
                    GerberHIDAttribute attribute = new GerberHIDAttribute(drillAttributeList[i]);
                    image.ImageInfo.AttributeList.Add(attribute);
                }
            }

            DrillAttributeMerge(image.ImageInfo.AttributeList, image.ImageInfo.NumberOfAttribute, attributeList, numberOfAttributes);
            image.FileType = GerberFileType.Drill;
            image.DrillStats = new DrillFileStats();
            image.Format = new GerberFormat();
            image.Format.OmitZeros = GerberOmitZero.OmitZerosUnspecified;

            if (image.ImageInfo.AttributeList[(int)HA.AutoDetect].DefaultValue.IntValue > 0)
            {
                drillState.AutoDetect = false;
                drillState.DataNumberFormat = DrillNumberFormat.UserDefined;
                drillState.DecimalPlaces = image.ImageInfo.AttributeList[(int)HA.Digits].DefaultValue.IntValue;
                if (image.ImageInfo.AttributeList[(int)HA.ZeroSuppression].DefaultValue.IntValue == (int)Units.Millimeters)
                    drillState.Unit = GerberUnit.Millimeter;

                switch (image.ImageInfo.AttributeList[(int)HA.ZeroSuppression].DefaultValue.IntValue)
                {
                    case (int)ZeroSuppression.Leading:
                        image.Format.OmitZeros = GerberOmitZero.OmitZerosLeading;
                        break;

                    case (int)ZeroSuppression.Trailing:
                        image.Format.OmitZeros = GerberOmitZero.OmitZerosTrailing;
                        break;

                    default:
                        image.Format.OmitZeros = GerberOmitZero.OmitZerosExplicit;
                        break;
                }
            }

            //Debug.WriteLine(String.Format("Starting to parse drill file {0}", drillFileName));

            using (StreamReader drillFileStream = new StreamReader(drillFileName, Encoding.ASCII))
            {
                GerberLineReader lineReader = new GerberLineReader(drillFileStream);
                foundEOF = ParseDrillSegment(drillFileName, lineReader, image, drillState);
            }

            if (!foundEOF)
            {
                errorMessage = String.Format(CultureInfo.CurrentCulture, "File {0} is missing Excellon Drill EOF code./n", drillFileName);
                image.DrillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError);
            }

            //Debug.WriteLine("... done parsing Excellon Drill file.");
            return image;
        }

        static bool ParseDrillSegment(string drillFileName, GerberLineReader lineReader, GerberImage image, DrillState drillState)
        {
            bool foundEOF = false;
            DrillFileStats stats = image.DrillStats;
            GerberNet currentNet = image.GerberNetList[0];
            currentNet.Level = image.LevelList[0];
            currentNet.NetState = image.NetStateList[0];

            bool done = false;
            string line;
            string[] command;
            string errorMessage;
            char nextCharacter;

            while (!lineReader.EndOfFile && !foundEOF)
            {
                nextCharacter = lineReader.Read();
                switch (nextCharacter)
                {
                    case ';':   // Comment.
                        line = lineReader.ReadLineToEnd();
                        break;

                    case 'D':
                        lineReader.Position--;
                        line = lineReader.ReadLineToEnd();
                        if (line.Substring(0, 6) == "DETECT")
                            stats.Detect = line.Substring(6, (line.Length - 7));

                        else
                        {
                            errorMessage = String.Format(CultureInfo.CurrentCulture, "Undefined header line: {0}.\n", line);
                            stats.AddNewError(-1, errorMessage, GerberErrorType.GerberNote, lineReader.LineNumber, drillFileName);
                        }
                        break;

                    case 'F':
                        lineReader.Position--;
                        line = lineReader.ReadLineToEnd();
                        if (line != "FMAT,2")
                        {
                            errorMessage = String.Format(CultureInfo.CurrentCulture, "Undefined header line: {0}.\n", line);
                            stats.AddNewError(-1, errorMessage, GerberErrorType.GerberNote, lineReader.LineNumber, drillFileName);
                        }
                        break;

                    case 'G':
                        DrillGCode gCode = ParseGCode(lineReader, image);
                        switch (gCode)
                        {
                            case DrillGCode.Rout:
                                errorMessage = "Rout Mode not supported.\n";
                                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberNote, lineReader.LineNumber, drillFileName);
                                break;

                            case DrillGCode.Drill:
                                break;

                            case DrillGCode.Slot:
                                nextCharacter = lineReader.Read();
                                ParseCoordinate(lineReader, nextCharacter, image, drillState);
                                currentNet.StopX = drillState.CurrentX;
                                currentNet.StopY = drillState.CurrentY;
                                if (drillState.Unit == GerberUnit.Millimeter)
                                {
                                    currentNet.StopX /= 25.4;
                                    currentNet.StopY /= 25.4;
                                }

                                currentNet.ApertureState = GerberApertureState.On;
                                break;

                            case DrillGCode.Absolute:
                                drillState.CoordinateMode = DrillCoordinateMode.Absolute;
                                break;

                            case DrillGCode.Incrementle:
                                drillState.CoordinateMode = DrillCoordinateMode.Incremental;
                                break;

                            case DrillGCode.ZeroSet:
                                nextCharacter = lineReader.Read();
                                ParseCoordinate(lineReader, nextCharacter, image, drillState);
                                drillState.OriginX = drillState.CurrentX;
                                drillState.OriginY = drillState.CurrentY;
                                break;

                            default:
                                line = lineReader.ReadLineToEnd();
                                break;

                        }

                        break;

                    case 'I':   // Inch header.
                        if (drillState.CurrentSection != DrillFileSection.Header)
                            break;

                        nextCharacter = lineReader.Read();
                        switch (nextCharacter)
                        {
                            // Inch
                            case 'N':
                                lineReader.Position -= 2;
                                line = lineReader.ReadLineToEnd();
                                command = line.Split(',');
                                if (command[0] == "INCH")
                                    drillState.Unit = GerberUnit.Inch;

                                if (command.Length == 2)
                                {
                                    if (command[1] == "TZ")
                                    {
                                        if (drillState.AutoDetect)
                                        {
                                            image.Format.OmitZeros = GerberOmitZero.OmitZerosLeading;
                                            drillState.HeaderNumberFormat = DrillNumberFormat.Format_00_0000;
                                            drillState.DecimalPlaces = 4;
                                        }
                                    }

                                    else if (command[1] == "LZ")
                                    {
                                        image.Format.OmitZeros = GerberOmitZero.OmitZerosTrailing;
                                        drillState.HeaderNumberFormat = DrillNumberFormat.Format_00_0000;
                                        drillState.DecimalPlaces = 4;
                                    }

                                    else
                                    {
                                        errorMessage = "Invalid zero suppression found after INCH.\n";
                                        stats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.LineNumber, drillFileName);
                                    }
                                }

                                else
                                {
                                    // No TZ/LZ specified, use defaults.
                                    if (drillState.AutoDetect)
                                    {
                                        image.Format.OmitZeros = GerberOmitZero.OmitZerosLeading;
                                        drillState.HeaderNumberFormat = DrillNumberFormat.Format_00_0000;
                                        drillState.DecimalPlaces = 4;
                                    }
                                }

                                break;

                            case 'C':
                                lineReader.Position -= 2;
                                line = lineReader.ReadLineToEnd();
                                command = line.Split(',');
                                if (command.Length == 2)
                                {
                                    if (command[1] == "ON")
                                        drillState.CoordinateMode = DrillCoordinateMode.Incremental;

                                    else if (command[1] == "OFF")
                                        drillState.CoordinateMode = DrillCoordinateMode.Absolute;

                                    else
                                    {
                                        errorMessage = "Invalid coordinate data found.\n";
                                        stats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.LineNumber, drillFileName);
                                    }
                                }

                                else
                                {
                                    errorMessage = "Invalid data found.\n";
                                    stats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.LineNumber, drillFileName);
                                }

                                break;
                        }

                        break;

                    case 'M':   // M code or Metric
                        nextCharacter = lineReader.Read();
                        if (!Char.IsDigit(nextCharacter))    // Not a M## command.
                        {
                            // Should be a metric command in header.
                            // METRIC is only acceptable within the header section.
                            // The syntax is METRIC[,{TZ|LZ}][,{000.000|000.00|0000.00}] ??????
                            if (drillState.CurrentSection != DrillFileSection.Header)
                                break;

                            done = true;
                            lineReader.Position -= 2;   // Point back to the start on the line.
                            line = lineReader.ReadLineToEnd();
                            command = line.Split(',');
                            if (command[0] == "METRIC")
                                drillState.Unit = GerberUnit.Millimeter;

                            if (command.Length > 1)
                            {
                                if (command[1] == "TZ")
                                {
                                    if (drillState.AutoDetect)
                                        image.Format.OmitZeros = GerberOmitZero.OmitZerosLeading;

                                    done = false;
                                }

                                else if (command[1] == "LZ")
                                {
                                    if (drillState.AutoDetect)
                                        image.Format.OmitZeros = GerberOmitZero.OmitZerosTrailing;

                                    done = false;
                                }

                                else
                                {
                                    errorMessage = "Invalid zero suppression found after METRIC.\n";
                                    stats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.LineNumber, drillFileName);
                                    done = true;
                                }

                                // Number format may or may not be specified.
                                if (!done && command.Length == 3)
                                {
                                    if (drillState.AutoDetect)
                                    {
                                        drillState.HeaderNumberFormat = drillState.DataNumberFormat = DrillNumberFormat.Format_000_000;
                                        drillState.DecimalPlaces = 3;
                                    }

                                    if (command[2] == "0000.00")
                                    {
                                        drillState.DataNumberFormat = DrillNumberFormat.Format_0000_00;
                                        drillState.DecimalPlaces = 2;
                                    }

                                    else if (command[2] == "000.000")
                                    {
                                        drillState.DataNumberFormat = DrillNumberFormat.Format_000_000;
                                        drillState.DecimalPlaces = 3;
                                    }

                                    else if (command[2] == "000.00")
                                    {
                                        drillState.DataNumberFormat = DrillNumberFormat.Format_000_00;
                                        drillState.DecimalPlaces = 2;
                                    }

                                    else
                                    {
                                        errorMessage = "Invalid number format found after TZ/LZ.\n";
                                        stats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.LineNumber,  drillFileName);
                                    }
                                }
                            }

                            else
                            {
                                // No TZ/LZ or number format specified, use defaults.
                                if (drillState.AutoDetect)
                                {
                                    image.Format.OmitZeros = GerberOmitZero.OmitZerosLeading;
                                    drillState.HeaderNumberFormat = DrillNumberFormat.Format_000_000;
                                    drillState.DecimalPlaces = 3;
                                }
                            }
                        }

                        else if (Char.IsDigit(nextCharacter))
                        {
                            // Must be an M## code.
                            lineReader.Position--;
                            DrillMCode mCode = ParseMCode(lineReader, drillState, image);
                            switch (mCode)
                            {
                                case DrillMCode.Header:
                                    drillState.CurrentSection = DrillFileSection.Header;
                                    break;

                                case DrillMCode.EndHeader:
                                    drillState.CurrentSection = DrillFileSection.Data;
                                    break;

                                case DrillMCode.Metric:
                                    if (drillState.Unit == GerberUnit.Unspecified && drillState.CurrentSection != DrillFileSection.Header)
                                    {
                                        errorMessage = "M71 code found with no METRIC specification in header.\n";
                                        stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, drillFileName);

                                        errorMessage = "Assuming all tool sizes are in millimeters.\n";
                                        stats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning);


                                        for (int toolNumber = ToolMin; toolNumber < ToolMax; toolNumber++)
                                        {
                                            if (image.ApertureArray[toolNumber] != null)
                                            {
                                                double toolSize = image.ApertureArray[toolNumber].Parameters[0];
                                                stats.ModifyDrillList(toolNumber, toolSize, "MM");
                                                image.ApertureArray[toolNumber].Parameters[0] /= 25.4;
                                            }
                                        }
                                    }

                                    if (drillState.AutoDetect)
                                    {
                                        drillState.DataNumberFormat = drillState.BackupNumberFormat;
                                        drillState.Unit = GerberUnit.Millimeter;
                                    }

                                    break;

                                case DrillMCode.Imperial:
                                    if (drillState.AutoDetect)
                                    {
                                        if (drillState.DataNumberFormat != DrillNumberFormat.Format_00_0000)
                                            drillState.BackupNumberFormat = drillState.DataNumberFormat;    // Save format definition for later.

                                        drillState.DataNumberFormat = DrillNumberFormat.Format_00_0000;
                                        drillState.DecimalPlaces = 4;
                                        drillState.Unit = GerberUnit.Inch;
                                    }
                                    break;

                                case DrillMCode.LongMessage:
                                case DrillMCode.Message:
                                case DrillMCode.CannedText:
                                    line = lineReader.ReadLineToEnd();
                                    // message here.
                                    break;

                                case DrillMCode.NotImplemented:
                                case DrillMCode.EndPattern:
                                case DrillMCode.TipCheck:
                                    break;

                                case DrillMCode.End:
                                    line = lineReader.ReadLineToEnd();
                                    break;

                                case DrillMCode.EndRewind:  // EOF.
                                    //done = true;
                                    foundEOF = true;
                                    break;

                                default:
                                    stats.AddNewError(-1, "Undefined M code.", GerberErrorType.GerberError, lineReader.LineNumber, drillFileName);
                                    break;

                            }
                        }
                        break;

                    case 'R':
                        if (drillState.CurrentSection == DrillFileSection.Header)
                            stats.AddNewError(-1, "R code not allowed in the header.", GerberErrorType.GerberError, lineReader.LineNumber, drillFileName);

                        else
                        {
                            double stepX = 0.0, stepY = 0.0;
                            int length = 0;

                            image.DrillStats.R++;
                            double startX = drillState.CurrentX;
                            double startY = drillState.CurrentY;
                            int repeatcount = lineReader.GetIntegerValue(ref length);
                            nextCharacter = lineReader.Read();
                            if (nextCharacter == 'X')
                            {
                                stepX = GetDoubleValue(lineReader, drillState.DataNumberFormat, image.Format.OmitZeros, drillState.DecimalPlaces);
                                nextCharacter = lineReader.Read();
                            }

                            if (nextCharacter == 'Y')
                                stepY = GetDoubleValue(lineReader, drillState.DataNumberFormat, image.Format.OmitZeros, drillState.DecimalPlaces);

                            else
                                lineReader.Position--;

                            for (int i = 1; i < repeatcount; i++)
                            {
                                drillState.CurrentX = startX + i * stepX;
                                drillState.CurrentY = startY + i * stepY;
                                currentNet = AddDrillHole(image, drillState, currentNet);
                            }
                        }
                        break;

                    case 'S':
                        // Ignore spindle speed.
                        lineReader.ReadLineToEnd();
                        break;

                    case 'T':
                        int tool = ParseTCode(lineReader, drillFileName, drillState, image);
                        break;

                    case 'X':
                    case 'Y':
                        // Hole coordinate found. Do some parsing.
                        ParseCoordinate(lineReader, nextCharacter, image, drillState);

                        // Add the new drill hole.
                        currentNet = AddDrillHole(image, drillState, currentNet);
                        break;

                    case '%':
                        drillState.CurrentSection = DrillFileSection.Data;
                        break;

                    // Ignore white space or null characters.
                    case '\n':
                    case '\r':
                    case ' ':
                    case '\t':
                    case '\0':
                        break;
                }
            }

            return foundEOF;
        }

        private static void CreateDefaultAttributeList()
        {
            // Initialize the default attributes.
            GerberHIDAttribute attribute = null;

            attribute = new GerberHIDAttribute();
            attribute.Name = "autodetect";
            attribute.HelpText = "Try to autodetect the format.";
            attribute.HIDType = GerberHIDType.Boolean;
            attribute.MinValue = 0;
            attribute.MaxValue = 0;
            attribute.DefaultValue = new HIDAttributeValue(1, null, 0);
            attribute.Enumerations = null;
            attribute.Value = IntPtr.Zero;
            drillAttributeList.Add(attribute);

            attribute = new GerberHIDAttribute();
            attribute.Name = "zero_suppession";
            attribute.HelpText = "Zero suppression.";
            attribute.HIDType = GerberHIDType.Enumeration;
            attribute.MinValue = 0;
            attribute.MaxValue = 0;
            attribute.DefaultValue = new HIDAttributeValue(0, null, 0);
            attribute.Enumerations = supressionList;
            attribute.Value = IntPtr.Zero;
            drillAttributeList.Add(attribute);

            attribute = new GerberHIDAttribute();
            attribute.Name = "units";
            attribute.HelpText = "Units.";
            attribute.HIDType = GerberHIDType.Enumeration;
            attribute.MinValue = 0;
            attribute.MaxValue = 0;
            attribute.DefaultValue = new HIDAttributeValue(0, null, 0);
            attribute.Enumerations = unitsList;
            attribute.Value = IntPtr.Zero;
            drillAttributeList.Add(attribute);

            attribute = new GerberHIDAttribute();
            attribute.Name = "tool_units";
            attribute.HelpText = "Tool size units.";
            attribute.HIDType = GerberHIDType.Enumeration;
            attribute.MinValue = 0;
            attribute.MaxValue = 0;
            attribute.DefaultValue = new HIDAttributeValue(1, null, 0);
            attribute.Enumerations = unitsList;
            attribute.Value = IntPtr.Zero;
            drillAttributeList.Add(attribute);

            attribute = new GerberHIDAttribute();
            attribute.Name = "digits";
            attribute.HelpText = "Number of digits. For trailing zero supression," +
                                 "this is the number of digits before the decimal point. " +
                                 "Otherwise this is the number of digits after the decimal point.";
            attribute.HIDType = GerberHIDType.Integer;
            attribute.MinValue = 0;
            attribute.MaxValue = 0;
            attribute.DefaultValue = new HIDAttributeValue(5, null, 0);
            attribute.Enumerations = null;
            attribute.Value = IntPtr.Zero;
            drillAttributeList.Add(attribute);
        }

        static void DrillAttributeMerge(List<GerberHIDAttribute> dest, int ndest, List<GerberHIDAttribute> src, int nsrc)
        {
            for (int i = 0; i < nsrc; i++)
            {
                // See if our destination wants this attribute.
                int j = 0;
                while (j < ndest && (src[i].Name != dest[j].Name))
                    j++;

                // If we wanted it and it is the same type, copy it over.
                if (j < ndest && src[i].HIDType == dest[j].HIDType)
                {
                    dest[j].DefaultValue = src[i].DefaultValue;
                }
            }
        }

        static DrillGCode ParseGCode(GerberLineReader lineReader, GerberImage image)
        {
            DrillFileStats stats = image.DrillStats;
            DrillGCode result = DrillGCode.Unknown;
            int length = 0;

            int value = lineReader.GetIntegerValue(ref length);
            if (length == 2)
            {
                switch (value)
                {
                    case 0:
                        stats.G00++;
                        result = DrillGCode.Rout;
                        break;

                    case 1:
                        stats.G01++;
                        result = DrillGCode.LinearMove;
                        break;

                    case 2:
                        stats.G02++;
                        result = DrillGCode.ClockwiseMove;
                        break;

                    case 3:
                        stats.G03++;
                        result = DrillGCode.CounterClockwiseMove;
                        break;

                    case 5:
                        image.DrillStats.G05++;
                        result = DrillGCode.Drill;
                        break;

                    case 90:
                        stats.G90++;
                        result = DrillGCode.Absolute;
                        break;

                    case 91:
                        stats.G91++;
                        result = DrillGCode.Incrementle;
                        break;

                    case 93:
                        stats.G93++;
                        result = DrillGCode.ZeroSet;
                        break;

                    default:
                        stats.GUnknown++;
                        break;
                }
            }

            return result;
        }

        static DrillMCode ParseMCode(GerberLineReader lineReader, DrillState drillState, GerberImage image)
        {
            DrillFileStats stats = image.DrillStats;
            DrillMCode result = DrillMCode.Unknown;
            int length = 0;
            string line = String.Empty;

            int value = lineReader.GetIntegerValue(ref length);
            switch (value)
            {
                case 0:
                    stats.M00++;
                    result = DrillMCode.End;
                    break;

                case 1:
                    stats.M01++;
                    result = DrillMCode.EndPattern;
                    break;

                case 18:
                    stats.M18++;
                    result = DrillMCode.TipCheck;
                    break;

                case 25:
                    stats.M25++;
                    result = DrillMCode.BeginPattern;
                    break;

                case 30:
                    stats.M30++;
                    result = DrillMCode.EndRewind;
                    break;

                case 31:
                    stats.M31++;
                    result = DrillMCode.BeginPattern;
                    break;

                case 45:
                    stats.M45++;
                    result = DrillMCode.LongMessage;
                    break;

                case 47:
                    stats.M47++;
                    result = DrillMCode.Message;
                    break;

                case 48:
                    stats.M48++;
                    result = DrillMCode.Header;
                    break;

                case 71:
                    stats.M71++;
                    result = DrillMCode.Metric;
                    break;

                case 72:
                    stats.M72++;
                    result = DrillMCode.Imperial;
                    break;

                case 95:
                    stats.M95++;
                    result = DrillMCode.EndHeader;
                    break;

                case 97:
                    stats.M97++;
                    result = DrillMCode.CannedText;
                    break;

                case 98:
                    stats.M98++;
                    result = DrillMCode.CannedText;
                    break;

                default:
                    stats.MUnknown++;
                    break;
            }

            return result;
        }

        static int ParseTCode(GerberLineReader lineReader, string drillFileName, DrillState drillState, GerberImage image)
        {
            int drillNumber;
            double drillSize = 0.0;
            int length = 0;
            char nextCharacter;
            bool done = false;
            DrillFileStats stats = image.DrillStats;
            string line = String.Empty;
            string errorMessage = String.Empty;

            nextCharacter = lineReader.Read();
            if (!Char.IsDigit(nextCharacter))
            {
                if (nextCharacter == 'C')
                {
                    lineReader.Position -= 2;
                    line = lineReader.ReadLineToEnd();
                    if (line == "TCST")
                    {
                        errorMessage = "Tool change stop switch found.";
                        stats.AddNewError(-1, errorMessage, GerberErrorType.GerberNote, lineReader.LineNumber, drillFileName);
                    }

                    return -1;
                }
            }

            lineReader.Position--;
            drillNumber = lineReader.GetIntegerValue(ref length);
            // T00 is a tool unload command.
            if (drillNumber == 0)
                return drillNumber;

            if (drillNumber < ToolMin && drillNumber >= ToolMax)
            {
                errorMessage = String.Format(CultureInfo.CurrentCulture, "Drill number out of bounds:{0}.\n", drillNumber);
                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, drillFileName);
            }

            drillState.CurrentTool = drillNumber;

            // Tool definition following tool number.
            if (lineReader.Position > 0)
            {
                while (!done)
                {
                    nextCharacter = lineReader.Read();
                    switch (nextCharacter)
                    {
                        case 'C':
                            drillSize = GetDoubleValue(lineReader, drillState.HeaderNumberFormat, GerberOmitZero.OmitZerosTrailing, drillState.DecimalPlaces);
                            if (drillState.Unit == GerberUnit.Millimeter)
                                drillSize /= 25.4;

                            else if (drillSize >= 4.0)
                                drillSize /= 1000.0;

                            if (drillSize <= 0 || drillSize >= 10000)
                            {
                                errorMessage = "Unreasonable drill size found.";
                                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, drillFileName);
                            }

                            else
                            {
                                // Allow a redefinition of a tool only if all parameters are the same.
                                if (image.ApertureArray[drillNumber] != null)
                                {
                                    if (image.ApertureArray[drillNumber].Parameters[0] != drillSize ||
                                        image.ApertureArray[drillNumber].ApertureType != GerberApertureType.Circle ||
                                        image.ApertureArray[drillNumber].ParameterCount != 1 ||
                                         image.ApertureArray[drillNumber].Unit != GerberUnit.Inch)
                                    {
                                        errorMessage = String.Format(CultureInfo.CurrentCulture, "Found redefinition if drill {0}.\n", drillNumber);
                                        stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError);
                                    }
                                }

                                else
                                {
                                    image.ApertureArray[drillNumber] = new ApertureDefinition();
                                    image.ApertureArray[drillNumber].Parameters[0] = drillSize;
                                    image.ApertureArray[drillNumber].ApertureType = GerberApertureType.Circle;
                                    image.ApertureArray[drillNumber].ParameterCount = 1;
                                    image.ApertureArray[drillNumber].Unit = GerberUnit.Inch;
                                }
                            }

                            string drillUnit = (drillState.Unit == GerberUnit.Millimeter) ? "MM" : "INCH";
                            stats.AddToDrillList(drillNumber, (drillState.Unit == GerberUnit.Millimeter) ? drillSize * 25.4 : drillSize, drillUnit);
                            break;

                        case 'F':
                        case 'S':
                            lineReader.GetIntegerValue(ref length);
                            break;

                        default:
                            lineReader.Position--;
                            done = true;
                            break;
                    }
                }
            }

            return drillNumber;
        }

        static GerberNet AddDrillHole(GerberImage image, DrillState drillState, GerberNet currentNet)
        {
            image.DrillStats.IncrementDrillCounter(drillState.CurrentTool);
            GerberNet newDrillNet = new GerberNet(image, currentNet, null, null);
            newDrillNet.BoundingBox = new BoundingBox();
            newDrillNet.StartX = drillState.CurrentX;
            newDrillNet.StartY = drillState.CurrentY;
            if (drillState.Unit == GerberUnit.Millimeter)
            {
                newDrillNet.StartX /= 25.4;
                newDrillNet.StartY /= 25.4;
                newDrillNet.NetState.Unit = GerberUnit.Inch;
            }

            newDrillNet.StopX = newDrillNet.StartX - drillState.OriginX;
            newDrillNet.StopY = newDrillNet.StartY - drillState.OriginY;
            newDrillNet.Aperture = drillState.CurrentTool;
            newDrillNet.ApertureState = GerberApertureState.Flash;

            // Check if the aperture is set.
            if (image.ApertureArray[drillState.CurrentTool] == null)
                return newDrillNet;

            newDrillNet.BoundingBox.Left = newDrillNet.StartX - image.ApertureArray[drillState.CurrentTool].Parameters[0] / 2;
            newDrillNet.BoundingBox.Right = newDrillNet.StartX + image.ApertureArray[drillState.CurrentTool].Parameters[0] / 2;
            newDrillNet.BoundingBox.Bottom = newDrillNet.StartY - image.ApertureArray[drillState.CurrentTool].Parameters[0] / 2;
            newDrillNet.BoundingBox.Top = newDrillNet.StartY + image.ApertureArray[drillState.CurrentTool].Parameters[0] / 2;

            image.ImageInfo.MinX = Math.Min(image.ImageInfo.MinX, (newDrillNet.StartX - image.ApertureArray[drillState.CurrentTool].Parameters[0] / 2));
            image.ImageInfo.MinY = Math.Min(image.ImageInfo.MinY, (newDrillNet.StartY - image.ApertureArray[drillState.CurrentTool].Parameters[0] / 2));
            image.ImageInfo.MaxX = Math.Max(image.ImageInfo.MaxX, (newDrillNet.StartX + image.ApertureArray[drillState.CurrentTool].Parameters[0] / 2));
            image.ImageInfo.MaxY = Math.Max(image.ImageInfo.MaxY, (newDrillNet.StartY + image.ApertureArray[drillState.CurrentTool].Parameters[0] / 2));

            return newDrillNet;
        }

        static void ParseCoordinate(GerberLineReader lineReader, char firstCharacter, GerberImage image, DrillState drillState)
        {
            if (drillState.CoordinateMode == DrillCoordinateMode.Absolute)
            {
                if (firstCharacter == 'X')
                {
                    drillState.CurrentX = GetDoubleValue(lineReader, drillState.DataNumberFormat, image.Format.OmitZeros, drillState.DecimalPlaces);
                    if (lineReader.Read() == 'Y')
                        drillState.CurrentY = GetDoubleValue(lineReader, drillState.DataNumberFormat, image.Format.OmitZeros, drillState.DecimalPlaces);

                    else
                        lineReader.Position--;
                }

                else
                    drillState.CurrentY = GetDoubleValue(lineReader, drillState.DataNumberFormat, image.Format.OmitZeros, drillState.DecimalPlaces);
            }

            else
            {
                if (firstCharacter == 'X')
                {
                    drillState.CurrentX += GetDoubleValue(lineReader, drillState.DataNumberFormat, image.Format.OmitZeros, drillState.DecimalPlaces);
                    if (lineReader.Read() == 'Y')
                        drillState.CurrentY += GetDoubleValue(lineReader, drillState.DataNumberFormat, image.Format.OmitZeros, drillState.DecimalPlaces);
                }

                else
                    drillState.CurrentY += GetDoubleValue(lineReader, drillState.DataNumberFormat, image.Format.OmitZeros, drillState.DecimalPlaces);
            }
        }

        /// <summary>
        /// Reads a double value and applies number formating if no decimal places.
        /// </summary>
        /// <param name="lineReader"></param>
        /// <param name="numberFormat"></param>
        /// <param name="omitZeros"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        static double GetDoubleValue(GerberLineReader lineReader, DrillNumberFormat numberFormat, GerberOmitZero omitZeros, int decimals)
        {
            int length = 0;
            bool hasDecimalPoint = false;

            if (lineReader.CurrentLine.Contains('.'))
                hasDecimalPoint = true;

            double value = lineReader.GetDoubleValue(ref length);
            if (hasDecimalPoint)    // No formatting required.
                return value;

            if (omitZeros == GerberOmitZero.OmitZerosTrailing)
            {
                // Append the number of zeros as needed.
                switch (numberFormat)
                {
                    case DrillNumberFormat.Format_00_0000:
                        value *= Math.Pow(10.0, (decimals + 2) - length);
                        break;

                    case DrillNumberFormat.Format_000_000:
                        value *= Math.Pow(10.0, (decimals + 3) - length);
                        break;

                    case DrillNumberFormat.Format_0000_00:
                        value *= Math.Pow(10.0, (decimals + 4) - length);
                        break;

                    case DrillNumberFormat.Format_000_00:
                        value *= Math.Pow(10.0, (decimals + 3) - length);
                        break;
                }
            }

            switch (numberFormat)
            {
                case DrillNumberFormat.Format_00_0000:
                    value /= 10000;
                    break;

                case DrillNumberFormat.Format_000_000:
                    value /= 1000;
                    break;

                case DrillNumberFormat.Format_0000_00:
                    value /= 100;
                    break;

                case DrillNumberFormat.Format_000_00:
                    value /= 100;
                    break;

                case DrillNumberFormat.UserDefined:
                    value /= Math.Pow(10.0, -1.0 * decimals);
                    break;
            }

            return value;
        }

        public static bool IsDrillFile(string fullPathName)
        {
            bool foundM48 = false;
            bool foundM30 = false;
            bool foundPercent = false;
            bool foundT = false;
            bool foundX = false;
            bool foundY = false;
            int index = 0;
            bool result = false;

            using (StreamReader streamReader = new StreamReader(fullPathName, Encoding.ASCII))
            {
                string stream = streamReader.ReadToEnd();
                // Test for a binary file by scanning the file for non ascii characters.
                stream = stream.TrimEnd(new char[] { '\0' });   // Remove any nulls that might be at the end of the file.
                foreach (char c in stream)
                {
                    if ((c < 32 || c > 127) && c != '\r' && c != '\n' && c != '\t')
                        return result;
                }

                // Look for start of header.
                if (stream.Contains("M48"))
                    foundM48 = true;

                // Look for % on it's own line at the end of the header.
                index = stream.IndexOf("%");
                if (index != -1)
                {
                    if (stream[index + 1] == '\n' || stream[index + 1] == '\r')
                        foundPercent = true;
                }

                // EOF.
                if (stream.Contains("M30"))
                {
                    if (foundPercent)
                        foundM30 = true;
                }
                // Look for T<number>.
                index = stream.IndexOf("T");
                while (index != -1)
                {
                    if (Char.IsDigit(stream[index + 1]))
                    {
                        foundT = true;
                        break;
                    }

                    index = stream.IndexOf("T", index + 1);
                }

                index = stream.IndexOf("X");
                while (index != -1)
                {
                    if (Char.IsDigit(stream[index + 1]))
                    {
                        foundX = true;
                        break;
                    }

                    index = stream.IndexOf("X", index + 1);
                }

                index = stream.IndexOf("Y");
                while (index != -1)
                {
                    if (Char.IsDigit(stream[index + 1]))
                    {
                        foundY = true;
                        break;
                    }

                    index = stream.IndexOf("Y", index + 1);
                }
            }

            // Now form logical expression determining if this is a drill file.
            if (((foundX || foundY) && foundT) && (foundM48 || (foundPercent && foundM30)))
                result = true;

            else if (foundM48 && foundT && foundPercent && foundM30)
                // Pathological case of drill file with valid header and EOF but no drill XY locations.
                result = true;

            return result;
        }
    }
}
