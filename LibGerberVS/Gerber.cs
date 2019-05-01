/* Gerber.cs - Handles processing of Gerber files. */

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
    internal static class Gerber
    {
        /// <summary>
        /// Lowest allowable aperture number.
        /// </summary>
        public const int MinimumAperture = 10;
        /// <summary>
        /// Maximum allowable apertures.
        /// </summary>
        public const int MaximumApertures = 9999;
        /// <summary>
        /// Maximum allowable aperture parameters.
        /// </summary>
        public const int MaximumApertureParameters = 102;

        static List<GerberLineReader> lineReaderList;

        static bool foundEOF = false;               // Will be set true if a gerber program stop/end command is found.
        static int levelOfRecursion = 0;            // Keeps track of included file levels.
        static string errorMessage = String.Empty;

        private static double imageScaleA = 1.0;
        private static double imageScaleB = 1.0;
        private static double imageRotation = 0.0;

        private static Matrix apertureMatrix;

        // Knockout variables.
        private static bool knockoutMeasure = false;
        private static double knockoutLimitXmin, knockoutLimitYmin, knockoutLimitXmax, knockoutLimitYmax;
        private static GerberLevel knockoutLevel = null;

        /// <summary>
        /// Logical test for RS427X file.
        /// </summary>
        /// <param name="fullPathName">file to test</param>
        /// <returns>result of test</returns>
        public static bool IsGerberRS427X(string fullPathName)
        {
            bool foundADD = false;
            bool foundD0 = false;
            bool foundD2 = false;
            bool foundM0 = false;
            bool foundM2 = false;
            bool foundStar = false;
            bool foundX = false;
            bool foundY = false;
            bool result = false;
            int index = 0;

            using (StreamReader streamReader = new StreamReader(fullPathName, Encoding.ASCII))
            {
                string line = String.Empty;
                int lineCount = 0;

                while ((line = streamReader.ReadLine()) != null)
                {
                    lineCount++;
                    // Test for a binary file by scanning the file for non ascii characters.
                    foreach (char c in line)
                    {
                        if ((c < 32 || c > 127) && c != '\r' && c != '\n' && c != '\t')
                            return result;
                    }

                    if (line.Contains("%ADD"))
                        foundADD = true;

                    if (line.Contains("D00") || line.Contains("D0"))
                        foundD0 = true;

                    if (line.Contains("D02") || line.Contains("D2"))
                        foundD2 = true;

                    if (line.Contains("M0") || line.Contains("M00"))
                        foundM0 = true;

                    if (line.Contains("M2") || line.Contains("M02"))
                        foundM2 = true;

                    if (line.Contains('*'))
                        foundStar = true;

                    index = line.IndexOf("X");
                    while (index != -1)
                    {
                        if (Char.IsDigit(line[index + 1]))
                        {
                            foundX = true;
                            break;
                        }

                        index = line.IndexOf("X", index + 2);
                    }

                    index = line.IndexOf("Y");
                    while (index != -1)
                    {
                        if (Char.IsDigit(line[index + 1]))
                        {
                            foundY = true;
                            break;
                        }

                        index = line.IndexOf("Y", index + 2);
                    }


                    // Logical expression determining if the file is RS-274X.
                    if ((foundD0 || foundD2 || foundM0 || foundM2) && foundADD && foundStar && (foundX || foundY))
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Process the Gerber file.
        /// </summary>
        /// <param name="gerberFile">full path and file name of the gerber file</param>
        /// <returns>gerber image</returns>
        /// <remarks>
        /// This is a wrapper which gets called from top level. 
        /// It does some initialization and pre-processing, and 
        /// then calls GerberParseFileSegment method which
        /// processes the actual file. Then it does final 
        /// modifications to the image created.
        /// </remarks>
        public static GerberImage ParseGerber(string gerberFile)
        {
            lineReaderList = new List<GerberLineReader>();
            GerberLineReader lineReader;
            string fileName = Path.GetFileName(gerberFile);
            string filePath = Path.GetDirectoryName(gerberFile);

            // Create new state. This is used locally to keep track
            // of the photoplotter's state as the Gerber is read in.
            GerberState gerberStateLocal = new GerberState();

            // Create new image. This will be returned.
            GerberImage gerberImage = new GerberImage("RS274-X (Gerber) File");
            gerberImage.FileType = GerberFileType.RS274X;

            // Set active Netlist, Level and NetState to point
            // to first default ones created in GerberImage constructor. 
            GerberNet currentNet = gerberImage.GerberNetList[0];
            gerberStateLocal.Level = gerberImage.LevelList[0];
            gerberStateLocal.NetState = gerberImage.NetStateList[0];
            currentNet.Level = gerberStateLocal.Level;
            currentNet.NetState = gerberStateLocal.NetState;

            // Start parsing.
            //Debug.WriteLine(String.Format("Starting to parse Gerber file: {0}", fileName));

            using (StreamReader gerberFileStream = new StreamReader(gerberFile, Encoding.ASCII))
            {
                lineReader = new GerberLineReader(gerberFileStream);
                lineReader.FileName = Path.GetFileName(gerberFile);
                lineReader.FilePath = Path.GetDirectoryName(gerberFile);
                lineReaderList.Add(lineReader);
                foundEOF = ParseFileSegment(gerberImage, gerberStateLocal, currentNet);
            }

            if (!foundEOF)
            {
                errorMessage = String.Format(CultureInfo.CurrentCulture, "File {0} is missing Gerber EOF code.", lineReader.FileName);
                gerberImage.GerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError);
            }

            //Debug.WriteLine("... done parsing Gerber file.");
            UpdateKnockoutMeasurements();
            GerberCalculateFinalJustifyEffects(gerberImage);
            return gerberImage;
        }

        private static bool ParseFileSegment(GerberImage gerberImage, GerberState gerberState, GerberNet currentNet)
        {
            char nextCharacter;
            int length;
            int coordinate;
            int polygonPoints = 0;
            double scaleX = 0.0, scaleY = 0.0;
            double centerX = 0.0, centerY = 0.0;
            SizeD apertureSize = new SizeD();
            double scale;
            int quadrant = -1;
            
            GerberFileStats gerberStats = gerberImage.GerberStats;
            GerberLineReader lineReader = lineReaderList[levelOfRecursion];
            BoundingBox boundingBox = new BoundingBox();

            while (!lineReader.EndOfFile)
            {
                length = 0;
                // Figure out the scale, since we need to normalize all dimensions to inches.
                if (gerberState.NetState.Unit == GerberUnit.Millimeter)
                    scale = 25.4;

                else
                    scale = 1.0;

                nextCharacter = lineReader.Read();
                switch (nextCharacter)
                {
                    case 'G':
                        //Debug.WriteLine("Found G code in Line: {0}", lineReader.LineNumber);
                        ParseGCode(lineReader, gerberState, gerberImage);
                        break;

                    case 'D':
                        //Debug.WriteLine("Found D code in Line: {0}", lineReader.LineNumber);
                        ParseDCode(lineReader, gerberState, gerberImage);
                        break;

                    case 'M':
                        //Debug.WriteLine("Found M code in Line: {0}", lineReader.LineNumber);
                        switch (ParseMCode(lineReader, gerberImage))
                        {
                            case 1:
                            case 2:
                            case 3:
                                foundEOF = true;
                                break;

                            default:
                                foundEOF = false;
                                break;
                        }
                        break;

                    case 'X':
                        //Debug.WriteLine("Found X code in Line: {0}", lineReader.LineNumber);
                        gerberStats.XCount++;
                        length = 0;
                        coordinate = lineReader.GetIntegerValue(ref length);
                        if (gerberImage.Format != null && gerberImage.Format.OmitZeros == GerberOmitZero.OmitZerosTrailing)
                            NormaliseCoordinate(gerberImage.Format.IntegralPartX, gerberImage.Format.DecimalPartX, length, ref coordinate);

                        if (gerberImage.Format != null && (gerberImage.Format.Coordinate == GerberCoordinate.Incremental))
                            gerberState.CurrentX += coordinate;

                        else
                            gerberState.CurrentX = coordinate;

                        gerberState.ChangedState = true;
                        break;

                    case 'Y':
                        //Debug.WriteLine("Found Y code in Line: {0}", lineReader.LineNumber);
                        gerberStats.YCount++;
                        length = 0;
                        coordinate = lineReader.GetIntegerValue(ref length);
                        if (gerberImage.Format != null && gerberImage.Format.OmitZeros == GerberOmitZero.OmitZerosTrailing)
                            NormaliseCoordinate(gerberImage.Format.IntegralPartY, gerberImage.Format.DecimalPartY, length, ref coordinate);

                        if (gerberImage.Format != null && (gerberImage.Format.Coordinate == GerberCoordinate.Incremental))
                            gerberState.CurrentY += coordinate;

                        else
                            gerberState.CurrentY = coordinate;

                        gerberState.ChangedState = true;
                        break;

                    case 'I':
                        //Debug.WriteLine("Found I code in Line: {0}", lineReader.LineNumber);
                        gerberStats.ICount++;
                        coordinate = lineReader.GetIntegerValue(ref length); ;
                        if (gerberImage.Format != null && gerberImage.Format.OmitZeros == GerberOmitZero.OmitZerosTrailing)
                            NormaliseCoordinate(gerberImage.Format.IntegralPartX, gerberImage.Format.DecimalPartX, length, ref coordinate);

                        gerberState.CenterX = coordinate;
                        gerberState.ChangedState = true;
                        break;

                    case 'J':
                        //Debug.WriteLine("Found J code in Line: {0}", lineReader.LineNumber);
                        gerberStats.JCount++;
                        coordinate = lineReader.GetIntegerValue(ref length);
                        if (gerberImage.Format != null && gerberImage.Format.OmitZeros == GerberOmitZero.OmitZerosTrailing)
                            NormaliseCoordinate(gerberImage.Format.IntegralPartY, gerberImage.Format.DecimalPartY, length, ref coordinate);

                        gerberState.CenterY = coordinate;
                        gerberState.ChangedState = true;
                        break;

                    case '%':
                        //Debug.WriteLine("Found % code in Line: {0}", lineReader.LineNumber);
                        while (true)
                        {
                            ParseRS274X(gerberImage, gerberState, currentNet, lineReader);
                            // Skip past any whitespaces.
                            lineReader.SkipWhiteSpaces();
                            nextCharacter = lineReader.Read();

                            if (lineReader.EndOfFile || nextCharacter == '%')
                                break;
                            // Loop again to catch multiple blocks on the same line (separated by '*' character)
                            lineReader.Position--;
                        }

                        break;

                    case '*':
                        //Debug.WriteLine("Found * in Line: {0}", lineReader.LineNumber);
                        gerberStats.StarCount++;
                        if (!gerberState.ChangedState)
                            break;

                        apertureMatrix = new Matrix(1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f);    // Matrix for rotation, offsets etc.
                        quadrant = -1;
                        gerberState.ChangedState = false;
                        // Don't even bother saving the geberNet if the aperture state is GERBER_APERTURE_STATE_OFF and we
                        // aren't starting a polygon fill (where we need it to get to the start point) 
                        if ((gerberState.ApertureState == GerberApertureState.Off)
                            && (!gerberState.IsPolygonAreaFill) && (gerberState.Interpolation != GerberInterpolation.PolygonAreaStart))
                        {
                            // Save the coordinate so the next Net can use it for a start point 
                            gerberState.PreviousX = gerberState.CurrentX;
                            gerberState.PreviousY = gerberState.CurrentY;
                            break;
                        }

                        currentNet = new GerberNet(gerberImage, currentNet, gerberState.Level, gerberState.NetState);
                        // Scale to given coordinate format
                        // XXX only "omit leading zeros".
                        if (gerberImage != null && gerberImage.Format != null)
                        {
                            scaleX = Math.Pow(10.0, gerberImage.Format.DecimalPartX);
                            scaleY = Math.Pow(10.0, gerberImage.Format.DecimalPartY);
                        }

                        scaleX *= scale;
                        scaleY *= scale;
                        currentNet.StartX = gerberState.PreviousX / scaleX;
                        currentNet.StartY = gerberState.PreviousY / scaleY;
                        currentNet.StopX = gerberState.CurrentX / scaleX;
                        currentNet.StopY = gerberState.CurrentY / scaleY;
                        centerX = gerberState.CenterX / scaleX;
                        centerY = gerberState.CenterY / scaleY;
                        switch (gerberState.Interpolation)
                        {
                            case GerberInterpolation.ClockwiseCircular:   // Clockwise.
                                currentNet.CircleSegment = new CircleSegment();
                                if (gerberState.MultiQuadrant)
                                    CalculateCircleSegmentMQ(currentNet, true, centerX, centerY);

                                else
                                    CalculateCircleSegmentSQ(currentNet, true, centerX, centerY);

                                break;

                            case GerberInterpolation.CounterClockwiseCircular:
                                currentNet.CircleSegment = new CircleSegment();
                                if (gerberState.MultiQuadrant)
                                    CalculateCircleSegmentMQ(currentNet, false, centerX, centerY);

                                else
                                    CalculateCircleSegmentSQ(currentNet, false, centerX, centerY);

                                break;

                            case GerberInterpolation.PolygonAreaStart:
                                gerberState.ApertureState = GerberApertureState.On;     // Aperure state set to on for polygon areas.
                                gerberState.PolygonAreaStartNode = currentNet;          // To be able to get back and fill in number of polygon corners.
                                gerberState.IsPolygonAreaFill = true;
                                polygonPoints = 0;
                                break;

                            case GerberInterpolation.PolygonAreaEnd:
                                // Save the calculated bounding box to the master node.
                                gerberState.PolygonAreaStartNode.BoundingBox = boundingBox;
                                gerberState.PolygonAreaStartNode = null;
                                gerberState.IsPolygonAreaFill = false;
                                polygonPoints = 0;
                                boundingBox = new BoundingBox();
                                break;

                            default:
                                break;
                        }

                        // Count number of points in Polygon Area 
                        if (gerberState.IsPolygonAreaFill && gerberState.PolygonAreaStartNode != null)
                        {
                            // "...all lines drawn with D01 are considered edges of the
                            // polygon. D02 closes and fills the polygon."
                            // p.49 rs274xrevd_e.pdf
                            // D02 . state.apertureState == GERBER_APERTURE_STATE_OFF

                            // UPDATE: only end the polygon during a D02 call if we've already
                            // drawn a polygon edge (with D01)

                            if ((gerberState.ApertureState == GerberApertureState.Off &&
                                    gerberState.Interpolation != GerberInterpolation.PolygonAreaStart) && (polygonPoints > 0))
                            {
                                currentNet.Interpolation = GerberInterpolation.PolygonAreaEnd;
                                currentNet = new GerberNet(gerberImage, currentNet, gerberState.Level, gerberState.NetState);
                                currentNet.Interpolation = GerberInterpolation.PolygonAreaStart;
                                gerberState.PolygonAreaStartNode.BoundingBox = boundingBox;
                                gerberState.PolygonAreaStartNode = currentNet;
                                polygonPoints = 0;

                                currentNet = new GerberNet(gerberImage, currentNet, gerberState.Level, gerberState.NetState);
                                currentNet.StartX = gerberState.PreviousX / scaleX;
                                currentNet.StartY = gerberState.PreviousY / scaleY;
                                currentNet.StopX = gerberState.CurrentX / scaleX;
                                currentNet.StopY = gerberState.CurrentY / scaleY;
                            }

                            else if (gerberState.Interpolation != GerberInterpolation.PolygonAreaStart)
                                polygonPoints++;

                        }

                        currentNet.Interpolation = gerberState.Interpolation;
                        // Override circular interpolation if no center was given.
                        // This should be a safe hack, since a good file should always 
                        // include I or J.  And even if the radius is zero, the end point 
                        // should be the same as the start point, creating no line 

                        if (((gerberState.Interpolation == GerberInterpolation.ClockwiseCircular) ||
                             (gerberState.Interpolation == GerberInterpolation.CounterClockwiseCircular)) &&
                             ((gerberState.CenterX == 0.0) && (gerberState.CenterY == 0.0)))
                            currentNet.Interpolation = GerberInterpolation.LinearX1;

                        // If we detected the end of Polygon Area Fill we go back to
                        // the interpolation we had before that.
                        // Also if we detected any of the quadrant flags, since some
                        // gerbers don't reset the interpolation (EagleCad again).
                        if ((gerberState.Interpolation == GerberInterpolation.PolygonAreaStart) || (gerberState.Interpolation == GerberInterpolation.PolygonAreaEnd))
                            gerberState.Interpolation = gerberState.PreviousInterpolation;

                        // Save level polarity and unit
                        currentNet.Level = gerberState.Level;
                        gerberState.CenterX = 0;
                        gerberState.CenterY = 0;
                        currentNet.Aperture = gerberState.CurrentAperture;
                        currentNet.ApertureState = gerberState.ApertureState;

                        // For next round we save the current position as the previous position
                        gerberState.PreviousX = gerberState.CurrentX;
                        gerberState.PreviousY = gerberState.CurrentY;

                        // If we have an aperture defined at the moment we find min and max of image with compensation for mm.
                        if ((currentNet.Aperture == 0) && !gerberState.IsPolygonAreaFill)
                            break;

                        // Only update the min/max values and aperture stats if we are drawing.
                        if ((currentNet.ApertureState != GerberApertureState.Off) && (currentNet.Interpolation != GerberInterpolation.PolygonAreaStart))
                        {
                            double repeatOffsetX = 0.0;
                            double repeatOffsetY = 0.0;

                            // Update stats with current aperture number if not in polygon
                            if (!gerberState.IsPolygonAreaFill)
                            {
                                //Debug.WriteLine("    Found D code: adding 1 to D list.");
                                if (!gerberStats.IncrementDListCount(currentNet.Aperture, 1))
                                {
                                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Found undefined D code D{0}.\n", currentNet.Aperture);
                                    gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                    gerberStats.UnknownDCodes++;
                                }
                            }

                            //  If step_and_repeat (%SR%) is used, check min_x, max_y etc for
                            //  the ends of the step_and_repeat lattice. This goes wrong in 
                            //  the case of negative dist_X or dist_Y, in which case we 
                            //  should compare against the start points of the lines, not 
                            //  the stop points, but that seems an uncommon case (and the 
                            //  error isn't very big any way).

                            repeatOffsetX = (gerberState.Level.StepAndRepeat.X - 1) * gerberState.Level.StepAndRepeat.DistanceX;
                            repeatOffsetY = (gerberState.Level.StepAndRepeat.Y - 1) * gerberState.Level.StepAndRepeat.DistanceY;

                            // Offset image.
                            apertureMatrix.Translate((float)gerberImage.ImageInfo.OffsetA, (float)gerberImage.ImageInfo.OffsetB);

                            // Rotate image.
                            apertureMatrix.Rotate((float)gerberImage.ImageInfo.ImageRotation);

                            // It's a new level, so recalculate the new transformation matrix for it do any rotations 
                            apertureMatrix.Rotate((float)gerberState.Level.Rotation);

                            // Apply image scale factor.
                            apertureMatrix.Scale((float)gerberState.NetState.ScaleA, (float)gerberState.NetState.ScaleB);

                            // Apply offset.
                            apertureMatrix.Translate((float)gerberState.NetState.OffsetA, (float)gerberState.NetState.OffsetB);

                            // Apply mirror. 
                            switch (gerberState.NetState.MirrorState)
                            {
                                case GerberMirrorState.FlipA:
                                    apertureMatrix.Scale(-1, 1);
                                    break;

                                case GerberMirrorState.FlipB:
                                    apertureMatrix.Scale(1, -1);
                                    break;

                                case GerberMirrorState.FlipAB:
                                    apertureMatrix.Scale(-1, -1);
                                    break;

                                default:
                                    break;
                            }

                            // Finally, apply axis select 
                            if (gerberState.NetState.AxisSelect == GerberAxisSelect.SwapAB)
                            {
                                // We do this by rotating 90 clockwise, then mirroring the Y axis.
                                apertureMatrix.Rotate(90);
                                apertureMatrix.Scale(1, -1);
                            }

                            // If it's a macro, step through all the primitive components and calculate the true bounding box. 
                            if ((gerberImage.ApertureArray[currentNet.Aperture] != null) &&
                                (gerberImage.ApertureArray[currentNet.Aperture].ApertureType == GerberApertureType.Macro))
                            {
                                foreach (SimplifiedApertureMacro macro in gerberImage.ApertureArray[currentNet.Aperture].SimplifiedMacroList)
                                {
                                    PointD offset = new PointD();
                                    double largestDimension;
                                    bool calculatedAlready = false;

                                    apertureMatrix.Reset();
                                    if (macro.ApertureType == GerberApertureType.MacroCircle)
                                    {
                                        offset.X = macro.Parameters[(int)GerberCircleParameters.CentreX];
                                        offset.Y = macro.Parameters[(int)GerberCircleParameters.CentreY];
                                        apertureSize.Width = apertureSize.Height = macro.Parameters[(int)GerberCircleParameters.Diameter];
                                    }

                                    else if (macro.ApertureType == GerberApertureType.MacroOutline)
                                    {
                                        int numberOfPoints = (int)macro.Parameters[(int)GerberOutlineParameters.NumberOfPoints];

                                        for (int pointCount = 0; pointCount <= numberOfPoints; pointCount++)
                                        {
                                            UpdateNetBounds(boundingBox,
                                                currentNet.StopX + macro.Parameters[pointCount * 2 + (int)GerberOutlineParameters.FirstX],
                                                currentNet.StopY + macro.Parameters[pointCount * 2 + (int)GerberOutlineParameters.FirstY],
                                                0, 0);
                                        }

                                        calculatedAlready = true;
                                    }

                                    else if (macro.ApertureType == GerberApertureType.MacroPolygon)
                                    {
                                        offset.X = macro.Parameters[(int)GerberPolygonParameters.CenterX];
                                        offset.Y = macro.Parameters[(int)GerberPolygonParameters.CenterY];
                                        apertureSize.Width = apertureSize.Height = macro.Parameters[(int)GerberPolygonParameters.Diameter];
                                    }

                                    else if (macro.ApertureType == GerberApertureType.MacroMoire)
                                    {
                                        double crossHairLength = macro.Parameters[(int)GerberMoireParameters.CrosshairLength];
                                        double outsideDiameter = macro.Parameters[(int)GerberMoireParameters.OutsideDiameter];
                                        offset.X = macro.Parameters[(int)GerberMoireParameters.CenterX];
                                        offset.Y = macro.Parameters[(int)GerberMoireParameters.CenterY];
                                        // Select the larger of crosshair length and outer diameter.
                                        largestDimension = (crossHairLength > outsideDiameter) ? crossHairLength : outsideDiameter;
                                        apertureSize.Width = apertureSize.Height = largestDimension;
                                    }

                                    else if (macro.ApertureType == GerberApertureType.MacroThermal)
                                    {
                                        offset.X = macro.Parameters[(int)GerberThermalParameters.CenterX];
                                        offset.Y = macro.Parameters[(int)GerberThermalParameters.CenterY];
                                        apertureSize.Width = apertureSize.Height = macro.Parameters[(int)GerberThermalParameters.OutsideDiameter];
                                    }

                                    else if (macro.ApertureType == GerberApertureType.MacroLine20)
                                    {
                                        apertureSize.Width = apertureSize.Height = macro.Parameters[(int)GerberLine20Parameters.LineWidth];
                                        UpdateNetBounds(boundingBox,
                                            currentNet.StopX + macro.Parameters[(int)GerberLine20Parameters.StartX],
                                            currentNet.StopY + macro.Parameters[(int)GerberLine20Parameters.StartY],
                                            apertureSize.Width / 2, apertureSize.Height / 2);

                                        UpdateNetBounds(boundingBox,
                                            currentNet.StopX + macro.Parameters[(int)GerberLine20Parameters.EndX],
                                            currentNet.StopY + macro.Parameters[(int)GerberLine20Parameters.EndY],
                                            apertureSize.Width / 2, apertureSize.Height / 2);

                                        calculatedAlready = true;
                                    }

                                    else if (macro.ApertureType == GerberApertureType.MarcoLine21)
                                    {
                                        largestDimension = Math.Sqrt(macro.Parameters[(int)GerberLine21Parameters.LineWidth] / 2 *
                                                         macro.Parameters[(int)GerberLine21Parameters.LineWidth] / 2 +
                                                         macro.Parameters[(int)GerberLine21Parameters.LineHeight / 2] *
                                                         macro.Parameters[(int)GerberLine21Parameters.LineHeight] / 2);

                                        offset.X = macro.Parameters[(int)GerberLine21Parameters.CenterX];
                                        offset.Y = macro.Parameters[(int)GerberLine21Parameters.CenterY];
                                        apertureSize.Width = apertureSize.Height = largestDimension;
                                        //apertureSize.Width = macro.Parameters[(int)GerberLine21Parameters.LineWidth];
                                        //apertureSize.Height = macro.Parameters[(int)GerberLine21Parameters.LineHeight];
                                        /*float angle = (float)(macro.Parameters[(int)GerberLine21Parameters.Rotation]);
                                        PointF point = new PointF((float)offset.X, (float)offset.Y);
                                        if (angle < 0)
                                            angle += 360.0f;

                                        quadrant = ((int)angle / 90) % 4 + 1;
                                        angle = (float)(macro.Parameters[(int)GerberLine21Parameters.Rotation]);
                                        apertureMatrix.Translate(point.X, point.Y);
                                        apertureMatrix.Rotate(angle);*/
                                    }

                                    else if (macro.ApertureType == GerberApertureType.MacroLine22)
                                    {
                                        largestDimension = Math.Sqrt(macro.Parameters[(int)GerberLine22Parameters.LineWidth] / 2 *
                                                         macro.Parameters[(int)GerberLine22Parameters.LineWidth] / 2 +
                                                         macro.Parameters[(int)GerberLine22Parameters.LineHeight / 2] *
                                                         macro.Parameters[(int)GerberLine22Parameters.LineHeight] / 2);

                                        offset.X = macro.Parameters[(int)GerberLine22Parameters.LowerLeftX] +
                                                   macro.Parameters[(int)GerberLine22Parameters.LineWidth] / 2;

                                        offset.Y = macro.Parameters[(int)GerberLine22Parameters.LowerLeftY] +
                                                   macro.Parameters[(int)GerberLine22Parameters.LineHeight] / 2;

                                        apertureSize.Width = apertureSize.Height = largestDimension;
                                        //apertureSize.Width = macro.Parameters[(int)GerberLine22Parameters.LineWidth];
                                        //apertureSize.Height = macro.Parameters[(int)GerberLine22Parameters.LineHeight];
                                        /*float angle = (float)(macro.Parameters[(int)GerberLine22Parameters.Rotation]);
                                        PointF point = new PointF((float)currentNet.StopX, (float)currentNet.StopY);
                                        if (angle < 0)
                                            angle += 360.0f;

                                        quadrant = ((int)angle / 90) % 4 + 1;
                                        angle = (float)(macro.Parameters[(int)GerberLine22Parameters.Rotation]);
                                        //apertureMatrix.RotateAt(angle, point);
                                        apertureMatrix.Translate(point.X, point.Y);
                                        apertureMatrix.Rotate(angle);*/
                                    }

                                    if (!calculatedAlready)
                                    {
                                        UpdateNetBounds(boundingBox, currentNet.StopX + offset.X, currentNet.StopY + offset.Y,
                                                        apertureSize.Width / 2, apertureSize.Height / 2);
                                    }
                                }
                            }

                            else
                            {
                                if (gerberImage.ApertureArray[currentNet.Aperture] != null)
                                {
                                    apertureSize.Width = gerberImage.ApertureArray[currentNet.Aperture].Parameters[0];
                                    if ((gerberImage.ApertureArray[currentNet.Aperture].ApertureType == GerberApertureType.Rectangle) ||
                                        (gerberImage.ApertureArray[currentNet.Aperture].ApertureType == GerberApertureType.Oval))
                                    {
                                        apertureSize.Height = gerberImage.ApertureArray[currentNet.Aperture].Parameters[1];
                                    }

                                    else
                                        apertureSize.Height = apertureSize.Width;
                                }

                                else
                                    apertureSize.Width = apertureSize.Height = 0;  // This is usually for polygon fills, where the aperture width is "zero"

                                // If it's an arc path, use a special calculation. 
                                if ((currentNet.Interpolation == GerberInterpolation.ClockwiseCircular) ||
                                     (currentNet.Interpolation == GerberInterpolation.CounterClockwiseCircular))
                                {
                                    // To calculate the arc bounding size, we chop it into 1 degree steps, calculate
                                    // the point at each step, and use it to figure out the bounding size.
                                    // **** This seems only to be accurate for a circular aperture ****
                                    double sweepAngle = currentNet.CircleSegment.SweepAngle;
                                    int steps = (int)(Math.Abs(sweepAngle));
                                    PointD temp = new PointD();

                                    for (int i = 0; i <= steps; i++)
                                    {
                                        temp.X = currentNet.CircleSegment.CenterX + currentNet.CircleSegment.Width / 2.0 *
                                                Math.Cos((currentNet.CircleSegment.StartAngle + (sweepAngle * i) / steps) * Math.PI / 180);

                                        temp.Y = currentNet.CircleSegment.CenterY + currentNet.CircleSegment.Height / 2.0 *
                                                 Math.Sin((currentNet.CircleSegment.StartAngle + (sweepAngle * i) / steps) * Math.PI / 180);

                                        UpdateNetBounds(boundingBox, temp.X, temp.Y,
                                                        apertureSize.Width / 2, apertureSize.Height / 2);
                                    }
                                }

                                else
                                {
                                    // Check both the start and stop of the aperture points against a running min/max counter 
                                    // Note: only check start coordinate if this isn't a flash, 
                                    // since the start point may be invalid if it is a flash. 
                                    if (currentNet.ApertureState != GerberApertureState.Flash)
                                        UpdateNetBounds(boundingBox, currentNet.StartX, currentNet.StartY,
                                                        apertureSize.Width / 2, apertureSize.Height / 2);  // Start points

                                    UpdateNetBounds(boundingBox, currentNet.StopX, currentNet.StopY,
                                                    apertureSize.Width / 2, apertureSize.Height / 2);  // Stop points.
                                }
                            }

                            // Update the info bounding box with this latest bounding box 
                            // don't change the bounding box if the polarity is clear 
                            if (gerberState.Level.Polarity != GerberPolarity.Clear)
                                UpdateImageBounds(boundingBox, repeatOffsetX, repeatOffsetY, gerberImage);

                            // Optionally update the knockout measurement box.
                            if (knockoutMeasure)
                            {
                                if (boundingBox.Left < knockoutLimitXmin)
                                    knockoutLimitXmin = boundingBox.Left;

                                if (boundingBox.Right + repeatOffsetX > knockoutLimitXmax)
                                    knockoutLimitXmax = boundingBox.Right + repeatOffsetX;

                                if (boundingBox.Bottom < knockoutLimitYmin)
                                    knockoutLimitYmin = boundingBox.Bottom;

                                if (boundingBox.Top + repeatOffsetY > knockoutLimitYmax)
                                    knockoutLimitYmax = boundingBox.Top + repeatOffsetY;
                            }

                            // If we're not in a polygon fill, then update the current object bounding box
                            // and instansiate a new one for the next net.
                            if (!gerberState.IsPolygonAreaFill)
                            {
                                currentNet.BoundingBox = boundingBox;
                                boundingBox = new BoundingBox();
                            }
                        }

                        apertureMatrix.Dispose();
                        break;

                    // Ignore white space or null characters.
                    case '\n':
                    case '\r':
                    case ' ':
                    case '\t':
                    case '\0':
                        break;

                    default:
                        errorMessage = String.Format(CultureInfo.CurrentCulture, "Found unknown character {0}.", nextCharacter);
                        gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                        gerberStats.UnknownCount++;
                        break;
                }
            }

            return foundEOF;
        }

        // Process G codes.
        private static void ParseGCode(GerberLineReader lineReader, GerberState gerberState, GerberImage gerberImage)
        {
            int intValue;
            GerberNetState geberNetState;
            GerberFileStats gerberStats = gerberImage.GerberStats;
            int length = 0;

            intValue = lineReader.GetIntegerValue(ref length);
            if (lineReader.EndOfFile)
            {
                errorMessage = String.Format(CultureInfo.CurrentCulture, "Unexpected EOF found processing file {0}.", lineReader.FileName);
                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError);
            }

            ////Debug.WriteLine("    ParseGCcode: found G{0:d2} ", intValue);
            switch (intValue)
            {
                case 0:  // Move ... Is this doing anything really?
                    gerberStats.G0++;
                    break;

                case 1:  // Linear Interpolation (1X scale)
                    gerberState.Interpolation = GerberInterpolation.LinearX1;
                    gerberStats.G1++;
                    break;

                case 2:  // Clockwise Linear Interpolation
                    gerberState.Interpolation = GerberInterpolation.ClockwiseCircular;
                    gerberStats.G2++;
                    break;

                case 3:  // Counter Clockwise Linear Interpolation.
                    gerberState.Interpolation = GerberInterpolation.CounterClockwiseCircular;
                    gerberStats.G3++;
                    break;

                case 4:  // Ignore comment blocks.
                    gerberStats.G4++;
                    lineReader.Position = lineReader.LineLength;

                    break;

                case 10: // Linear Interpolation (10X scale)
                    gerberState.Interpolation = GerberInterpolation.LinearX10;
                    gerberStats.G10++;
                    break;

                case 11: // Linear Interpolation (0.1X scale)
                    gerberState.Interpolation = GerberInterpolation.LinearX01;
                    gerberStats.G11++;
                    break;

                case 12: // Linear Interpolation (0.01X scale)
                    gerberState.Interpolation = GerberInterpolation.LinearX001;
                    gerberStats.G12++;
                    break;

                case 36: // Turn on Polygon Area Fill
                    gerberState.PreviousInterpolation = gerberState.Interpolation;
                    gerberState.Interpolation = GerberInterpolation.PolygonAreaStart;
                    gerberState.ChangedState = true;
                    gerberStats.G36++;
                    break;

                case 37: // Turn off Polygon Area Fill 
                    gerberState.Interpolation = GerberInterpolation.PolygonAreaEnd;
                    gerberState.ChangedState = true;
                    gerberStats.G37++;
                    break;

                case 54: // Tool prepare - XXX Maybe uneccesary???
                    if (lineReader.Read() == 'D')
                    {
                        int apartureNumber = lineReader.GetIntegerValue(ref length);

                        if ((apartureNumber >= 0) && (apartureNumber <= MaximumApertures))
                            gerberState.CurrentAperture = apartureNumber;

                        else
                        {
                            errorMessage = String.Format(CultureInfo.CurrentCulture, "Found aperture D{0} out of bounds.", apartureNumber);
                            gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                        }
                    }

                    else
                    {
                        errorMessage = String.Format(CultureInfo.CurrentCulture, "Found unexpected code after G54.");
                        gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                    }

                    gerberStats.G54++;
                    break;

                case 55: // Prepare for flash.
                    gerberStats.G55++;
                    break;

                case 70: // Specify inches.
                    geberNetState = new GerberNetState(gerberImage);
                    geberNetState.Unit = GerberUnit.Inch;
                    gerberState.NetState = geberNetState;
                    gerberStats.G70++;
                    break;

                case 71: // Specify millimeters
                    geberNetState = new GerberNetState(gerberImage);
                    geberNetState.Unit = GerberUnit.Millimeter;
                    gerberState.NetState = geberNetState;
                    gerberStats.G71++;
                    break;

                case 74: // Disable 360 circular interpolation.
                    gerberState.MultiQuadrant = false;
                    gerberStats.G74++;
                    break;

                case 75: // Enable 360 circular interpolation.
                    gerberState.MultiQuadrant = true;
                    gerberStats.G75++;
                    break;

                case 90: // Specify absolute format.
                    if (gerberImage.Format != null)
                        gerberImage.Format.Coordinate = GerberCoordinate.Absolute;

                    gerberStats.G90++;
                    break;

                case 91: // Specify incremental format.
                    if (gerberImage.Format != null)
                        gerberImage.Format.Coordinate = GerberCoordinate.Incremental;

                    gerberStats.G91++;
                    break;

                default:
                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Found unknown G code G{0}.", intValue);
                    gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Ignoring unknown G code.");
                    gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.FileName, lineReader.LineNumber);
                    gerberStats.UnknowGCodes++;
                    break;
            }

            return;
        }

        // Process D codes.
        private static void ParseDCode(GerberLineReader lineReader, GerberState gerberState, GerberImage gerberImage)
        {
            int intValue;
            int length = 0;
            GerberFileStats stats = gerberImage.GerberStats;

            intValue = lineReader.GetIntegerValue(ref length);
            if (lineReader.EndOfFile)
            {
                errorMessage = String.Format(CultureInfo.CurrentCulture, "Unexpected EOF found processing file {0}.", lineReader.FileName);
                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError);
            }

            //Debug.WriteLine("    ParseDCcode: found D{0:d2} ", intValue);
            switch (intValue)
            {
                case 0: // Invalid code.
                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Found invalid D code {0}.", intValue);
                    stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                    stats.DCodeErrors++;
                    break;

                case 1: // Exposure on.
                    gerberState.ApertureState = GerberApertureState.On;
                    gerberState.ChangedState = true;
                    stats.D1++;
                    break;

                case 2: // Exposure off.
                    gerberState.ApertureState = GerberApertureState.Off;
                    gerberState.ChangedState = true;
                    stats.D2++;
                    break;

                case 3: // Flash aperture.
                    gerberState.ApertureState = GerberApertureState.Flash;
                    gerberState.ChangedState = true;
                    stats.D3++;
                    break;

                default: // Aperture in use.
                    if ((intValue >= 0) && (intValue <= MaximumApertures))
                        gerberState.CurrentAperture = intValue;

                    else
                    {
                        errorMessage = String.Format(CultureInfo.CurrentCulture, "Found reference out of bounds in aperture D{0}.", intValue);
                        stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                        stats.DCodeErrors++;
                    }

                    gerberState.ChangedState = false;
                    break;
            }

            return;
        }

        // Parse M codes.
        private static int ParseMCode(GerberLineReader lineReader, GerberImage gerberImage)
        {
            int intValue;
            int length = 0;
            int rtnValue = 0;
            GerberFileStats stats = gerberImage.GerberStats;

            intValue = lineReader.GetIntegerValue(ref length);
            if (lineReader.EndOfFile)
            {
                errorMessage = String.Format(CultureInfo.CurrentCulture, "Unexpected EOF found processing file {0}.", lineReader.FileName);
                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError);
            }

            //Debug.WriteLine("    ParseMCcode: found M{0:d2} ", intValue);
            switch (intValue)
            {
                case 0:  // Program stop
                    stats.M0++;
                    rtnValue = 1;
                    break;

                case 1:  // Optional stop.
                    stats.M1++;
                    rtnValue = 2;
                    break;

                case 2:  // End of program.
                    stats.M2++;
                    rtnValue = 3;
                    break;

                default:
                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Found unknown M code M{0}.", intValue);
                    stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Ignoring unknown M code.");
                    stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                    stats.UnknownMCodes++;
                    break;
            }

            return rtnValue;
        }

        private static void ParseRS274X(GerberImage gerberImage, GerberState gerberState, GerberNet currentNet, GerberLineReader lineReader)
        {
            int intValue;
            int apertureNumber;
            string stringValue;
            ApertureDefinition aperture = null;
            ApertureMacro apertureMacro;
            GerberFileStats stats = gerberImage.GerberStats;
            char nextCharacter;
            int length = 0;
            float scale = 1.0f;

            if (gerberState.NetState.Unit == GerberUnit.Millimeter)
                scale = 25.4f;

            stringValue = lineReader.GetStringValue(2);

            if (lineReader.CurrentLine == null)
            {
                errorMessage = string.Format(CultureInfo.CurrentCulture, "Unexpected EOF found in file {0}", lineReader.FileName);
                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError);
            }

            switch (stringValue)
            {
                // Directive parameters. 
                case "AS": // Axis Select   ** Depreciated but included for legacy files **
                    gerberState.NetState = new GerberNetState(gerberImage);
                    stringValue = lineReader.GetStringValue(2);
                    if (stringValue == "AY" || stringValue == "BX")
                        gerberState.NetState.AxisSelect = GerberAxisSelect.SwapAB;

                    else
                        gerberState.NetState.AxisSelect = GerberAxisSelect.None;

                    stringValue = lineReader.GetStringValue(2);
                    if (stringValue == "AY" || stringValue == "BX")
                        gerberState.NetState.AxisSelect = GerberAxisSelect.SwapAB;

                    else
                        gerberState.NetState.AxisSelect = GerberAxisSelect.None;

                    break;

                case "FS": // Format Statement.
                    gerberImage.Format = new GerberFormat();
                    nextCharacter = lineReader.Read();
                    switch (nextCharacter)
                    {
                        case 'L':
                            gerberImage.Format.OmitZeros = GerberOmitZero.OmitZerosLeading;
                            break;

                        case 'T':
                            gerberImage.Format.OmitZeros = GerberOmitZero.OmitZerosTrailing;
                            break;

                        case 'D':
                            gerberImage.Format.OmitZeros = GerberOmitZero.OmitZerosExplicit;
                            break;

                        default:
                            errorMessage = "Undefined handling of zeros in format code.";
                            stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                            errorMessage = "Defaulting to omit leading zeros.";
                            stats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.FileName, lineReader.LineNumber);
                            gerberImage.Format.OmitZeros = GerberOmitZero.OmitZerosLeading;
                            lineReader.Position--;
                            break;
                    }

                    nextCharacter = lineReader.Read();
                    switch (nextCharacter)
                    {
                        case 'A':
                            gerberImage.Format.Coordinate = GerberCoordinate.Absolute;
                            break;

                        case 'I':
                            gerberImage.Format.Coordinate = GerberCoordinate.Incremental;
                            break;

                        default:
                            errorMessage = "Invalid coordinate type defined in format code.";
                            stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                            errorMessage = "Defaulting to absolute coordinates.";
                            stats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.FileName, lineReader.LineNumber);
                            gerberImage.Format.Coordinate = GerberCoordinate.Absolute;
                            break;
                    }

                    nextCharacter = lineReader.Read();
                    while (nextCharacter != '*')
                    {
                        switch (nextCharacter)
                        {
                            case 'N':
                                gerberImage.Format.SequenceNumberLimit = nextCharacter - '0';
                                break;

                            case 'G':
                                gerberImage.Format.GeneralFunctionLimit = nextCharacter - '0';
                                break;

                            case 'D':
                                gerberImage.Format.PlotFunctionLimit = nextCharacter - '0';
                                break;

                            case 'M':
                                gerberImage.Format.MiscFunctionLimit = (nextCharacter & 0xff) - '0';
                                break;

                            case 'X':
                                nextCharacter = lineReader.Read();
                                if ((nextCharacter < '0') || (nextCharacter > '6'))
                                {
                                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Illegal format size {0}.", nextCharacter);
                                    stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                }

                                gerberImage.Format.IntegralPartX = nextCharacter - '0';
                                nextCharacter = lineReader.Read();
                                if ((nextCharacter < '0') || (nextCharacter > '6'))
                                {
                                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Illegal format size {0}.", nextCharacter);
                                    stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                }

                                gerberImage.Format.DecimalPartX = nextCharacter - '0';
                                break;

                            case 'Y':
                                nextCharacter = lineReader.Read();
                                if ((nextCharacter < '0') || (nextCharacter > '6'))
                                {
                                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Illegal format size {0}.", nextCharacter);
                                    stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                }

                                gerberImage.Format.IntegralPartY = nextCharacter - '0';
                                nextCharacter = lineReader.Read();
                                if ((nextCharacter < '0') || (nextCharacter > '6'))
                                {
                                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Illegal format size {0}.", nextCharacter);
                                    stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                }

                                gerberImage.Format.DecimalPartY = nextCharacter - '0';
                                break;

                            default:
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Illegal format size {0}.", nextCharacter);
                                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Ignoring invalid format statement.");
                                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.FileName, lineReader.LineNumber);
                                break;
                        }

                        nextCharacter = lineReader.Read();
                    }

                    break;

                case "MI": // Mirror Image  ** Depreciated, but included for legacy files **
                    gerberState.NetState = new GerberNetState(gerberImage);
                    nextCharacter = lineReader.Read();
                    while (nextCharacter != '*')
                    {
                        switch (nextCharacter)
                        {
                            case 'A':
                                intValue = lineReader.GetIntegerValue(ref length);
                                if (intValue == 1)
                                {
                                    if (gerberState.NetState.MirrorState == GerberMirrorState.FlipB)
                                        gerberState.NetState.MirrorState = GerberMirrorState.FlipAB;

                                    else
                                        gerberState.NetState.MirrorState = GerberMirrorState.FlipA;
                                }
                                break;

                            case 'B':
                                intValue = lineReader.GetIntegerValue(ref length);
                                if (intValue == 1)
                                {
                                    if (gerberState.NetState.MirrorState == GerberMirrorState.FlipA)
                                        gerberState.NetState.MirrorState = GerberMirrorState.FlipAB;

                                    else
                                        gerberState.NetState.MirrorState = GerberMirrorState.FlipB;
                                }
                                break;

                            default:
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Invalid character in mirror:{0}", nextCharacter);
                                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                break;
                        }

                        nextCharacter = lineReader.Read();
                    }
                    break;

                case "MO": // Mode of Units.
                    stringValue = lineReader.GetStringValue(2);
                    switch (stringValue)
                    {
                        case "IN":
                            gerberState.NetState = new GerberNetState(gerberImage);
                            gerberState.NetState.Unit = GerberUnit.Inch;
                            break;

                        case "MM":
                            gerberState.NetState = new GerberNetState(gerberImage);
                            gerberState.NetState.Unit = GerberUnit.Millimeter;
                            break;

                        default:
                            errorMessage = String.Format(CultureInfo.CurrentCulture, "Illegal unit of measure:{0}.", stringValue);
                            stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                            break;
                    }

                    break;

                case "OF": // Offset.
                    nextCharacter = lineReader.Read();
                    while (nextCharacter != '*')
                    {
                        switch (nextCharacter)
                        {
                            case 'A':
                                gerberState.NetState.OffsetA = lineReader.GetDoubleValue() / scale;
                                break;

                            case 'B':
                                gerberState.NetState.OffsetB = lineReader.GetDoubleValue() / scale;
                                break;

                            default:
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Invalid character in offset:{0}.", nextCharacter);
                                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                break;
                        }

                        nextCharacter = lineReader.Read();
                    }
                    break;

                case "IF": // Include File  ** Depreciated, but included for legacy files **
                    {
                        // Assuming here that the include file is in fact a RS-274X spec file.
                        // and is in the same directory as the calling file.
                        GerberLineReader includeLineReader;
                        string includeFileName = lineReader.GetStringValue('*');
                        string includeFilePath = lineReader.FilePath + "\\" + includeFileName;

                        if (levelOfRecursion < 9)   // 0..9
                        {
                            if (File.Exists(includeFilePath))
                            {
                                using (StreamReader fileStream = new StreamReader(includeFilePath, Encoding.ASCII))
                                {
                                    includeLineReader = new GerberLineReader(fileStream);
                                    includeLineReader.FileName = includeFileName;
                                    includeLineReader.FilePath = lineReader.FilePath;   // Same path as caller.
                                    lineReaderList.Add(includeLineReader);
                                    levelOfRecursion++;
                                    ParseFileSegment(gerberImage, gerberState, currentNet);
                                    lineReaderList.RemoveAt(levelOfRecursion);
                                    levelOfRecursion--;
                                }

                            }

                            else
                            {
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Include file {0} not found.", includeFileName);
                                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberCritical, lineReader.FileName, lineReader.LineNumber);
                            }
                        }

                        else
                        {
                            errorMessage = "More than 10 levels of include file recursion";
                            stats.AddNewError(-1, errorMessage, GerberErrorType.GerberCritical, lineReader.FileName, lineReader.LineNumber);
                        }
                    }

                    break;

                case "SF": // Scale Factor  ** Depreciated, but included for legacy files **
                    gerberState.NetState = new GerberNetState(gerberImage);
                    if (lineReader.Read() == 'A')
                    {
                        imageScaleA = lineReader.GetDoubleValue();
                        gerberState.NetState.ScaleA = imageScaleA;
                    }

                    if (lineReader.Read() == 'B')
                    {
                        imageScaleB = lineReader.GetDoubleValue();
                        gerberState.NetState.ScaleB = imageScaleB;
                    }
                    break;


                case "IO": // Image offset. ** Depreciated, but included for legacy files **
                    nextCharacter = lineReader.Read();
                    while (nextCharacter != '*')
                    {
                        switch (nextCharacter)
                        {
                            case 'A':
                                gerberImage.ImageInfo.OffsetA = lineReader.GetDoubleValue() / scale;
                                break;

                            case 'B':
                                gerberImage.ImageInfo.OffsetB = lineReader.GetDoubleValue() / scale;
                                break;

                            default:
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Invalid character in image offset:{0}.", nextCharacter);
                                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                break;
                        }

                        nextCharacter = lineReader.Read();
                    }
                    break;

                case "IJ": // Image Justify  ** Depreciated, but included for legacy files **
                    nextCharacter = lineReader.Read();
                    gerberImage.ImageInfo.ImageJustifyTypeA = GerberImageJustifyType.LowerLeft;
                    gerberImage.ImageInfo.ImageJustifyTypeB = GerberImageJustifyType.LowerLeft;
                    gerberImage.ImageInfo.ImageJustifyOffsetA = 0.0;
                    gerberImage.ImageInfo.ImageJustifyOffsetB = 0.0;
                    while (nextCharacter != '*')
                    {
                        switch (nextCharacter)
                        {
                            case 'A':
                                nextCharacter = lineReader.Read();
                                if (nextCharacter == 'C')
                                    gerberImage.ImageInfo.ImageJustifyTypeA = GerberImageJustifyType.Centre;

                                else if (nextCharacter == 'L')
                                    gerberImage.ImageInfo.ImageJustifyTypeA = GerberImageJustifyType.LowerLeft;

                                else
                                {
                                    lineReader.Position--;
                                    gerberImage.ImageInfo.ImageJustifyOffsetA = (lineReader.GetDoubleValue() / scale);
                                }
                                break;

                            case 'B':
                                nextCharacter = lineReader.Read();
                                if (nextCharacter == 'C')
                                    gerberImage.ImageInfo.ImageJustifyTypeB = GerberImageJustifyType.Centre;

                                else if (nextCharacter == 'L')
                                    gerberImage.ImageInfo.ImageJustifyTypeB = GerberImageJustifyType.LowerLeft;

                                else
                                {
                                    lineReader.Position--;
                                    gerberImage.ImageInfo.ImageJustifyOffsetB = (lineReader.GetDoubleValue() / scale);
                                }
                                break;

                            default:
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Wrong character in image justify:{0}.", nextCharacter);
                                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                break;
                        }

                        nextCharacter = lineReader.Read();
                    }
                    break;

                case "IN": // Image Name.   ** Depreciated, but included for legacy files **
                    gerberImage.ImageInfo.ImageName = lineReader.GetStringValue('*');
                    break;

                case "IP": // Image Polarity
                    stringValue = lineReader.GetStringValue(3);
                    if (stringValue == "POS")
                        gerberImage.ImageInfo.Polarity = GerberPolarity.Positive;

                    else if (stringValue == "NEG")
                        gerberImage.ImageInfo.Polarity = GerberPolarity.Negative;

                    else
                    {
                        errorMessage = String.Format(CultureInfo.CurrentCulture, "Unknown polarity:{0}.", stringValue);
                        stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                    }
                    break;

                case "IR": // Image Rotation ** Depreciated, but included for legacy files.
                    imageRotation = lineReader.GetIntegerValue(ref length) % 360;
                    if (imageRotation == 0 || imageRotation == 90 || imageRotation == 180 || imageRotation == 270)
                        gerberImage.ImageInfo.ImageRotation = imageRotation;

                    else
                    {
                        errorMessage = String.Format(CultureInfo.CurrentCulture,
                            "Image rotation must be 0, 90, 180 or 270 [{0}° is invalid].", imageRotation);

                        stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                        imageRotation = 0.0;
                    }
                    break;

                case "PF": // Plotter Film ** Depreciated, but included for legacy files **
                    gerberImage.ImageInfo.PlotterFilm = lineReader.GetStringValue('*');
                    break;

                // Aperture parameters
                case "AD": // Aperture Definition.
                    aperture = new ApertureDefinition();
                    apertureNumber = ParseApertureDefinition(aperture, gerberImage, scale, lineReader);

                    if ((apertureNumber >= 0) && (apertureNumber <= MaximumApertures))
                    {
                        aperture.Unit = gerberState.NetState.Unit;
                        gerberImage.ApertureArray[apertureNumber] = aperture;
                        //Debug.WriteLine("In parseRS274X: adding new aperture.");
                        stats.AddNewAperture(-1, apertureNumber, aperture.ApertureType, aperture.Parameters);
                        stats.AddNewDList(apertureNumber);
                        if (apertureNumber < MinimumAperture)
                        {
                            errorMessage = String.Format(CultureInfo.CurrentCulture, "Aperture number {0} out of lower bounds.", apertureNumber);
                            stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                        }
                    }

                    else
                    {
                        errorMessage = String.Format(CultureInfo.CurrentCulture, "Aperture number {0} out of upper bounds.", apertureNumber);
                        stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                    }

                    break;

                case "AM": // Aperture Macro.
                    //Debug.WriteLine(String.Format("Found {0} command in Line: {1}", stringValue, lineReader.LineNumber));
                    apertureMacro = ApertureMacro.ProcessApertureMacro(lineReader);
                    if (apertureMacro != null)
                        gerberImage.ApertureMacroList.Add(apertureMacro);

                    else
                    {
                        errorMessage = "Failed to read aperture macro.";
                        stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                    }

                    // Return, since we want to skip the later back-up loop.
                    return;

                // Level Commands
                case "LN": // Level Name.
                    gerberState.Level = new GerberLevel(gerberImage);
                    gerberState.Level.LevelName = lineReader.GetStringValue('*');
                    break;

                case "LP": // Level Polarity.
                    gerberState.Level = new GerberLevel(gerberImage);
                    nextCharacter = lineReader.Read();
                    switch (nextCharacter)
                    {
                        case 'D': // Dark Polarity (default).
                            gerberState.Level.Polarity = GerberPolarity.Dark;
                            break;

                        case 'C': // Clear Polarity.
                            gerberState.Level.Polarity = GerberPolarity.Clear;
                            break;

                        default:
                            errorMessage = String.Format(CultureInfo.CurrentCulture, "Unknown level polarity {0}.", nextCharacter);
                            stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                            break;
                    }
                    break;

                case "KO": // Knock Out  ** Depreciated, but included for legacy files **
                    gerberState.Level = new GerberLevel(gerberImage);
                    UpdateKnockoutMeasurements();
                    // Reset any previous knockout measurements.
                    knockoutMeasure = false;
                    nextCharacter = lineReader.Read();
                    if (nextCharacter == '*')
                    {
                        // Disable previous SR parameters.
                        gerberState.Level.Knockout.Type = GerberKnockoutType.NoKnockout;
                        break;
                    }

                    else if (nextCharacter == 'C')
                        gerberState.Level.Knockout.Polarity = GerberPolarity.Clear;

                    else if (nextCharacter == 'D')
                        gerberState.Level.Knockout.Polarity = GerberPolarity.Dark;

                    else
                    {
                        errorMessage = "Knockout must supply a polarity (C, D, or *).";
                        stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                    }

                    gerberState.Level.Knockout.LowerLeftX = 0.0;
                    gerberState.Level.Knockout.LowerLeftY = 0.0;
                    gerberState.Level.Knockout.Width = 0.0;
                    gerberState.Level.Knockout.Height = 0.0;
                    gerberState.Level.Knockout.Border = 0.0;
                    gerberState.Level.Knockout.FirstInstance = true;

                    nextCharacter = lineReader.Read();
                    while ((nextCharacter != '*') && (!lineReader.EndOfFile))
                    {
                        switch (nextCharacter)
                        {
                            case 'X':
                                gerberState.Level.Knockout.Type = GerberKnockoutType.FixedKnockout;
                                gerberState.Level.Knockout.LowerLeftX = lineReader.GetDoubleValue() / scale;
                                break;

                            case 'Y':
                                gerberState.Level.Knockout.Type = GerberKnockoutType.FixedKnockout;
                                gerberState.Level.Knockout.LowerLeftY = lineReader.GetDoubleValue() / scale;
                                break;

                            case 'I':
                                gerberState.Level.Knockout.Type = GerberKnockoutType.FixedKnockout;
                                gerberState.Level.Knockout.Width = lineReader.GetDoubleValue() / scale;
                                break;

                            case 'J':
                                gerberState.Level.Knockout.Type = GerberKnockoutType.FixedKnockout;
                                gerberState.Level.Knockout.Height = lineReader.GetDoubleValue() / scale;
                                break;

                            case 'K':
                                gerberState.Level.Knockout.Type = GerberKnockoutType.Border;
                                gerberState.Level.Knockout.Border = lineReader.GetDoubleValue() / scale;
                                // This is a bordered knockout, so we need to start measuring the size of the area bordering all future components.
                                knockoutMeasure = true;
                                knockoutLimitXmin = Double.MaxValue;
                                knockoutLimitYmin = Double.MaxValue;
                                knockoutLimitXmax = Double.MinValue;
                                knockoutLimitYmax = Double.MinValue;
                                knockoutLevel = gerberState.Level;      // Save a copy of this level for future access.
                                break;

                            default:
                                errorMessage = "Unknown variable in knockout.";
                                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);

                                break;
                        }

                        nextCharacter = lineReader.Read();
                    }

                    break;

                case "SR": // Step and Repeat
                    // Start by generating a new level by duplicating previous level's settings.
                    gerberState.Level = new GerberLevel(gerberImage);
                    nextCharacter = lineReader.Read();
                    if (nextCharacter == '*')
                    {
                        // Disable previous SR parameters.
                        gerberState.Level.StepAndRepeat.X = 1;
                        gerberState.Level.StepAndRepeat.Y = 1;
                        gerberState.Level.StepAndRepeat.DistanceX = 0.0;
                        gerberState.Level.StepAndRepeat.DistanceY = 0.0;
                        break;
                    }

                    while (nextCharacter != '*')
                    {
                        switch (nextCharacter)
                        {
                            // Repeating 0 times in any direction would disable the whole plot, and
                            // is probably not intended. At least one other tool (Viewmate) seems
                            // to interpret 0 times repeating as repeating just once.
                            case 'X':
                                int repeatX = lineReader.GetIntegerValue(ref length);
                                if (repeatX == 0)
                                    repeatX = 1;

                                gerberState.Level.StepAndRepeat.X = repeatX;
                                break;

                            case 'Y':
                                int repeatY = lineReader.GetIntegerValue(ref length);
                                if (repeatY == 0)
                                    repeatY = 1;

                                gerberState.Level.StepAndRepeat.Y = repeatY;
                                break;

                            case 'I':
                                gerberState.Level.StepAndRepeat.DistanceX = lineReader.GetDoubleValue() / scale;
                                break;

                            case 'J':
                                gerberState.Level.StepAndRepeat.DistanceY = lineReader.GetDoubleValue() / scale;
                                break;

                            default:
                                errorMessage = "Unknown step-and-repeat parameter.";
                                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                break;
                        }

                        nextCharacter = lineReader.Read();
                    }

                    break;

                case "RO":  // Level Rotate  ** Depreciated, but included for legacy files ** ????
                    gerberState.Level.Rotation = lineReader.GetDoubleValue();
                    nextCharacter = lineReader.Read();
                    if (nextCharacter != '*')
                    {

                        errorMessage = "Error in level rotation command.\n";
                        stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                    }
                    break;


                default:
                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Unknown or unsupported command {0}.", stringValue);
                    stats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.FileName, lineReader.LineNumber);
                    break;
            }

            // Make sure we read until the trailing '*' character
            // First, backspace once in case we already read the trailing '*'
            lineReader.Position--;
            nextCharacter = lineReader.Read();
            while ((!lineReader.EndOfFile) && (nextCharacter != '*'))
                nextCharacter = lineReader.Read();

            return;
        }

        private static int ParseApertureDefinition(ApertureDefinition aperture, GerberImage gerberImage, float scale, GerberLineReader lineReader)
        {
            int apertureNumber;
            int tokenCount;
            double parameterValue;
            GerberFileStats stats = gerberImage.GerberStats;
            string[] tokens;
            string stringValue;
            int parameterIndex = 0;
            int tokenIndex = 0;
            int length = 0;


            if (lineReader.Read() != 'D')
            {
                errorMessage = String.Format(CultureInfo.CurrentCulture, "Found AD code without 'D' code.");
                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                return -1;
            }

            // Get aperture number.
            apertureNumber = lineReader.GetIntegerValue(ref length);

            // Read in the whole aperture defintion and tokenize it.
            stringValue = lineReader.GetStringValue('*');
            tokens = stringValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens == null || tokens.Length == 0)
            {
                errorMessage = String.Format(CultureInfo.CurrentCulture, "Invalid aperture definition.");
                stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                return -1;
            }

            tokenCount = tokens.Length;
            if (tokens[tokenIndex].Length == 1)
            {
                switch (tokens[tokenIndex])
                {
                    case "C":
                        aperture.ApertureType = GerberApertureType.Circle;
                        break;

                    case "R":
                        aperture.ApertureType = GerberApertureType.Rectangle;
                        break;

                    case "O":
                        aperture.ApertureType = GerberApertureType.Oval;
                        break;

                    case "P":
                        aperture.ApertureType = GerberApertureType.Polygon;
                        break;

                    // Here should be a T defined, but I don't know what it represents.
                }

            }

            else
            {
                aperture.ApertureType = GerberApertureType.Macro;
                // In aperture definition, point to the aperture macro used in the defintion
                foreach (ApertureMacro aMacro in gerberImage.ApertureMacroList)
                {
                    if (aMacro.Name.Length == tokens[0].Length && aMacro.Name == tokens[0])
                    {
                        aperture.ApertureMacro = aMacro;
                        break;
                    }
                }
            }

            // Parse all parameters
            tokenIndex++;
            for (; tokenIndex < tokenCount; tokenIndex++)
            {
                if (parameterIndex == MaximumApertureParameters)
                {
                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Maximum allowed parameters exceeded in aperture {0}", apertureNumber);
                    stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                    break;
                }

                tokens = tokens[tokenIndex].Split(new char[] { 'X', 'x' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string token in tokens)
                {
                    if (!double.TryParse(token, out parameterValue))
                    {
                        errorMessage = String.Format(CultureInfo.CurrentCulture, "Failed to read all parameters in aperture {0}", apertureNumber);
                        stats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                        aperture.Parameters[parameterIndex] = 0.0;
                    }

                    else
                    {
                        // Scale any MM values to inches.
                        // Don't scale polygon angles or side numbers, or macro parameters.
                        if (!(((aperture.ApertureType == GerberApertureType.Polygon) &&
                            ((parameterIndex == 1) || (parameterIndex == 2))) ||
                            (aperture.ApertureType == GerberApertureType.Macro)))

                            parameterValue /= scale;

                        //aperture.Parameters.Add(parameterValue);
                        aperture.Parameters[parameterIndex] = parameterValue;
                        parameterIndex++;
                    }
                }
            }

            aperture.ParameterCount = parameterIndex;
            if (aperture.ApertureType == GerberApertureType.Macro)
            {
                //Debug.WriteLine("Simplifying aperture {0} using aperture macro {1}", apertureNumber, aperture.ApertureMacro.Name);
                SimplifiedApertureMacro.SimplifyApertureMacro(aperture, scale);
                //Debug.WriteLine("Done simplifying.");
            }

            return apertureNumber;
        }

        private static void GerberCalculateFinalJustifyEffects(GerberImage gerberImage)
        {
            double translateA = 0.0, translateB = 0.0;

            if (gerberImage.ImageInfo.ImageJustifyTypeA != GerberImageJustifyType.None)
            {
                if (gerberImage.ImageInfo.ImageJustifyTypeA == GerberImageJustifyType.Centre)
                    translateA = (gerberImage.ImageInfo.MaxX - gerberImage.ImageInfo.MinX) / 2.0f;

                else
                    translateA = -gerberImage.ImageInfo.MinX;
            }

            if (gerberImage.ImageInfo.ImageJustifyTypeB != GerberImageJustifyType.None)
            {
                if (gerberImage.ImageInfo.ImageJustifyTypeB == GerberImageJustifyType.Centre)
                    translateB = (gerberImage.ImageInfo.MaxY - gerberImage.ImageInfo.MinY) / 2.0f;

                else
                    translateB = -gerberImage.ImageInfo.MinY;
            }

            // Update the min/max values so the autoscale function can correctly centered a justified image.
            gerberImage.ImageInfo.MinX += translateA + gerberImage.ImageInfo.ImageJustifyOffsetA;
            gerberImage.ImageInfo.MaxX += translateA + gerberImage.ImageInfo.ImageJustifyOffsetA;
            gerberImage.ImageInfo.MinY += translateB + gerberImage.ImageInfo.ImageJustifyOffsetB;
            gerberImage.ImageInfo.MaxY += translateB + gerberImage.ImageInfo.ImageJustifyOffsetB;

            // Store the absolute offset for the justify so we can quickly offset the rendered picture during drawing.
            gerberImage.ImageInfo.ImageJustifyOffsetActualA = translateA + gerberImage.ImageInfo.ImageJustifyOffsetA;
            gerberImage.ImageInfo.ImageJustifyOffsetActualB = translateB + gerberImage.ImageInfo.ImageJustifyOffsetB;
        } /* gerber_calculate_final_justify_effects */

        /// <summary>
        /// Calculates the start, end and sweep angles of a multi-quadrant arc.
        /// </summary>
        /// <param name="currentNet">location to save the segment data</param>
        /// <param name="clockwise">true if clockwise arc</param>
        /// <param name="centerX">center X</param>
        /// <param name="centerY">center Y</param>
        private static void CalculateCircleSegmentMQ(GerberNet currentNet, bool clockwise, double centerX, double centerY)
        {
            double d1x, d1y, d2x, d2y;
            double angle1, angle2;

            currentNet.CircleSegment.CenterX = currentNet.StartX + centerX;
            currentNet.CircleSegment.CenterY = currentNet.StartY + centerY;

            d1x = currentNet.StartX - currentNet.CircleSegment.CenterX;
            d1y = currentNet.StartY - currentNet.CircleSegment.CenterY;
            d2x = currentNet.StopX - currentNet.CircleSegment.CenterX;
            d2y = currentNet.StopY - currentNet.CircleSegment.CenterY;

            angle1 = Math.Atan2(d1y, d1x) * (180 / Math.PI);
            angle2 = Math.Atan2(d2y, d2x) * (180 / Math.PI);

            currentNet.CircleSegment.Width = Math.Sqrt((centerX * centerX) + (centerY * centerY));
            currentNet.CircleSegment.Width *= 2.0;
            currentNet.CircleSegment.Height = currentNet.CircleSegment.Width;
            // Make sure it's always positive angles
            if (angle1 < 0.0)
            {
                angle1 += 360.0;
                angle2 += 360.0;
            }

            if (angle2 < 0.0)
                angle2 += 360.0;

            if (angle2 == 0.0)
                angle2 = 360.0;

            if (clockwise)
            {
                if (angle1 <= angle2)
                    angle2 -= 360;
            }

            else
            {
                if (angle1 >= angle2)
                    angle2 += 360;
            }

            currentNet.CircleSegment.StartAngle = angle1;
            currentNet.CircleSegment.EndAngle = angle2;
            currentNet.CircleSegment.SweepAngle = angle2 - angle1;
            return;
        }

        private static void CalculateCircleSegmentSQ(GerberNet gerberNet, bool clockWise, double delta_cp_x, double delta_cp_y)
        {
            double d1x, d1y, d2x, d2y;
            double alpha, beta;
            int quadrant = 0;

            /*
             * Quadrant detection (based on ccw, converted below if cw)
             *  Y ^
             *    |
             *    |
             *    --->X
             */

            if (gerberNet.StartX > gerberNet.StopX)
            {
                // 1st and 2nd quadrant.
                if (gerberNet.StartY < gerberNet.StopY)
                    quadrant = 1;

                else
                    quadrant = 2;
            }

            else
            {
                // 3rd and 4th quadrant.
                if (gerberNet.StartY > gerberNet.StopY)
                    quadrant = 3;

                else
                    quadrant = 4;
            }

            // If clockwise, rotate quadrant
            if (clockWise)
            {
                switch (quadrant)
                {
                    case 1:
                        quadrant = 3;
                        break;

                    case 2:
                        quadrant = 4;
                        break;

                    case 3:
                        quadrant = 1;
                        break;

                    case 4:
                        quadrant = 2;
                        break;

                    default:
                        break;
                    // GERB_COMPILE_ERROR("Unknow quadrant value while converting to cw\n");
                }
            }

            // Calculate arc center point.
            switch (quadrant)
            {
                case 1:
                    gerberNet.CircleSegment.CenterX = gerberNet.StartX - delta_cp_x;
                    gerberNet.CircleSegment.CenterY = gerberNet.StartY - delta_cp_y;
                    break;

                case 2:
                    gerberNet.CircleSegment.CenterX = gerberNet.StartX + delta_cp_x;
                    gerberNet.CircleSegment.CenterY = gerberNet.StartY - delta_cp_y;
                    break;

                case 3:
                    gerberNet.CircleSegment.CenterX = gerberNet.StartX + delta_cp_x;
                    gerberNet.CircleSegment.CenterY = gerberNet.StartY + delta_cp_y;
                    break;

                case 4:
                    gerberNet.CircleSegment.CenterX = gerberNet.StartX - delta_cp_x;
                    gerberNet.CircleSegment.CenterY = gerberNet.StartY + delta_cp_y;
                    break;

                default:
                    break;
            }

            /*
             * Some good values 
             */
            d1x = Math.Abs(gerberNet.StartX - gerberNet.CircleSegment.CenterX);
            d1y = Math.Abs(gerberNet.StartY - gerberNet.CircleSegment.CenterY);
            d2x = Math.Abs(gerberNet.StopX - gerberNet.CircleSegment.CenterX);
            d2y = Math.Abs(gerberNet.StopY - gerberNet.CircleSegment.CenterY);

            alpha = Math.Atan2(d1y, d1x);
            beta = Math.Atan2(d2y, d2x);

            // Avoid divide by zero when sin(0) = 0 and cos(90) = 0
            gerberNet.CircleSegment.Width = alpha < beta ?
            2 * (d1x / Math.Cos(alpha)) : 2 * (d2x / Math.Cos(beta));
            gerberNet.CircleSegment.Height = alpha > beta ?
            2 * (d1y / Math.Sin(alpha)) : 2 * (d2y / Math.Sin(beta));

            if (alpha < 0.000001 && beta < 0.000001)
                gerberNet.CircleSegment.Height = 0;

            switch (quadrant)
            {
                case 1:
                    gerberNet.CircleSegment.StartAngle = alpha * 180 / Math.PI;
                    gerberNet.CircleSegment.EndAngle = beta * 180 / Math.PI;
                    break;

                case 2:
                    gerberNet.CircleSegment.StartAngle = 180.0 - alpha * 180 / Math.PI;
                    gerberNet.CircleSegment.EndAngle = 180.0 - beta * 180 / Math.PI;
                    break;

                case 3:
                    gerberNet.CircleSegment.StartAngle = 180.0 + alpha * 180 / Math.PI;
                    gerberNet.CircleSegment.EndAngle = 180.0 + beta * 180 / Math.PI;
                    break;

                case 4:
                    gerberNet.CircleSegment.StartAngle = 360.0 - alpha * 180 / Math.PI;
                    gerberNet.CircleSegment.EndAngle = 360.0 - beta * 180 / Math.PI;
                    break;

                default:
                    break;
            }

            gerberNet.CircleSegment.SweepAngle = gerberNet.CircleSegment.EndAngle - gerberNet.CircleSegment.StartAngle;

            /*if (gerberNet.cirseg.width < 0.0)
	        GERB_COMPILE_WARNING("Negative width [%f] in quadrant %d [%f][%f]\n", 
			             gerberNet.cirseg.width, quadrant, alfa, beta);
    
            if (gerberNet.cirseg.height < 0.0)
	        GERB_COMPILE_WARNING("Negative height [%f] in quadrant %d [%f][%f]\n", 
			             gerberNet.cirseg.height, quadrant, RAD2DEG(alfa), RAD2DEG(beta));*/

            return;

        }

        private static void UpdateKnockoutMeasurements()
        {
            if (knockoutMeasure)
            {
                knockoutLevel.Knockout.LowerLeftX = knockoutLimitXmin;
                knockoutLevel.Knockout.LowerLeftY = knockoutLimitYmin;
                knockoutLevel.Knockout.Width = knockoutLimitXmax - knockoutLimitXmin;
                knockoutLevel.Knockout.Height = knockoutLimitYmax - knockoutLimitYmin;
                knockoutMeasure = false;
            }
        }

        /// <summary>
        /// Running update of the final image size.
        /// </summary>
        /// <param name="boundingBox">current net's bounding box</param>
        /// <param name="repeatOffsetX">step and repeat X</param>
        /// <param name="repeatOffsetY">step and repeat Y</param>
        /// <param name="gerberImage">save the update in gerberImage.info</param>
        private static void UpdateImageBounds(BoundingBox boundingBox, double repeatOffsetX, double repeatOffsetY, GerberImage gerberImage)
        {
            if (boundingBox.Left < gerberImage.ImageInfo.MinX)
                gerberImage.ImageInfo.MinX = boundingBox.Left;

            if (boundingBox.Right + repeatOffsetX > gerberImage.ImageInfo.MaxX)
                gerberImage.ImageInfo.MaxX = boundingBox.Right + repeatOffsetX;

            if (boundingBox.Bottom < gerberImage.ImageInfo.MinY)
                gerberImage.ImageInfo.MinY = boundingBox.Bottom;

            if (boundingBox.Top + repeatOffsetY > gerberImage.ImageInfo.MaxY)
                gerberImage.ImageInfo.MaxY = boundingBox.Top + repeatOffsetY;
        }

        /// <summary>
        /// Running update of the current net bounding box.
        /// </summary>
        /// <param name="boundingBox">current net's bounding box</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="apertureSizeX1"></param>
        /// <param name="apertureSizeX2"></param>
        /// <param name="apertureSizeY1"></param>
        /// <param name="apertureSizeY2"></param>
        private static void UpdateNetBounds(BoundingBox boundingBox, double x, double y, double apertureSizeX1, double apertureSizeY1)
        {
            bool transform = false;
            float[] elements = new float[] { 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f };

            // Check if we need to transform.
            for (int c = 0; c < elements.Length; c++)
            {
                if (elements[c] != apertureMatrix.Elements[c])
                {
                    transform = true;
                    break;
                }
            }

            double X1 = x - apertureSizeX1, Y1 = y - apertureSizeY1;
            double X2 = x + apertureSizeX1, Y2 = y + apertureSizeY1;
            PointF[] points = new PointF[] { new PointF((float)X1, (float)Y1), new PointF((float)X2, (float)Y2) };

            // Transform the point to the final rendered position, accounting for any scaling, offsets, mirroring, etc.
            if (transform)
                apertureMatrix.TransformPoints(points);

            // Swap X and Y values if the rotation angle is in quadrant 2 or 4.
            /*if ((quadrant % 2) == 0)
            {
               for(int i = 0; i < points.Length; i++)
                {
                    float px = points[i].X;
                    points[i].X = points[i].Y;
                    points[i].Y = px;
                }
            }*/

            // Check both points against the min/max, since depending on the rotation,
            // mirroring, etc, either point could possibly be a min or max
            if (boundingBox.Left > points[0].X)
                boundingBox.Left = points[0].X;

            if (boundingBox.Left > points[1].X)
                boundingBox.Left = points[1].X;

            if (boundingBox.Right < points[0].X)
                boundingBox.Right = points[0].X;

            if (boundingBox.Right < points[1].X)
                boundingBox.Right = points[1].X;

            if (boundingBox.Bottom > points[0].Y)
                boundingBox.Bottom = points[0].Y;

            if (boundingBox.Bottom > points[1].Y)
                boundingBox.Bottom = points[1].Y;

            if (boundingBox.Top < points[0].Y)
                boundingBox.Top = points[0].Y;

            if (boundingBox.Top < points[1].Y)
                boundingBox.Top = points[1].Y;
        }

        private static void RotateImage(GerberImageInfo imageInfo)
        {
            float angle = (float)imageInfo.ImageRotation;
            float minX = (float)imageInfo.MinX;
            float minY = (float)imageInfo.MinY;
            float maxX = (float)imageInfo.MaxX;
            float maxY = (float)imageInfo.MaxY;

            var corners = new[] { new PointF(minX, minY), new PointF(maxX, minY), new PointF(minX, maxY), new PointF(maxX, maxY) };

            var xc = corners.Select(p => Rotate(p, angle).X);
            var yc = corners.Select(p => Rotate(p, angle).Y);

            imageInfo.MinX = xc.Min();
            imageInfo.MinY = yc.Min();
            imageInfo.MaxX = xc.Max();
            imageInfo.MaxY = yc.Max();
        }

        /// <summary>
        /// Rotates a point around the origin (0,0)
        /// </summary>
        private static PointF Rotate(PointF p, float angle)
        {
            // Convert from degrees to radians.
            var theta = Math.PI * angle / 180;
            return new PointF(
                (float)(Math.Cos(theta) * (p.X) - Math.Sin(theta) * (p.Y)),
                (float)(Math.Sin(theta) * (p.X) + Math.Cos(theta) * (p.Y)));
        }

        /// <summary>
        /// Normalises a coordinate if the trailing zeros are omitted.
        /// </summary>
        /// <param name="integerPart">integer part of the format scale (FS)</param>
        /// <param name="decimalPart">decimal part od the formatscale (FS)</param>
        /// <param name="length">length of coordinate</param>
        /// <param name="coordinate">coordinate value</param>
        private static void NormaliseCoordinate(int integerPart, int decimalPart, int length, ref int coordinate)
        {
            switch ((integerPart + decimalPart) - length)
            {
                case 7:
                    coordinate *= 10000000;
                    break;

                case 6:
                    coordinate *= 1000000;
                    break;

                case 5:
                    coordinate *= 100000;
                    break;

                case 4:
                    coordinate *= 10000;
                    break;

                case 3:
                    coordinate *= 1000;
                    break;

                case 2:
                    coordinate *= 100;
                    break;

                case 1:
                    coordinate *= 10;
                    break;
            }
        }

        public static bool IsGerber274D(string fullPathName)
        {
            return false;
        }
    }
}
