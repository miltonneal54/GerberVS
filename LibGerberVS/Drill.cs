/* Drill.cs - Class for processing Excellon Drill files. */

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

        public static GerberImage ParseDrillFile(string drillFileName)
        {
            return ParseDrillFile(drillFileName, false);
        }

        public static GerberImage ParseDrillFile(string drillFileName, bool reload)
        {
            bool foundEOF = false;
            string errorMessage = String.Empty;
            DrillState drillState = new DrillState();

            GerberImage drillImage = new GerberImage("Excellon Drill File");
            drillImage.FileType = GerberFileType.Drill;
            drillImage.Format.OmitZeros = GerberOmitZero.OmitZerosUnspecified;
            GerberNet gerberNet = new GerberNet(drillImage);    // Create the first gerberNet filled with some initial values.

            //Debug.WriteLine(String.Format("Starting to parse drill file {0}", drillFileName));

            using (StreamReader drillFileStream = new StreamReader(drillFileName, Encoding.ASCII))
            {
                GerberLineReader lineReader = new GerberLineReader(drillFileStream);
                foundEOF = ParseDrillSegment(drillFileName, lineReader, drillImage, drillState);
            }

            if (!foundEOF)
            {
                errorMessage = String.Format(CultureInfo.CurrentCulture, "File {0} is missing Excellon Drill EOF code./n", drillFileName);
                drillImage.DrillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError);
            }

            //Debug.WriteLine("... done parsing Excellon Drill file.");
            return drillImage;
        }

        static bool ParseDrillSegment(string drillFileName, GerberLineReader lineReader, GerberImage image, DrillState drillState)
        {
            bool foundEOF = false;
            DrillFileStats stats = image.DrillStats;
            GerberNet currentNet = image.GerberNetList[0];
            currentNet.Level = image.LevelList[0];
            currentNet.NetState = image.NetStateList[0];

            string line;
            string errorMessage;
            char nextCharacter;
            string temp2 = String.Empty;
            string temp3 = String.Empty;
            double radius;

            while (!lineReader.EndOfFile && !foundEOF)
            {
                nextCharacter = lineReader.Read();
                switch (nextCharacter)
                {
                    case ';':   // Comment.
                        line = lineReader.ReadLineToEnd();
                        errorMessage = String.Format(CultureInfo.CurrentCulture, "Comment found: {0}.\n", line);
                        stats.AddNewError(-1, errorMessage, GerberErrorType.GerberNote, lineReader.LineNumber, drillFileName);
                        stats.Comment++;
                        break;

                    case 'D':
                        lineReader.Position--;
                        line = lineReader.ReadLineToEnd();
                        if (line == "DETECT,ON" || line == "DETECT,OFF")
                        {
                            if (line == "DETECT,ON")
                                temp3 = "ON";

                            else
                                temp3 = "OFF";


                            if (!String.IsNullOrEmpty(stats.Detect))
                                temp2 = stats.Detect + "\n" + temp3;

                            else
                                temp2 = temp3;

                            stats.Detect = temp2;
                        }

                        else
                        {
                            errorMessage = String.Format(CultureInfo.CurrentCulture, "Unrecognised string {0} in header.\n", line);
                            stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, drillFileName);
                        }
                        break;

                    case 'F':
                        lineReader.Position--;
                        line = lineReader.ReadLineToEnd();
                        if (line == "FMAT,2")
                        {
                            stats.F++;
                            break;
                        }

                        if (line != "FMAT,1")
                        {
                            errorMessage = String.Format(CultureInfo.CurrentCulture, "Unsupport format: {0}.\n", line);
                            stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, drillFileName);
                        }
                        break;

                    case 'G':
                        DrillGCode gCode = ParseGCode(lineReader, image);
                        switch (gCode)
                        {
                            case DrillGCode.Drill:
                                break;

                            case DrillGCode.Slot:
                                nextCharacter = lineReader.Read();
                                ParseCoordinate(lineReader, nextCharacter, image, drillState);
                                currentNet.EndX = drillState.CurrentX;
                                currentNet.EndY = drillState.CurrentY;
                                if (drillState.Unit == GerberUnit.Millimeter)
                                {
                                    currentNet.EndX /= 25.4;
                                    currentNet.EndY /= 25.4;
                                }

                                // Update boundingBox with drilled slot stop_x,y coords.
                                radius = image.ApertureArray[drillState.CurrentTool].Parameters[0] / 2;
                                BoundingBox bbox = currentNet.BoundingBox;
                                bbox.Left = Math.Min(bbox.Left, currentNet.EndX - radius);
                                bbox.Right = Math.Max(bbox.Right, currentNet.EndX + radius);
                                bbox.Bottom = Math.Min(bbox.Bottom, currentNet.EndY - radius);
                                bbox.Top = Math.Max(bbox.Top, currentNet.EndY + radius);
                                UpdateImageInfoBounds(image.ImageInfo, currentNet.BoundingBox);
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

                            case DrillGCode.Unknown:
                                line = lineReader.ReadLineToEnd();
                                {
                                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Unrecognised string {0} found in.\n", line);
                                    stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, drillFileName);
                                }
                                break;

                            default:
                                line = lineReader.ReadLineToEnd();
                                {
                                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Unsupport G code: {0}.\n", line);
                                    stats.AddNewError(-1, errorMessage, GerberErrorType.GerberNote, lineReader.LineNumber, drillFileName);
                                }
                                break;

                        }

                        break;

                    case 'I':   // Inch header, or coordinate mode (absolute or incrimental).
                        lineReader.Position--;
                        if (HeaderIsInch(lineReader, drillFileName, drillState, image))
                            break;

                        if (HeaderIsIncremental(lineReader, drillState, image))
                            break;

                        line = lineReader.ReadLineToEnd();
                        errorMessage = String.Format(CultureInfo.CurrentCulture, "Unrecognised string {0} found in.\n", line);
                        stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, drillFileName);
                        break;

                    case 'M':   // M code or metric
                        DrillMCode mCode = ParseMCode(lineReader, image);
                        switch (mCode)
                        {
                            case DrillMCode.Header:
                                drillState.CurrentSection = DrillFileSection.Header;
                                break;

                            case DrillMCode.EndHeader:
                                drillState.CurrentSection = DrillFileSection.Data;
                                if (image.Format.OmitZeros == GerberOmitZero.OmitZerosUnspecified)
                                {
                                    errorMessage = "Unspecified format, assuming leading zeros.";
                                    stats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.LineNumber, drillFileName);
                                }

                                image.Format.OmitZeros = GerberOmitZero.OmitZerosLeading;
                                break;

                            case DrillMCode.Metric:
                                if (drillState.Unit == GerberUnit.Unspecified && drillState.CurrentSection != DrillFileSection.Header)
                                {
                                    errorMessage = "M71 code found with no METRIC specification in header.\n";
                                    stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, drillFileName);
                                    errorMessage = "Assuming all tool sizes are in millimeters.\n";
                                    stats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.LineNumber, drillFileName);

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

                            case DrillMCode.CannedTextX:
                            case DrillMCode.CannedTextY:
                                line = lineReader.ReadLineToEnd();
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Canned text {0} found.\n", line);
                                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberNote, lineReader.LineNumber, drillFileName);
                                break;

                            case DrillMCode.LongMessage:
                            case DrillMCode.Message:
                                line = lineReader.ReadLineToEnd();
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Embedded message {0} found.\n", line);
                                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberNote, lineReader.LineNumber, drillFileName);
                                break;

                            case DrillMCode.EndPattern:
                            case DrillMCode.ToolTipCheck:
                                break;

                            case DrillMCode.End:
                                line = lineReader.ReadLineToEnd();
                                break;

                            case DrillMCode.EndRewind:  // EOF.
                                foundEOF = true;
                                break;

                            case DrillMCode.Unknown:
                                lineReader.Position--;
                                if (HeaderIsMetric(lineReader, drillFileName, drillState, image))
                                    break;

                                stats.MUnknown++;
                                line = lineReader.ReadLineToEnd();
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Unrecognised string {0} found.\n", line);
                                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberNote, lineReader.LineNumber, drillFileName);
                                break;

                            default:
                                line = lineReader.ReadLineToEnd();
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Unsupported M{0} code found.\n", line);
                                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberNote, lineReader.LineNumber, drillFileName);
                                break;
                        }
                        break;

                    case 'R':
                        if (drillState.CurrentSection == DrillFileSection.Header)
                        {
                            stats.Unknown++;
                            stats.AddNewError(-1, "R code not allowed in the header.", GerberErrorType.GerberError, lineReader.LineNumber, drillFileName);
                        }

                        else
                        {
                            double stepX = 0.0, stepY = 0.0;
                            int length = 0;

                            stats.R++;
                            double startX = drillState.CurrentX;
                            double startY = drillState.CurrentY;
                            int repeatCount = lineReader.GetIntegerValue(ref length);
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

                            for (int i = 1; i < repeatCount; i++)
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

                    case 'V':
                        // Ignore VER,1.
                        lineReader.ReadLineToEnd();
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

                    default:
                        stats.Unknown++;
                        if (drillState.CurrentSection == DrillFileSection.Header)
                        {
                            errorMessage = String.Format(CultureInfo.CurrentCulture, "Undefined code {0} found in header.\n", nextCharacter);
                            stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, drillFileName);
                            line = lineReader.ReadLineToEnd();
                            errorMessage = String.Format(CultureInfo.CurrentCulture, "Unrecognised string found {0}in header.\n", line);
                            stats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.LineNumber, drillFileName);
                        }

                        else
                        {
                            errorMessage = String.Format(CultureInfo.CurrentCulture, "Ignoring undefined character {0}.\n", nextCharacter);
                            stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, drillFileName);
                        }

                        break;
                }
            }

            return foundEOF;
        }

         static DrillGCode ParseGCode(GerberLineReader lineReader, GerberImage image)
        {
            DrillFileStats stats = image.DrillStats;
            DrillGCode gCode = DrillGCode.Unknown;
            int length = 0;

            gCode = (DrillGCode)lineReader.GetIntegerValue(ref length);
            if (length == 2)
            {
                switch (gCode)
                {
                    case DrillGCode.Rout:
                        stats.G00++;
                        break;

                    case DrillGCode.LinearMove:
                        stats.G01++;
                        break;

                    case DrillGCode.ClockwiseMove:
                        stats.G02++;
                        break;

                    case DrillGCode.CounterClockwiseMove:
                        stats.G03++;
                        break;

                    case DrillGCode.Drill:
                        image.DrillStats.G05++;
                        break;

                    case DrillGCode.Slot:
                        stats.G85++;
                        break;

                    case DrillGCode.Absolute:
                        stats.G90++;
                        break;

                    case DrillGCode.Incrementle:
                        stats.G91++;
                        break;

                    case DrillGCode.ZeroSet:
                        stats.G93++;
                        break;

                    case DrillGCode.Unknown:
                    default:
                        stats.GUnknown++;
                        break;
                }
            }

            return gCode;
        }

        static DrillMCode ParseMCode(GerberLineReader lineReader, GerberImage image)
        {
            DrillFileStats stats = image.DrillStats;
            DrillMCode mCode = DrillMCode.Unknown;
            int length = 0;
            string line = String.Empty;

            mCode = (DrillMCode)lineReader.GetIntegerValue(ref length);
            if (length == 2)
            {
                switch (mCode)
                {
                    case DrillMCode.End:
                        stats.M00++;
                        break;

                    case DrillMCode.EndPattern:
                        stats.M01++;
                        break;

                    case DrillMCode.ToolTipCheck:
                        stats.M18++;
                        break;

                    case DrillMCode.BeginPattern:
                        stats.M25++;
                        break;

                    case DrillMCode.EndRewind:
                        stats.M30++;
                        break;

                    case DrillMCode.LongMessage:
                        stats.M45++;
                        break;

                    case DrillMCode.Message:
                        stats.M47++;
                        break;

                    case DrillMCode.Header:
                        stats.M48++;
                        break;

                    case DrillMCode.Metric:
                        stats.M71++;
                        break;

                    case DrillMCode.Imperial:
                        stats.M72++;
                        break;

                    case DrillMCode.EndHeader:
                        stats.M95++;
                        break;

                    case DrillMCode.CannedTextX:
                        stats.M97++;
                        break;

                    case DrillMCode.CannedTextY:
                        stats.M98++;
                        break;

                    default:
                    case DrillMCode.Unknown:
                        break;
                }
            }

            return mCode;
        }

        static int ParseTCode(GerberLineReader lineReader, string drillFileName, DrillState drillState, GerberImage image)
        {
            Aperture aperture;
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
            aperture = image.ApertureArray[drillNumber];

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
                                if (aperture != null)
                                {
                                    if (aperture.Parameters[0] != drillSize ||
                                        aperture.ApertureType != GerberApertureType.Circle ||
                                        aperture.ParameterCount != 1 ||
                                        aperture.Unit != GerberUnit.Inch)
                                    {
                                        errorMessage = String.Format(CultureInfo.CurrentCulture, "Found redefinition of drill {0}.\n", drillNumber);
                                        stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError);
                                    }
                                }

                                else
                                {
                                    aperture = new Aperture();
                                    image.ApertureArray[drillNumber] = aperture;
                                    aperture.Parameters[0] = drillSize;
                                    aperture.ApertureType = GerberApertureType.Circle;
                                    aperture.ParameterCount = 1;
                                    aperture.Unit = GerberUnit.Inch;
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

        static bool HeaderIsInch(GerberLineReader lineReader, string drillFileName, DrillState drillState, GerberImage image)
        {
            DrillFileStats stats = image.DrillStats;
            bool result = false;
            string line;
            string[] command;
            string errorMessage;

            if (drillState.CurrentSection != DrillFileSection.Header)
                return result;

            line = lineReader.ReadLineToEnd();
            command = line.Split(',');
            if (command[0] == "INCH")
            {
                result = true;
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

                else if (command.Length < 2)
                {
                    // No TZ/LZ specified, use defaults.
                    if (drillState.AutoDetect)
                    {
                        image.Format.OmitZeros = GerberOmitZero.OmitZerosLeading;
                        drillState.HeaderNumberFormat = DrillNumberFormat.Format_00_0000;
                        drillState.DecimalPlaces = 4;
                    }
                }
            }

            return result;
        }

        static bool HeaderIsMetric(GerberLineReader lineReader, string drillFileName, DrillState drillState, GerberImage image)
        {
            DrillFileStats stats = image.DrillStats;
            bool result = false;
            bool done = false;
            string line;
            string[] command;
            string errorMessage;

            /* METRIC is not an actual M code but a command that is only
             * acceptable within the header.
             *
             * The syntax is
             * METRIC[,{TZ|LZ}][,{000.000|000.00|0000.00}]
             */

            line = lineReader.ReadLineToEnd();
            if (drillState.CurrentSection != DrillFileSection.Header)
                return result;

            command = line.Split(',');
            if (command[0] == "METRIC")
            {
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
                        if (drillState.AutoDetect)
                        {
                            image.Format.OmitZeros = GerberOmitZero.OmitZerosLeading;
                            drillState.HeaderNumberFormat = drillState.DataNumberFormat = DrillNumberFormat.Format_000_000;
                            drillState.DecimalPlaces = 3;
                        }

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
                            stats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.LineNumber, drillFileName);
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

            return result;
        }

        static bool HeaderIsIncremental(GerberLineReader lineReader, DrillState drillState, GerberImage image)
        {
            DrillFileStats stats = image.DrillStats;
            bool result = false;
            string line;
            string[] command;

            /* METRIC is not an actual M code but a command that is only
             * acceptable within the header.
             *
             * The syntax is
             * METRIC[,{TZ|LZ}][,{000.000|000.00|0000.00}]
     */

            line = lineReader.ReadLineToEnd();
            command = line.Split(',');
            if (command.Length == 2)
            {
                if (command[1] == "ON")
                {
                    drillState.CoordinateMode = DrillCoordinateMode.Incremental;
                    result = true;
                }

                else if (command[1] == "OFF")
                {
                    drillState.CoordinateMode = DrillCoordinateMode.Absolute;
                    result = true;
                }

            }

            return result;
        }

        static GerberNet AddDrillHole(GerberImage image, DrillState drillState, GerberNet currentNet)
        {
            double radius;

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

            newDrillNet.EndX = newDrillNet.StartX - drillState.OriginX;
            newDrillNet.EndY = newDrillNet.StartY - drillState.OriginY;
            newDrillNet.Aperture = drillState.CurrentTool;
            newDrillNet.ApertureState = GerberApertureState.Flash;

            // Check if the aperture is set.
            if (image.ApertureArray[drillState.CurrentTool] == null)
                return newDrillNet;

            radius = image.ApertureArray[drillState.CurrentTool].Parameters[0] / 2;
            newDrillNet.BoundingBox.Left = newDrillNet.StartX - radius;
            newDrillNet.BoundingBox.Right = newDrillNet.StartX + radius;
            newDrillNet.BoundingBox.Bottom = newDrillNet.StartY - radius;
            newDrillNet.BoundingBox.Top = newDrillNet.StartY + radius;

            UpdateImageInfoBounds(image.ImageInfo, newDrillNet.BoundingBox);
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

        static void UpdateImageInfoBounds(GerberImageInfo info, BoundingBox bbox)
        {
            info.MinX = Math.Min(info.MinX, bbox.Left);
            info.MinY = Math.Min(info.MinY, bbox.Bottom);
            info.MaxX = Math.Max(info.MaxX, bbox.Right);
            info.MaxY = Math.Max(info.MaxY, bbox.Top);
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
