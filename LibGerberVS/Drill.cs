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
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

namespace GerberVS
{
    internal static class Drill
    {
        private const int MaxDoubleSize = 32;
        private const int ToolMin = 1;          // T00 code is reserved for unload tool command.
        private const int ToolMax = 9999;

        private static GerberLineReader lineReader;
        private static DrillFileStats drillStats;

        //private static string[] supressionList = new string[] { "None", "Leading", "Trailing" };
        //private static string[] unitsList = new string[] { "Inch", "MM" };

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

        public static GerberImage ParseDrillFile(string drillFileName)
        {
            return ParseDrillFile(drillFileName, false);
        }

        public static GerberImage ParseDrillFile(string drillFileName, bool reload)
        {
            bool foundEOF = false;
            string errorMessage = String.Empty;
            DrillState drillState = new DrillState();

            GerberImage image = new GerberImage("Excellon Drill File");
            image.FileType = GerberFileType.Drill;
            drillStats = image.DrillStats;  // Maintains the stats as the drill file is read in.

            image.Format.OmitZeros = GerberOmitZero.OmitZerosUnspecified;
            GerberNet currentNet = new GerberNet(image);    // Create the first gerberNet filled with some initial values.
            currentNet.Level = image.LevelList[0];
            currentNet.NetState = image.NetStateList[0];

            // Start parsing.
            //Debug.WriteLine(String.Format("Starting to parse drill file {0}", drillFileName));
            using (StreamReader drillFileStream = new StreamReader(drillFileName, Encoding.ASCII))
            {
                lineReader = new GerberLineReader(drillFileStream);
                lineReader.FileName = Path.GetFileName(drillFileName);
                lineReader.FilePath = Path.GetDirectoryName(drillFileName);
                foundEOF = ParseDrillSegment(image, drillState, currentNet);
            }

            if (!foundEOF)
            {
                errorMessage = String.Format(CultureInfo.CurrentCulture, "File {0} is missing Excellon Drill EOF code./n", drillFileName);
                drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError);
            }

            //Debug.WriteLine("... done parsing Excellon Drill file.");
            return image;
        }

        static bool ParseDrillSegment(GerberImage image, DrillState drillState, GerberNet currentNet)
        {
            bool foundEOF = false;
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
                        errorMessage = String.Format(CultureInfo.CurrentCulture, "Comment: {0}.\n", line);
                        drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberNote, lineReader.LineNumber, lineReader.FileName);
                        drillStats.Comment++;
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

                            if (!String.IsNullOrEmpty(drillStats.Detect))
                                temp2 = drillStats.Detect + "\n" + temp3;

                            else
                                temp2 = temp3;

                            drillStats.Detect = temp2;
                        }

                        else
                        {
                            errorMessage = String.Format(CultureInfo.CurrentCulture, "Unrecognised string {0} in header.\n", line);
                            drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, lineReader.FileName);
                        }
                        break;

                    case 'F':
                        lineReader.Position--;
                        line = lineReader.ReadLineToEnd();
                        if (line == "FMAT,2")
                        {
                            drillStats.F++;
                            break;
                        }

                        if (line != "FMAT,1")
                        {
                            errorMessage = String.Format(CultureInfo.CurrentCulture, "Unsupported format: {0}.\n", line);
                            drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, lineReader.FileName);
                        }
                        break;

                    case 'G':
                        DrillGCode gCode = ParseGCode(image);
                        switch (gCode)
                        {
                            case DrillGCode.Drill:
                                break;

                            case DrillGCode.Slot:
                                nextCharacter = lineReader.Read();
                                ParseCoordinate(nextCharacter, image, drillState);
                                currentNet.EndX = drillState.CurrentX;
                                currentNet.EndY = drillState.CurrentY;
                                if (drillState.Unit == GerberUnit.Millimeter)
                                {
                                    currentNet.EndX /= 25.4;
                                    currentNet.EndY /= 25.4;
                                }

                                // Update boundingBox with drilled slot stop_x,y coords.
                                radius = image.ApertureArray()[drillState.CurrentTool].Parameters()[0] / 2;
                                BoundingBox bbox = currentNet.BoundingBox;
                                bbox.Left = Math.Min(bbox.Left, currentNet.EndX - radius);
                                bbox.Right = Math.Max(bbox.Right, currentNet.EndX + radius);
                                bbox.Bottom = Math.Min(bbox.Bottom, currentNet.EndY - radius);
                                bbox.Top = Math.Max(bbox.Top, currentNet.EndY + radius);
                                UpdateImageInfoBounds(image.ImageInfo, currentNet.BoundingBox);
                                currentNet.ApertureState = GerberApertureState.On;
                                //currentNet.Interpolation = GerberInterpolation.DrillSlot;
                                break;

                            case DrillGCode.Absolute:
                                drillState.CoordinateMode = DrillCoordinateMode.Absolute;
                                break;

                            case DrillGCode.Incrementle:
                                drillState.CoordinateMode = DrillCoordinateMode.Incremental;
                                break;

                            case DrillGCode.ZeroSet:
                                nextCharacter = lineReader.Read();
                                ParseCoordinate(nextCharacter, image, drillState);
                                drillState.OriginX = drillState.CurrentX;
                                drillState.OriginY = drillState.CurrentY;
                                break;

                            case DrillGCode.Unknown:
                                line = lineReader.ReadLineToEnd();
                                {
                                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Unrecognised string {0} found in.\n", line);
                                    drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, lineReader.FileName);
                                }
                                break;

                            default:
                                line = lineReader.ReadLineToEnd();
                                {
                                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Unsupported G code: {0}.\n", line);
                                    drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberNote, lineReader.LineNumber, lineReader.FileName);
                                }
                                break;

                        }

                        break;

                    case 'I':   // Inch header, or coordinate mode (absolute or incrimental).
                        lineReader.Position--;
                        if (HeaderIsInch(drillState, image))
                            break;

                        if (HeaderIsIncremental(drillState, image))
                            break;

                        line = lineReader.ReadLineToEnd();
                        errorMessage = String.Format(CultureInfo.CurrentCulture, "Unrecognised string {0} found in.\n", line);
                        drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, lineReader.FileName);
                        break;

                    case 'M':   // M code or metric
                        // Peek at the next character and if 'E, then it maybe the "METRIC" header.
                        if (Char.ToUpper(lineReader.Peek()) == 'E')
                        {
                            if(HeaderIsMetric(drillState, image))
                                break;
                        }

                        DrillMCode mCode = ParseMCode(image);
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
                                    drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.LineNumber, lineReader.FileName);
                                }

                                image.Format.OmitZeros = GerberOmitZero.OmitZerosLeading;
                                break;

                            case DrillMCode.Metric:
                                if (drillState.Unit == GerberUnit.Unspecified && drillState.CurrentSection != DrillFileSection.Header)
                                {
                                    errorMessage = "M71 code found with no METRIC specification in header.\n";
                                    drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, lineReader.FileName);
                                    errorMessage = "Assuming all tool sizes are in millimeters.\n";
                                    drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.LineNumber, lineReader.FileName);

                                    for (int toolNumber = ToolMin; toolNumber < ToolMax; toolNumber++)
                                    {
                                        if (image.ApertureArray()[toolNumber] != null)
                                        {
                                            double toolSize = image.ApertureArray()[toolNumber].Parameters()[0];
                                            drillStats.ModifyDrillList(toolNumber, toolSize, "MM");
                                            image.ApertureArray()[toolNumber].Parameters()[0] /= 25.4;
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
                                drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberNote, lineReader.LineNumber, lineReader.FileName);
                                break;

                            case DrillMCode.LongMessage:
                            case DrillMCode.Message:
                                line = lineReader.ReadLineToEnd();
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Embedded message {0} found.\n", line);
                                drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberNote, lineReader.LineNumber, lineReader.FileName);
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
                                if (HeaderIsMetric(drillState, image))
                                    break;

                                drillStats.MUnknown++;
                                line = lineReader.ReadLineToEnd();
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Unrecognised string {0} found.\n", line);
                                drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberNote, lineReader.LineNumber, lineReader.FileName);
                                break;

                            default:
                                line = lineReader.ReadLineToEnd();
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Unsupported M{0} code found.\n", line);
                                drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberNote, lineReader.LineNumber, lineReader.FileName);
                                break;
                        }
                        break;

                    case 'R':
                        if (drillState.CurrentSection == DrillFileSection.Header)
                        {
                            drillStats.Unknown++;
                            drillStats.AddNewError(-1, "R code not allowed in the header.", GerberErrorType.GerberError, lineReader.LineNumber, lineReader.FileName);
                        }

                        else
                        {
                            double stepX = 0.0, stepY = 0.0;
                            int length = 0;

                            drillStats.R++;
                            double startX = drillState.CurrentX;
                            double startY = drillState.CurrentY;
                            int repeatCount = lineReader.GetIntegerValue(ref length);
                            nextCharacter = lineReader.Read();
                            if (nextCharacter == 'X')
                            {
                                stepX = GetDoubleValue(drillState.DataNumberFormat, image.Format.OmitZeros, drillState.DecimalPlaces);
                                nextCharacter = lineReader.Read();
                            }

                            if (nextCharacter == 'Y')
                                stepY = GetDoubleValue(drillState.DataNumberFormat, image.Format.OmitZeros, drillState.DecimalPlaces);

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
                        int tool = ParseTCode(drillState, image);
                        break;

                    case 'V':
                        // Ignore VER,1.
                        lineReader.ReadLineToEnd();
                        break;

                    case 'X':
                    case 'Y':
                        if(lineReader.LineNumber == 26)
                        {
                            ;
                        }
                        // Hole coordinate found. Do some parsing.
                        ParseCoordinate(nextCharacter, image, drillState);

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
                        drillStats.Unknown++;
                        if (drillState.CurrentSection == DrillFileSection.Header)
                        {
                            //errorMessage = String.Format(CultureInfo.CurrentCulture, "Undefined code {0} found in header.\n", nextCharacter);
                            //drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, lineReader.FileName);
                            line = lineReader.CurrentLine;
                            errorMessage = String.Format(CultureInfo.CurrentCulture, "Unrecognised line {0} found in header.\n", '"'+ line + '"');
                            drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberNote, lineReader.LineNumber, lineReader.FileName);
                        }

                        else
                        {
                            errorMessage = String.Format(CultureInfo.CurrentCulture, "Ignoring undefined character {0}.\n", nextCharacter);
                            drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, lineReader.FileName);
                        }

                        break;
                }
            }

            return foundEOF;
        }

        static DrillGCode ParseGCode(GerberImage image)
        {
            DrillGCode gCode = DrillGCode.Unknown;
            int length = 0;

            gCode = (DrillGCode)lineReader.GetIntegerValue(ref length);
            if (length == 2)
            {
                switch (gCode)
                {
                    case DrillGCode.Rout:
                        drillStats.G00++;
                        break;

                    case DrillGCode.LinearMove:
                        drillStats.G01++;
                        break;

                    case DrillGCode.ClockwiseMove:
                        drillStats.G02++;
                        break;

                    case DrillGCode.CounterClockwiseMove:
                        drillStats.G03++;
                        break;

                    case DrillGCode.Drill:
                        image.DrillStats.G05++;
                        break;

                    case DrillGCode.Slot:
                        drillStats.G85++;
                        break;

                    case DrillGCode.Absolute:
                        drillStats.G90++;
                        break;

                    case DrillGCode.Incrementle:
                        drillStats.G91++;
                        break;

                    case DrillGCode.ZeroSet:
                        drillStats.G93++;
                        break;

                    case DrillGCode.Unknown:
                    default:
                        drillStats.GUnknown++;
                        break;
                }
            }

            return gCode;
        }

        static DrillMCode ParseMCode(GerberImage image)
        {
            DrillMCode mCode = DrillMCode.Unknown;
            int length = 0;

            mCode = (DrillMCode)lineReader.GetIntegerValue(ref length);
            if (length == 2)
            {
                switch (mCode)
                {
                    case DrillMCode.End:
                        drillStats.M00++;
                        break;

                    case DrillMCode.EndPattern:
                        drillStats.M01++;
                        break;

                    case DrillMCode.ToolTipCheck:
                        drillStats.M18++;
                        break;

                    case DrillMCode.BeginPattern:
                        drillStats.M25++;
                        break;

                    case DrillMCode.EndRewind:
                        drillStats.M30++;
                        break;

                    case DrillMCode.LongMessage:
                        drillStats.M45++;
                        break;

                    case DrillMCode.Message:
                        drillStats.M47++;
                        break;

                    case DrillMCode.Header:
                        drillStats.M48++;
                        break;

                    case DrillMCode.Metric:
                        drillStats.M71++;
                        break;

                    case DrillMCode.Imperial:
                        drillStats.M72++;
                        break;

                    case DrillMCode.EndHeader:
                        drillStats.M95++;
                        break;

                    case DrillMCode.CannedTextX:
                        drillStats.M97++;
                        break;

                    case DrillMCode.CannedTextY:
                        drillStats.M98++;
                        break;

                    default:
                    case DrillMCode.Unknown:
                        break;
                }
            }

            return mCode;
        }

        static int ParseTCode(DrillState drillState, GerberImage image)
        {
            Aperture aperture;
            int drillNumber;
            double drillSize = 0.0;
            int length = 0;
            char nextCharacter;
            bool done = false;
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
                        drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberNote, lineReader.LineNumber, lineReader.FileName);
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
                drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, lineReader.FileName);
                return -1;
            }

            drillState.CurrentTool = drillNumber;
            aperture = image.ApertureArray()[drillNumber];

            // Tool definition following tool number.
            if (lineReader.Position > 0)
            {
                while (!done)
                {
                    nextCharacter = lineReader.Read();
                    switch (nextCharacter)
                    {
                        case 'C':
                            drillSize = GetDoubleValue(drillState.HeaderNumberFormat, GerberOmitZero.OmitZerosTrailing, drillState.DecimalPlaces);
                            if (drillState.Unit == GerberUnit.Millimeter)
                                drillSize /= 25.4;

                            else if (drillSize >= 4.0)
                                drillSize /= 1000.0;

                            if (drillSize <= 0 || drillSize >= 10000)
                            {
                                errorMessage = "Unreasonable drill size found.";
                                drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.LineNumber, lineReader.FileName);
                            }

                            else
                            {
                                // Allow a redefinition of a tool only if all parameters are the same.
                                if (aperture != null)
                                {
                                    if (aperture.Parameters()[0] != drillSize ||
                                        aperture.ApertureType != GerberApertureType.Circle ||
                                        aperture.ParameterCount != 1 ||
                                        aperture.Unit != GerberUnit.Inch)
                                    {
                                        errorMessage = String.Format(CultureInfo.CurrentCulture, "Found redefinition of drill {0}.\n", drillNumber);
                                        drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError);
                                    }
                                }

                                else
                                {
                                    aperture = new Aperture();
                                    image.ApertureArray()[drillNumber] = aperture;
                                    aperture.Parameters()[0] = drillSize;
                                    aperture.ApertureType = GerberApertureType.Circle;
                                    aperture.ParameterCount = 1;
                                    aperture.Unit = GerberUnit.Inch;
                                }
                            }

                            string drillUnit = (drillState.Unit == GerberUnit.Millimeter) ? "MM" : "INCH";
                            drillStats.AddToDrillList(drillNumber, (drillState.Unit == GerberUnit.Millimeter) ? drillSize * 25.4 : drillSize, drillUnit);
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

        static bool HeaderIsInch(DrillState drillState, GerberImage image)
        {
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
                        drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.LineNumber, lineReader.FileName);
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

        static bool HeaderIsMetric(DrillState drillState, GerberImage image)
        {
            bool done = false;
            string line;
            string[] command;
            string errorMessage;

            /* METRIC is not an actual M code but a command that is only
             * acceptable within the header region.
             *
             * The syntax is
             * METRIC[,{TZ|LZ}][,{000.000|000.00|0000.00}]
             */


            if (drillState.CurrentSection != DrillFileSection.Header)
                return false;

            lineReader.Position--;
            line = lineReader.ReadLineToEnd();
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
                            drillStats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.LineNumber, lineReader.FileName);
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

            return true;
        }

        static bool HeaderIsIncremental(DrillState drillState, GerberImage image)
        {
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
            if (image.ApertureArray()[drillState.CurrentTool] == null)
                return newDrillNet;

            radius = image.ApertureArray()[drillState.CurrentTool].Parameters()[0] / 2;
            newDrillNet.BoundingBox.Left = newDrillNet.StartX - radius;
            newDrillNet.BoundingBox.Right = newDrillNet.StartX + radius;
            newDrillNet.BoundingBox.Bottom = newDrillNet.StartY - radius;
            newDrillNet.BoundingBox.Top = newDrillNet.StartY + radius;

            UpdateImageInfoBounds(image.ImageInfo, newDrillNet.BoundingBox);
            return newDrillNet;
        }

        static void ParseCoordinate(char firstCharacter, GerberImage image, DrillState drillState)
        {
            //int currentLine = lineReader.LineNumber;
            if (drillState.CoordinateMode == DrillCoordinateMode.Absolute)
            {
                if (firstCharacter == 'X')
                {
                    // X coord.
                    drillState.CurrentX = GetDoubleValue(drillState.DataNumberFormat, image.Format.OmitZeros, drillState.DecimalPlaces);
                    // Get the Y coord only if it's in the same line.
                    if (lineReader.Read() == 'Y' /*&& lineReader.LineNumber == currentLine*/)
                        drillState.CurrentY = GetDoubleValue(drillState.DataNumberFormat, image.Format.OmitZeros, drillState.DecimalPlaces);

                    else
                        lineReader.Position--;
                }

                else
                {
                    // Only a Y coord in the line.
                    drillState.CurrentY = GetDoubleValue(drillState.DataNumberFormat, image.Format.OmitZeros, drillState.DecimalPlaces);
                }
            }

            else
            {
                if (firstCharacter == 'X')
                {
                    drillState.CurrentX += GetDoubleValue(drillState.DataNumberFormat, image.Format.OmitZeros, drillState.DecimalPlaces);
                    if (lineReader.Read() == 'Y' /*&& lineReader.LineNumber == currentLine*/)
                        drillState.CurrentY += GetDoubleValue(drillState.DataNumberFormat, image.Format.OmitZeros, drillState.DecimalPlaces);

                    else
                        lineReader.Position--;
                }

                else
                    drillState.CurrentY += GetDoubleValue(drillState.DataNumberFormat, image.Format.OmitZeros, drillState.DecimalPlaces);
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
        /// <param name="numberFormat"></param>
        /// <param name="omitZeros"></param>
        /// <param name="decimals"></param>
        /// <returns>double value</returns>
        static double GetDoubleValue(DrillNumberFormat numberFormat, GerberOmitZero omitZeros, int decimals)
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
    }
}
