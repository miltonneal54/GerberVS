/* Gerber.cs - Handles processing of Gerber files. */

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

        private static List<GerberLineReader> lineReaderList;   // List of line readers for use of "include files". Depreciated but supported.

        private static bool foundEOF = false;                   // Will be set true if a gerber program stop/end command is found.
        private static int levelOfRecursion = 0;                // Keeps track of included file levels.
        private static string errorMessage = String.Empty;

        private static double imageScaleA = 1.0;
        private static double imageScaleB = 1.0;
        private static double imageRotation = 0.0;

        private static Matrix apertureMatrix;
        private static GerberLineReader lineReader = null;
        private static GerberFileStats gerberStats = null;

        // Knockout variables.
        private static bool knockoutMeasure = false;
        private static double knockoutLimitXmin, knockoutLimitYmin, knockoutLimitXmax, knockoutLimitYmax;
        private static GerberLevel knockoutLevel = null;

        public static bool IsGerber274D(string fullPathName)
        {
            return false;
        }

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
        /// <param name="gerberFileName">full path and file name of the gerber file</param>
        /// <returns>gerber image</returns>
        /// <remarks>
        /// This is a wrapper which gets called from top level. 
        /// It does some initialization and pre-processing, and 
        /// then calls GerberParseFileSegment method which
        /// processes the actual file. Then it does final 
        /// modifications to the image created.
        /// </remarks>
        public static GerberImage ParseGerber(string gerberFileName)
        {
            lineReaderList = new List<GerberLineReader>();  // Keep a list of linereaders, used for include files (IF).
            string fileName = Path.GetFileName(gerberFileName);

            // Create new state. This is used locally to keep track of the state as the Gerber is read in.
            GerberState gerberStateLocal = new GerberState();

            // Create new image. This will be returned.
            GerberImage gerberImage = new GerberImage("RS274-X (Gerber) File");
            gerberImage.FileType = GerberFileType.RS274X;
            gerberStats = gerberImage.GerberStats;  // Maintains the stats as the file is read in.

            // Set active Netlist, Level and NetState to point to first default ones created in GerberImage constructor. 
            GerberNet currentNet = new GerberNet(gerberImage);   // Create the first gerberNet filled with some initial values and add it to the GerberNetList.
            gerberStateLocal.Level = gerberImage.LevelList[0];
            gerberStateLocal.NetState = gerberImage.NetStateList[0];
            currentNet.Level = gerberStateLocal.Level;
            currentNet.NetState = gerberStateLocal.NetState;

            // Start parsing.
            //Debug.WriteLine(String.Format("Starting to parse Gerber file: {0}", fileName));
            using (StreamReader gerberFileStream = new StreamReader(gerberFileName, Encoding.ASCII))
            {
                lineReader = new GerberLineReader(gerberFileStream);
                lineReader.FileName = Path.GetFileName(gerberFileName);
                lineReader.FilePath = Path.GetDirectoryName(gerberFileName);
                lineReaderList.Add(lineReader);
                foundEOF = ParseGerberSegment(gerberImage, gerberStateLocal, currentNet);
            }

            if (!foundEOF)
            {
                errorMessage = String.Format(CultureInfo.CurrentCulture, "File {0} is missing Gerber EOF code.", lineReader.FileName);
                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError);
            }

            //Debug.WriteLine("... done parsing Gerber file.");
            UpdateKnockoutMeasurements();
            GerberCalculateFinalJustifyEffects(gerberImage);
            return gerberImage;
        }

        private static bool ParseGerberSegment(GerberImage gerberImage, GerberState gerberState, GerberNet currentNet)
        {
            char nextCharacter;
            int length;
            int coordinate;
            int regionPoints = 0;
            double scaleX = 0.0, scaleY = 0.0;
            double centreX = 0.0, centreY = 0.0;
            SizeD apertureSize = SizeD.Empty();
            double scale;

            lineReader = lineReaderList[levelOfRecursion];
            
            BoundingBox boundingBox = new BoundingBox();
            Aperture[] apertures = gerberImage.ApertureArray();

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
                        ParseGCode(gerberState, gerberImage);
                        break;

                    case 'D':
                        //Debug.WriteLine("Found D code in Line: {0}", lineReader.LineNumber);
                        ParseDCode(gerberState, gerberImage);
                        break;

                    case 'M':
                        //Debug.WriteLine("Found M code in Line: {0}", lineReader.LineNumber);
                        switch (ParseMCode(gerberImage))
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
                            AddTrailingZeros(gerberImage.Format.IntegralPartX, gerberImage.Format.DecimalPartX, length, ref coordinate);

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
                            AddTrailingZeros(gerberImage.Format.IntegralPartY, gerberImage.Format.DecimalPartY, length, ref coordinate);

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
                            AddTrailingZeros(gerberImage.Format.IntegralPartX, gerberImage.Format.DecimalPartX, length, ref coordinate);

                        gerberState.CenterX = coordinate;
                        gerberState.ChangedState = true;
                        break;

                    case 'J':
                        //Debug.WriteLine("Found J code in Line: {0}", lineReader.LineNumber);
                        gerberStats.JCount++;
                        coordinate = lineReader.GetIntegerValue(ref length);
                        if (gerberImage.Format != null && gerberImage.Format.OmitZeros == GerberOmitZero.OmitZerosTrailing)
                            AddTrailingZeros(gerberImage.Format.IntegralPartY, gerberImage.Format.DecimalPartY, length, ref coordinate);

                        gerberState.CenterY = coordinate;
                        gerberState.ChangedState = true;
                        break;

                    case '%':
                        //Debug.WriteLine("Found % code in Line: {0}", lineReader.LineNumber);
                        while (true)
                        {
                            ParseRS274X(gerberImage, gerberState, currentNet);
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
                        {
                            break;
                        }

                        gerberState.ChangedState = false;
                        // Don't even bother saving the geberNet if the aperture state is GERBER_APERTURE_STATE_OFF and we
                        // aren't starting a polygon fill (where we need it to get to the start point) 
                        if ((gerberState.ApertureState == GerberApertureState.Off)
                            && (!gerberState.IsRegionFill)
                            && (gerberState.Interpolation != GerberInterpolation.RegionStart))
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
                        currentNet.EndX = gerberState.CurrentX / scaleX;
                        currentNet.EndY = gerberState.CurrentY / scaleY;
                        centreX = gerberState.CenterX / scaleX;
                        centreY = gerberState.CenterY / scaleY;

                        if (!gerberState.IsRegionFill)
                            boundingBox = new BoundingBox();

                        switch (gerberState.Interpolation)
                        {
                            case GerberInterpolation.ClockwiseCircular:
                            case GerberInterpolation.CounterclockwiseCircular:
                                bool cw = gerberState.Interpolation == GerberInterpolation.ClockwiseCircular;
                                currentNet.CircleSegment = new CircleSegment();
                                if (gerberState.MultiQuadrant)
                                    CalculateCircleSegmentMQ(currentNet, cw, centreX, centreY);

                                else
                                {
                                    CalculateCircleSegmentSQ(currentNet, cw, centreX, centreY);
                                }
                                break;

                            case GerberInterpolation.RegionStart:
                                gerberState.ApertureState = GerberApertureState.On;     // Aperure state set to on for polygon areas.
                                gerberState.RegionStartNode = currentNet;               // To be able to get back and fill in number of polygon corners.
                                gerberState.IsRegionFill = true;
                                regionPoints = 0;
                                break;

                            case GerberInterpolation.RegionEnd:
                                // Save the calculated bounding box to the start node.
                                gerberState.RegionStartNode.BoundingBox = boundingBox;
                                gerberState.RegionStartNode = null;
                                gerberState.IsRegionFill = false;
                                regionPoints = 0;
                                boundingBox = new BoundingBox();
                                break;

                            default:
                                break;
                        }

                        // Count number of points in region. 
                        if (gerberState.IsRegionFill && gerberState.RegionStartNode != null)
                        {
                            // "...all lines drawn with D01 are considered edges of the
                            // polygon. D02 closes and fills the polygon."
                            // p.49 rs274xrevd_e.pdf
                            // D02 . state.apertureState == GERBER_APERTURE_STATE_OFF

                            // UPDATE: only end the region during a D02 call if we've already
                            // drawn a polygon edge (with D01)

                            if (gerberState.ApertureState == GerberApertureState.Off
                                && gerberState.Interpolation != GerberInterpolation.RegionStart
                                && (regionPoints > 0))
                            {
                                currentNet.Interpolation = GerberInterpolation.RegionEnd;
                                currentNet = new GerberNet(gerberImage, currentNet, gerberState.Level, gerberState.NetState);
                                currentNet.Interpolation = GerberInterpolation.RegionStart;
                                gerberState.RegionStartNode.BoundingBox = boundingBox;
                                gerberState.RegionStartNode = currentNet;
                                regionPoints = 0;

                                currentNet = new GerberNet(gerberImage, currentNet, gerberState.Level, gerberState.NetState);
                                currentNet.StartX = gerberState.PreviousX / scaleX;
                                currentNet.StartY = gerberState.PreviousY / scaleY;
                                currentNet.EndX = gerberState.CurrentX / scaleX;
                                currentNet.EndY = gerberState.CurrentY / scaleY;
                            }

                            else if (gerberState.Interpolation != GerberInterpolation.RegionStart)
                            {
                                regionPoints++;
                            }

                        }

                        currentNet.Interpolation = gerberState.Interpolation;
                        // Override circular interpolation if no center was given.
                        // This should be a safe hack, since a good file should always 
                        // include I or J. And even if the radius is zero, the end point 
                        // should be the same as the start point, creating no line 

                        if (((gerberState.Interpolation == GerberInterpolation.ClockwiseCircular)
                            || (gerberState.Interpolation == GerberInterpolation.CounterclockwiseCircular))
                            && (gerberState.CenterX == 0.0)
                            && (gerberState.CenterY == 0.0))
                        {
                            currentNet.Interpolation = GerberInterpolation.Linear;
                        }

                        // If we detected the end of a region we go back to
                        // the interpolation we had before that.
                        // Also if we detected any of the quadrant flags, since some
                        // gerbers don't reset the interpolation (EagleCad again).
                        if ((gerberState.Interpolation == GerberInterpolation.RegionStart)
                            || (gerberState.Interpolation == GerberInterpolation.RegionEnd))
                        {
                            gerberState.Interpolation = gerberState.PreviousInterpolation;
                        }

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
                        if ((currentNet.Aperture == 0) && !gerberState.IsRegionFill)
                        {
                            break;
                        }

                        // Only update the min/max values and aperture stats if we are drawing.
                        apertureMatrix = new Matrix(1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f);    // Matrix for rotation, offsets etc.
                        if ((currentNet.ApertureState != GerberApertureState.Off)
                            && (currentNet.Interpolation != GerberInterpolation.RegionStart))
                        {
                            double repeatOffsetX = 0.0;
                            double repeatOffsetY = 0.0;

                            // Update stats with current aperture number if not in polygon
                            if (!gerberState.IsRegionFill)
                            {
                                //Debug.WriteLine("    Found D code: adding 1 to D list.");
                                if (!gerberStats.IncrementDListCount(currentNet.Aperture, 1, lineReader.LineNumber))
                                {
                                    currentNet.ApertureState = GerberApertureState.Off; // Undefined aperture, turn it off.
                                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Found use of undefined D code D{0}.", currentNet.Aperture);
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
                            if ((apertures[currentNet.Aperture] != null)
                                && (apertures[currentNet.Aperture].ApertureType == GerberApertureType.Macro))
                            {
                                PointF[] points = null;
                                
                                foreach (SimplifiedApertureMacro macro in apertures[currentNet.Aperture].SimplifiedMacroList)
                                {
                                    points = GetAperturePoints(macro, currentNet);
                                    UpdateNetBounds(boundingBox, points);
                                }
                            }

                            else
                            {
                                if (apertures[currentNet.Aperture] != null)
                                {
                                    apertureSize.Width = apertures[currentNet.Aperture].Parameters()[0];
                                    if (apertures[currentNet.Aperture].ApertureType == GerberApertureType.Rectangle
                                        || apertures[currentNet.Aperture].ApertureType == GerberApertureType.Oval)
                                    {
                                        apertureSize.Height = apertures[currentNet.Aperture].Parameters()[1];
                                    }

                                    else
                                    {
                                        apertureSize.Height = apertureSize.Width;
                                    }
                                }

                                else
                                {
                                    // This is usually for region fills, where the aperture width is "zero"
                                    apertureSize.Width = apertureSize.Height = 0;
                                }

                                // If it's an arc path, use a special calculation. 
                                if ((currentNet.Interpolation == GerberInterpolation.ClockwiseCircular)
                                    || (currentNet.Interpolation == GerberInterpolation.CounterclockwiseCircular))
                                {
                                    int steps = Convert.ToInt16(Math.Abs(currentNet.CircleSegment.SweepAngle));
                                    CircleSegment circleSegment = currentNet.CircleSegment;
                                    double width = apertureSize.Width / 2;
                                    double height = apertureSize.Height / 2;
                                    PointF[] points = new PointF[steps * 2];
                                    int idx = 0;

                                    for (int i = 0; i < steps; i++, idx += 2)
                                    {
                                        double angle = circleSegment.StartAngle + circleSegment.SweepAngle * i / steps;
                                        double tempX = circleSegment.CenterX + circleSegment.Width / 2.0 * Math.Cos(DegreesToRadians(angle));
                                        double tempY = circleSegment.CenterY + circleSegment.Width / 2.0 * Math.Sin(DegreesToRadians(angle));
                                        points[idx] = new PointF((float)(tempX - width), (float)(tempY - height));
                                        points[idx + 1] = new PointF((float)(tempX + width), (float)(tempY + height));
                                    }

                                    UpdateNetBounds(boundingBox, points);
                                }

                                else
                                {
                                    // Check both the start and stop of the aperture points against a running min/max counter 
                                    // Note: only check start coordinate if this isn't a flash, 
                                    // since the start point may be invalid if it is a flash. 
                                    if (currentNet.ApertureState != GerberApertureState.Flash)
                                    {
                                        // Start points.
                                        UpdateNetBounds(boundingBox, currentNet.StartX, currentNet.StartY, apertureSize.Width / 2, apertureSize.Height / 2);
                                    }

                                    // Stop points.
                                    UpdateNetBounds(boundingBox, currentNet.EndX, currentNet.EndY, apertureSize.Width / 2, apertureSize.Height / 2);
                                }
                            }

                            // Update the info bounding box with this latest bounding box 
                            // don't change the bounding box if the polarity is clear 
                            if (gerberState.Level.Polarity != GerberPolarity.Clear)
                            {
                                UpdateImageBounds(boundingBox, repeatOffsetX, repeatOffsetY, gerberImage);
                            }

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
                            if (!gerberState.IsRegionFill)
                            {
                                currentNet.BoundingBox = boundingBox;
                            }
                        }

                        if (apertureMatrix != null)
                        {
                            apertureMatrix.Dispose();
                        }
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
        private static void ParseGCode(GerberState gerberState, GerberImage gerberImage)
        {
            int intValue;
            GerberNetState geberNetState;
            int length = 0;

            intValue = lineReader.GetIntegerValue(ref length);
            if (lineReader.EndOfFile)
            {
                errorMessage = String.Format(CultureInfo.CurrentCulture, "Unexpected EOF found processing file {0}.", lineReader.FileName);
                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError);
            }

            //Debug.WriteLine("    ParseGCcode: found G{0:d2} ", intValue);
            switch (intValue)
            {
                case 0:  // Move ... Is this doing anything really? - Deprecated.
                    gerberStats.G0++;
                    break;

                case 1:  // Linear Interpolation (1X scale)
                    gerberState.Interpolation = GerberInterpolation.Linear;
                    gerberStats.G1++;
                    break;

                case 2:  // Clockwise Linear Interpolation
                    gerberState.Interpolation = GerberInterpolation.ClockwiseCircular;
                    gerberStats.G2++;
                    break;

                case 3:  // Counter Clockwise Linear Interpolation.
                    gerberState.Interpolation = GerberInterpolation.CounterclockwiseCircular;
                    gerberStats.G3++;
                    break;

                case 4:  // Ignore comment blocks.
                    gerberStats.G4++;
                    lineReader.Position = lineReader.LineLength;

                    break;

                case 36: // Turn on Polygon Area Fill
                    gerberState.PreviousInterpolation = gerberState.Interpolation;
                    gerberState.Interpolation = GerberInterpolation.RegionStart;
                    gerberState.ChangedState = true;
                    gerberStats.G36++;
                    break;

                case 37: // Turn off Polygon Area Fill 
                    gerberState.Interpolation = GerberInterpolation.RegionEnd;
                    gerberState.ChangedState = true;
                    gerberStats.G37++;
                    break;

                case 54: // Select aperture - Deprecated.
                    if (lineReader.Read() == 'D')
                    {
                        int apartureNumber = lineReader.GetIntegerValue(ref length);

                        if ((apartureNumber >= 10) && (apartureNumber <= MaximumApertures))
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

                case 55: // Prepare for flash - Deprecated.
                    gerberStats.G55++;
                    break;

                case 70: // Specify inches - Deprecated.
                    geberNetState = new GerberNetState(gerberImage);
                    geberNetState.Unit = GerberUnit.Inch;
                    gerberState.NetState = geberNetState;
                    gerberStats.G70++;
                    break;

                case 71: // Specify millimeters  - Deprecated.
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

                case 90: // Specify absolute format - Deprecated.
                    if (gerberImage.Format != null)
                        gerberImage.Format.Coordinate = GerberCoordinate.Absolute;

                    gerberStats.G90++;
                    break;

                case 91: // Specify incremental format - Deprecated.
                    if (gerberImage.Format != null)
                        gerberImage.Format.Coordinate = GerberCoordinate.Incremental;

                    gerberStats.G91++;
                    break;

                default:
                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Found unknown G code G{0}.", intValue);
                    gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Ignoring unknown G code.");
                    gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning);
                    gerberStats.UnknowGCodes++;
                    break;
            }
            return;
        }

        // Process D codes.
        private static void ParseDCode(GerberState gerberState, GerberImage gerberImage)
        {
            int intValue;
            int length = 0;

            intValue = lineReader.GetIntegerValue(ref length);
            if (lineReader.EndOfFile)
            {
                errorMessage = String.Format(CultureInfo.CurrentCulture, "Unexpected EOF found processing file {0}.", lineReader.FileName);
                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError);
            }

            //Debug.WriteLine("    ParseDCcode: found D{0:d2} ", intValue);
            switch (intValue)
            {
                case 0: // Invalid code.
                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Found invalid D code {0}.", intValue);
                    gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                    gerberStats.DCodeErrors++;
                    break;

                case 1: // Exposure on.
                    gerberState.ApertureState = GerberApertureState.On;
                    gerberState.ChangedState = true;
                    gerberStats.D1++;
                    break;

                case 2: // Exposure off.
                    gerberState.ApertureState = GerberApertureState.Off;
                    gerberState.ChangedState = true;
                    gerberStats.D2++;
                    break;

                case 3: // Flash aperture.
                    gerberState.ApertureState = GerberApertureState.Flash;
                    gerberState.ChangedState = true;
                    gerberStats.D3++;
                    break;

                default: // Aperture id in use.
                    if ((intValue >= 10) && (intValue <= MaximumApertures))
                        gerberState.CurrentAperture = intValue;

                    else
                    {
                        errorMessage = String.Format(CultureInfo.CurrentCulture, "Found reference out of bounds in aperture D{0}.", intValue);
                        gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                        gerberStats.DCodeErrors++;
                    }

                    gerberState.ChangedState = false;
                    break;
            }

            return;
        }

        // Parse M codes.
        private static int ParseMCode(GerberImage gerberImage)
        {
            int intValue;
            int length = 0;
            int rtnValue = 0;
            //GerberFileStats gerberStats = gerberImage.GerberStats;

            intValue = lineReader.GetIntegerValue(ref length);
            if (lineReader.EndOfFile)
            {
                errorMessage = String.Format(CultureInfo.CurrentCulture, "Unexpected EOF found processing file {0}.", lineReader.FileName);
                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError);
            }

            //Debug.WriteLine("    ParseMCcode: found M{0:d2} ", intValue);
            switch (intValue)
            {
                case 0:  // Program stop - Deprecated.
                    gerberStats.M0++;
                    rtnValue = 1;
                    break;

                case 1:  // Optional stop - Deprecated.
                    gerberStats.M1++;
                    rtnValue = 2;
                    break;

                case 2:  // End of program.
                    gerberStats.M2++;
                    rtnValue = 3;
                    break;

                default:
                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Found unknown M code M{0}.", intValue);
                    gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Ignoring unknown M code.");
                    gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError);
                    gerberStats.UnknownMCodes++;
                    break;
            }

            return rtnValue;
        }

        private static void ParseRS274X(GerberImage gerberImage, GerberState gerberState, GerberNet currentNet)
        {
            int intValue;
            int apertureNumber;
            string command;
            char nextCharacter;
            int length = 0;
            float scale = 1.0f;
            ApertureMacro apertureMacro;

            if (gerberState.NetState.Unit == GerberUnit.Millimeter)
                scale = 25.4f;

            command = lineReader.ReadLine(2);

            if (lineReader.EndOfFile)
            {
                errorMessage = string.Format(CultureInfo.CurrentCulture, "Unexpected EOF in file {0}.", lineReader.FileName);
                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError);
            }

            switch (command)
            {
                // Directive parameters. 
                case "AS": // Axis Select   ** Deprecated but included for legacy files **
                    gerberState.NetState = new GerberNetState(gerberImage);
                    command = lineReader.ReadLine(2);
                    if (command == "AY" || command == "BX")
                        gerberState.NetState.AxisSelect = GerberAxisSelect.SwapAB;

                    else
                        gerberState.NetState.AxisSelect = GerberAxisSelect.None;

                    command = lineReader.ReadLine(2);
                    if (command == "AY" || command == "BX")
                        gerberState.NetState.AxisSelect = GerberAxisSelect.SwapAB;

                    else
                        gerberState.NetState.AxisSelect = GerberAxisSelect.None;

                    break;

                case "FS": // Format Statement.
                    if (gerberImage.Format == null)
                        gerberImage.Format = new GerberFormat();

                    nextCharacter = lineReader.Read();
                    switch (nextCharacter)
                    {
                        case 'L':
                            gerberImage.Format.OmitZeros = GerberOmitZero.OmitZerosLeading;
                            break;

                        case 'T':   // ** Depreciated but included for legacy files **
                            gerberImage.Format.OmitZeros = GerberOmitZero.OmitZerosTrailing;
                            break;

                        case 'D':   // ** Depreciated but included for legacy files **
                            gerberImage.Format.OmitZeros = GerberOmitZero.OmitZerosExplicit;
                            break;

                        default:
                            errorMessage = "Undefined handling of zeros in format code.";
                            gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                            errorMessage = "Defaulting to omit leading zeros.";
                            gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning);
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

                        case 'I':   // ** Depreciated but included for legacy files **
                            gerberImage.Format.Coordinate = GerberCoordinate.Incremental;
                            break;

                        default:
                            errorMessage = "Invalid coordinate type defined in format code.";
                            gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                            errorMessage = "Defaulting to absolute coordinates.";
                            gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.FileName, lineReader.LineNumber);
                            gerberImage.Format.Coordinate = GerberCoordinate.Absolute;
                            break;
                    }

                    nextCharacter = lineReader.Read();
                    while (nextCharacter != '*')
                    {
                        // Note: Nn, Gn, Dn, Mn parameters ** Depreciated but included for legacy files **
                        switch (nextCharacter)
                        {
                            case 'N':
                                nextCharacter = lineReader.Read();
                                gerberImage.Format.SequenceNumberLimit = nextCharacter - '0';
                                break;

                            case 'G':
                                nextCharacter = lineReader.Read();
                                gerberImage.Format.GeneralFunctionLimit = nextCharacter - '0';
                                break;

                            case 'D':
                                nextCharacter = lineReader.Read();
                                gerberImage.Format.PlotFunctionLimit = nextCharacter - '0';
                                break;

                            case 'M':
                                nextCharacter = lineReader.Read();
                                gerberImage.Format.MiscFunctionLimit = (nextCharacter & 0xff) - '0';
                                break;

                            case 'X':
                                nextCharacter = lineReader.Read();
                                if ((nextCharacter < '0') || (nextCharacter > '6'))
                                {
                                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Illegal format size {0}.", nextCharacter);
                                    gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                }

                                gerberImage.Format.IntegralPartX = nextCharacter - '0';
                                nextCharacter = lineReader.Read();
                                if ((nextCharacter < '0') || (nextCharacter > '6'))
                                {
                                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Illegal format size {0}.", nextCharacter);
                                    gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                }

                                gerberImage.Format.DecimalPartX = nextCharacter - '0';
                                break;

                            case 'Y':
                                nextCharacter = lineReader.Read();
                                if ((nextCharacter < '0') || (nextCharacter > '6'))
                                {
                                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Illegal format size {0}.", nextCharacter);
                                    gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                }

                                gerberImage.Format.IntegralPartY = nextCharacter - '0';
                                nextCharacter = lineReader.Read();
                                if ((nextCharacter < '0') || (nextCharacter > '6'))
                                {
                                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Illegal format size {0}.", nextCharacter);
                                    gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                }

                                gerberImage.Format.DecimalPartY = nextCharacter - '0';
                                break;

                            default:
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Illegal format size {0}.", nextCharacter);
                                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Ignoring invalid format statement.");
                                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning);
                                break;
                        }

                        nextCharacter = lineReader.Read();
                    }

                    break;

                case "MI": // Mirror Image  ** Deprecated, but included for legacy files **
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
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Invalid character in mirror:{0}.", nextCharacter);
                                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                break;
                        }

                        nextCharacter = lineReader.Read();
                    }
                    break;

                case "MO": // Mode of Units.
                    command = lineReader.ReadLine(2);
                    switch (command)
                    {
                        case "IN":
                            //gerberImage.Unit = GerberUnit.Inch;
                            gerberState.NetState = new GerberNetState(gerberImage);
                            gerberState.NetState.Unit = GerberUnit.Inch;
                            break;

                        case "MM":
                            //gerberImage.Unit = GerberUnit.Millimeter;
                            gerberState.NetState = new GerberNetState(gerberImage);
                            gerberState.NetState.Unit = GerberUnit.Millimeter;
                            break;

                        default:
                            errorMessage = String.Format(CultureInfo.CurrentCulture, "Illegal unit of measure:{0}.", command);
                            gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                            break;
                    }
                    break;

                case "OF": // Offset  ** Deprecated, but included for legacy files **
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
                                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
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
                        string includeFileName = lineReader.ReadLine('*');
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
                                    ParseGerberSegment(gerberImage, gerberState, currentNet); // Parse the include file, and remove it from the the list.
                                    lineReaderList.RemoveAt(levelOfRecursion);
                                    levelOfRecursion--;
                                }

                            }

                            else
                            {
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Include file {0} not found.", includeFileName);
                                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberCritical, lineReader.FileName, lineReader.LineNumber);
                            }
                        }

                        else
                        {
                            errorMessage = "More than 10 levels of include file recursion.";
                            gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberCritical, lineReader.FileName, lineReader.LineNumber);
                        }
                    }

                    break;

                case "SF": // Scale Factor  ** Deprecated, but included for legacy files **
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
                                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
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
                                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                break;
                        }

                        nextCharacter = lineReader.Read();
                    }
                    break;

                case "IN": // Image Name.   ** Deprecated, but included for legacy files **
                    gerberImage.ImageInfo.ImageName = lineReader.ReadLine('*');
                    break;

                case "IP": // Image Polarity.   ** Deprecated, but included for legacy files **
                    command = lineReader.ReadLine(3);
                    if (command == "POS")
                        gerberImage.ImageInfo.Polarity = GerberPolarity.Positive;

                    else if (command == "NEG")
                        gerberImage.ImageInfo.Polarity = GerberPolarity.Negative;

                    else
                    {
                        errorMessage = String.Format(CultureInfo.CurrentCulture, "Unknown polarity:{0}.", command);
                        gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                    }
                    break;

                case "IR": // Image Rotation ** Depreciated, but included for legacy files.
                    imageRotation = lineReader.GetIntegerValue(ref length) % 360;
                    if (imageRotation == 0 || imageRotation == 90 || imageRotation == 180 || imageRotation == 270)
                    {
                        gerberImage.ImageInfo.ImageRotation = imageRotation;
                    }

                    else
                    {
                        errorMessage = String.Format(CultureInfo.CurrentCulture,
                            "Image rotation must be 0, 90, 180 or 270 [{0}° is invalid].", imageRotation);

                        gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                        imageRotation = 0.0;
                    }
                    break;

                case "PF": // Plotter Film ** Deprecated, but included for legacy files **
                    gerberImage.ImageInfo.PlotterFilm = lineReader.ReadLine('*');
                    break;

                // Aperture parameters
                case "AD": // Aperture Definition.
                    Aperture aperture = new Aperture();
                    apertureNumber = ParseApertureDefinition(aperture, gerberImage, scale);
                    if (apertureNumber != -1)
                    {
                        if ((apertureNumber >= 0) && (apertureNumber <= MaximumApertures))
                        {
                            aperture.Unit = gerberState.NetState.Unit;
                            gerberImage.ApertureArray()[apertureNumber] = aperture;
                            //Debug.WriteLine("In parseRS274X: adding new aperture.");
                            gerberStats.AddNewAperture(-1, apertureNumber, aperture.ApertureType, aperture.Parameters());
                            gerberStats.AddNewDList(apertureNumber);
                            if (apertureNumber < MinimumAperture)
                            {
                                errorMessage = String.Format(CultureInfo.CurrentCulture, "Aperture number {0} out of lower bounds.", apertureNumber);
                                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                            }
                        }

                        else
                        {
                            errorMessage = String.Format(CultureInfo.CurrentCulture, "Aperture number {0} out of upper bounds.", apertureNumber);
                            gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                        }
                    }
                    break;

                case "AM": // Aperture Macro.
                    //Debug.WriteLine(String.Format("Found {0} command in Line: {1}", stringValue, lineReader.LineNumber));
                    apertureMacro = ApertureMacro.ParseApertureMacro(lineReader);
                    if (apertureMacro != null)
                        gerberImage.ApertureMacroList.Add(apertureMacro);

                    else
                    {
                        errorMessage = "Failed to read aperture macro.";
                        gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                    }

                    return; // Return, since we want to skip the later back-up loop.

                // Level Commands
                case "LN": // Level Name  ** Deprecated, but included for legacy files **
                    gerberState.Level = new GerberLevel(gerberImage);
                    gerberState.Level.LevelName = lineReader.ReadLine('*');
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
                            gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
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
                        gerberState.Level.Knockout.KnockoutType = GerberKnockoutType.NoKnockout;
                        break;
                    }

                    else if (nextCharacter == 'C')
                        gerberState.Level.Knockout.Polarity = GerberPolarity.Clear;

                    else if (nextCharacter == 'D')
                        gerberState.Level.Knockout.Polarity = GerberPolarity.Dark;

                    else
                    {
                        errorMessage = "Knockout must supply a polarity (C, D, or *).";
                        gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
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
                                gerberState.Level.Knockout.KnockoutType = GerberKnockoutType.FixedKnockout;
                                gerberState.Level.Knockout.LowerLeftX = lineReader.GetDoubleValue() / scale;
                                break;

                            case 'Y':
                                gerberState.Level.Knockout.KnockoutType = GerberKnockoutType.FixedKnockout;
                                gerberState.Level.Knockout.LowerLeftY = lineReader.GetDoubleValue() / scale;
                                break;

                            case 'I':
                                gerberState.Level.Knockout.KnockoutType = GerberKnockoutType.FixedKnockout;
                                gerberState.Level.Knockout.Width = lineReader.GetDoubleValue() / scale;
                                break;

                            case 'J':
                                gerberState.Level.Knockout.KnockoutType = GerberKnockoutType.FixedKnockout;
                                gerberState.Level.Knockout.Height = lineReader.GetDoubleValue() / scale;
                                break;

                            case 'K':
                                gerberState.Level.Knockout.KnockoutType = GerberKnockoutType.Border;
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
                                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
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
                                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                                break;
                        }

                        nextCharacter = lineReader.Read();
                    }
                    break;

                default:
                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Unknown or unsupported command '{0}'.", command);
                    gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.FileName, lineReader.LineNumber);
                    break;
            }

            // Make sure we read until the trailing '*' character
            // First, backspace once in case we already read it.
            lineReader.Position--;
            nextCharacter = lineReader.Read();
            while ((!lineReader.EndOfFile) && (nextCharacter != '*'))
                nextCharacter = lineReader.Read();

            return;
        }

        private static int ParseApertureDefinition(Aperture aperture, GerberImage gerberImage, float scale)
        {
            int apertureNumber;
            int tokenCount;
            string[] tokens;
            string stringValue;
            int parameterIndex = 0;
            int tokenIndex = 0;
            int length = 0;


            if (lineReader.Read() != 'D')
            {
                errorMessage = String.Format(CultureInfo.CurrentCulture, "Found AD code without a following 'D' code.");
                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                return -1;
            }

            // Get aperture number.
            apertureNumber = lineReader.GetIntegerValue(ref length);

            // Read in the whole aperture defintion and tokenize it.
            stringValue = lineReader.ReadLine('*');
            tokens = stringValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens == null || tokens.Length == 0)
            {
                errorMessage = String.Format(CultureInfo.CurrentCulture, "Invalid aperture definition.");
                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
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
                foreach (ApertureMacro macro in gerberImage.ApertureMacroList)
                {
                    if (macro.Name.Length == tokens[0].Length && macro.Name == tokens[0])
                    {
                        aperture.ApertureMacro = macro;
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
                    errorMessage = String.Format(CultureInfo.CurrentCulture, "Maximum allowed parameters exceeded in aperture {0}.", apertureNumber);
                    gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                    break;
                }

                tokens = tokens[tokenIndex].Split(new char[] { 'X', 'x' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string token in tokens)
                {
                    if (!double.TryParse(token, out double parameterValue))
                    {
                        errorMessage = String.Format(CultureInfo.CurrentCulture, "Failed to read all parameters in aperture {0}.", apertureNumber);
                        gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
                        aperture.Parameters()[parameterIndex] = 0.0;
                    }

                    else
                    {
                        // Scale any MM values to inches.
                        // Don't scale polygon angles or side numbers, or macro parameters.
                        if (!(((aperture.ApertureType == GerberApertureType.Polygon) &&
                            ((parameterIndex == 1) || (parameterIndex == 2))) ||
                            (aperture.ApertureType == GerberApertureType.Macro)))

                            parameterValue /= scale;

                        aperture.Parameters()[parameterIndex] = parameterValue;
                    }

                    parameterIndex++;
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
        }

        /// <summary>
        /// Calculates the start, and sweep angles of a multi-quadrant arc.
        /// </summary>
        /// <param name="currentNet">location to save the segment data</param>
        /// <param name="clockwise">true if clockwise arc</param>
        /// <param name="centreX">center X</param>
        /// <param name="centreY">center Y</param>
        private static void CalculateCircleSegmentMQ(GerberNet currentNet, bool clockwise, double centreX, double centreY)
        {
            double d1x, d1y, d2x, d2y;
            double alpha, beta;

            currentNet.CircleSegment.CenterX = currentNet.StartX + centreX;
            currentNet.CircleSegment.CenterY = currentNet.StartY + centreY;

            d1x = -centreX;
            d1y = -centreY;
            d2x = currentNet.EndX - currentNet.CircleSegment.CenterX;
            d2y = currentNet.EndY - currentNet.CircleSegment.CenterY;

            if (Math.Abs(d1x) < Double.Epsilon) d1x = 0;
            if (Math.Abs(d1y) < Double.Epsilon) d1y = 0;
            if (Math.Abs(d2x) < Double.Epsilon) d2x = 0;
            if (Math.Abs(d2y) < Double.Epsilon) d2y = 0;

            alpha = Math.Atan2(d1y, d1x);
            beta = Math.Atan2(d2y, d2x);

            currentNet.CircleSegment.Width = Math.Sqrt((centreX * centreX) + (centreY * centreY));
            currentNet.CircleSegment.Width *= 2.0;
            currentNet.CircleSegment.Height = currentNet.CircleSegment.Width;

            // Make sure it's always positive angles.
            if (alpha < 0.0)
            {
                alpha += Math.PI * 2;
                beta += Math.PI * 2;
            }

            if (beta < 0.0)
                beta += Math.PI * 2;

            if (clockwise)
            {
                if (alpha - beta < Double.Epsilon)
                    beta -= Math.PI * 2;
            }

            else
            {
                if (beta - alpha < Double.Epsilon)
                    beta += Math.PI * 2;
            }

            currentNet.CircleSegment.StartAngle = RadiansToDegrees(alpha);
            currentNet.CircleSegment.EndAngle = RadiansToDegrees(beta);
        }

        private static void CalculateCircleSegmentSQ(GerberNet gerberNet, bool clockWise, double centreX, double centreY)
        {
            // Using single values to limit rounding errors.
            PointD center = PointD.Empty;
            float d1x, d1y, d2x, d2y;
            float deviation, bestDeviation = float.MaxValue;
            float startRadius, endRadius;
            float alpha, beta;
            bool validArc = false;

            const float PI = 3.14159274f;
            const float PI_2 = PI * 2;
            const float Allowable_Deviation = 0.0005f;    // Maximum allowable deviation for a valid arc segement.

            // Get all possible centers.
            PointD[] centerPoints = new PointD[] { new PointD(gerberNet.StartX + centreX, gerberNet.StartY + centreY),
                                                   new PointD(gerberNet.StartX - centreX, gerberNet.StartY + centreY),
                                                   new PointD(gerberNet.StartX + centreX, gerberNet.StartY - centreY),
                                                   new PointD(gerberNet.StartX - centreX, gerberNet.StartY - centreY) };

            // Find the correct center by stepping through the possiblities.
            // Use the center that:
            // Produce an arc angle <= to 90°.
            // Has the correct rotation.
            // Has the smallest arc deviation. (A valid arc, in theory should produce an end radius equal to the start radius)
            for (int idx = 0; idx < centerPoints.Length; idx++)
            {
                center.X = Math.Round(centerPoints[idx].X, 6);
                center.Y = Math.Round(centerPoints[idx].Y, 6);

                d1x = (float)(center.X - gerberNet.StartX);
                d1y = (float)(center.Y - gerberNet.StartY);
                startRadius = (float)Math.Sqrt((d1x * d1x) + (d1y * d1y));

                d2x = (float)(center.X - gerberNet.EndX);
                d2y = (float)(center.Y - gerberNet.EndY);
                endRadius = (float)Math.Sqrt((d2x * d2x) + (d2y * d2y));

                deviation = Math.Abs(startRadius - endRadius);
                if (deviation > bestDeviation)
                    continue;

                // Calculate the start and end angles.
                alpha = (float)Math.Atan2(d1y, d1x);    // Start angle.
                if (d1x < 0)
                {
                    alpha -= PI;
                    if (alpha < 0)
                        alpha += PI_2;
                }

                else
                    alpha += PI;

                beta = (float)Math.Atan2(d2y, d2x);     // End angle.
                if (d2x < 0)
                {
                    beta -= PI;
                    if (beta <= 0)
                        beta += PI_2;
                }

                else
                    beta += PI;

                // Check arc direction.
                if (clockWise)
                {
                    if (alpha == 0)
                        alpha = PI_2;

                    if (beta == PI_2)
                        beta = 0;

                    // beta > 270° and alpha < 90°.
                    if (beta > 4.71239 && alpha < 1.5708)
                        beta -= 6.28319f;   // Subtract 360°.
                }


                else  // Counter clockwise.
                {
                    // alpha > 270° and beta < 90°.
                    if (alpha > 4.71239 && beta < 1.5708)
                        alpha -= 6.28319f;  // Subtact 360°.
                }

                // Check for valid angle, =< 90°.
                if (Math.Abs(beta - alpha) > 1.5708)
                    continue;

                bestDeviation = deviation;
                gerberNet.CircleSegment.StartAngle = RadiansToDegrees(alpha);
                gerberNet.CircleSegment.EndAngle = RadiansToDegrees(beta);
                gerberNet.CircleSegment.CenterX = center.X;
                gerberNet.CircleSegment.CenterY = center.Y;

                gerberNet.CircleSegment.Width = alpha < beta
                ? 2 * startRadius
                : 2 * endRadius;

                gerberNet.CircleSegment.Height = alpha > beta
                    ? 2 * startRadius
                    : 2 * endRadius;

                if (bestDeviation < Allowable_Deviation)
                    validArc = true;
            }

            if (!validArc)
            {
                errorMessage = String.Format(CultureInfo.CurrentCulture, "Invalid arc definition, deviation =  {0}.", bestDeviation);
                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberError, lineReader.FileName, lineReader.LineNumber);
            }
        }

        private static void CalculateCircleSegmentSQ_1(GerberNet gerberNet, bool clockWise, double centreX, double centreY)
        {
            double d1x, d1y, d2x, d2y;
            double alpha, beta;
            int quadrant;
            double deviation;
            float PI = (float)Math.PI;
            float PI_2 = PI * 2;

            /*
            * Quadrant detection (based on ccw, converted below if cw)
            *  Y ^
            *    |
            *    |
            *    --->X
            */

            if (gerberNet.StartX > gerberNet.EndX)
            {
                // 1st and 2nd quadrant.
                if (gerberNet.StartY < gerberNet.EndY)
                    quadrant = 1;

                else
                    quadrant = 2;
            }

            else
            {
                // 3rd and 4th quadrant.
                if (gerberNet.StartY > gerberNet.EndY)
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
                }
            }

            // Calculate arc center point.
            switch (quadrant)
            {
                case 1:
                    gerberNet.CircleSegment.CenterX = gerberNet.StartX - centreX;
                    gerberNet.CircleSegment.CenterY = gerberNet.StartY - centreY;
                    break;

                case 2:
                    gerberNet.CircleSegment.CenterX = gerberNet.StartX + centreX;
                    gerberNet.CircleSegment.CenterY = gerberNet.StartY - centreY;
                    break;

                case 3:
                    gerberNet.CircleSegment.CenterX = gerberNet.StartX + centreX;
                    gerberNet.CircleSegment.CenterY = gerberNet.StartY + centreY;
                    break;

                case 4:
                    gerberNet.CircleSegment.CenterX = gerberNet.StartX - centreX;
                    gerberNet.CircleSegment.CenterY = gerberNet.StartY + centreY;
                    break;

                default:
                    break;
            }

            // Some good values. 
            d1x = Math.Abs(gerberNet.StartX - gerberNet.CircleSegment.CenterX);
            d1y = Math.Abs(gerberNet.StartY - gerberNet.CircleSegment.CenterY);
            d2x = Math.Abs(gerberNet.EndX - gerberNet.CircleSegment.CenterX);
            d2y = Math.Abs(gerberNet.EndY - gerberNet.CircleSegment.CenterY);

            alpha = Math.Atan2(d1y, d1x);
            beta = Math.Atan2(d2y, d2x);

            double width = Math.Sqrt((d1x * d1x) + (d1y * d1y));
            double height = Math.Sqrt((d2x * d2x) + (d2y * d2y));

            deviation = Math.Abs(width - height);
            if (deviation > .001)
            {
                errorMessage = String.Format(CultureInfo.CurrentCulture, "Invalid arc definition, deviation =  {0}.", deviation);
                gerberStats.AddNewError(-1, errorMessage, GerberErrorType.GerberWarning, lineReader.FileName, lineReader.LineNumber);
            }


            //Avoid divide by zero when sin(0) = 0 and cos(90) = 0
            gerberNet.CircleSegment.Width = alpha < beta
                ? 2 * (d1x / Math.Cos(alpha))
                : 2 * (d2x / Math.Cos(beta));

            gerberNet.CircleSegment.Height = alpha > beta
                ? 2 * (d1y / Math.Sin(alpha))
                : 2 * (d2y / Math.Sin(beta));

            if (alpha < 0.000001 && beta < 0.000001)
                gerberNet.CircleSegment.Height = 0;

            switch (quadrant)
            {
                case 1:
                    gerberNet.CircleSegment.StartAngle = RadiansToDegrees(alpha);
                    gerberNet.CircleSegment.EndAngle = RadiansToDegrees(beta);
                    break;

                case 2:
                    gerberNet.CircleSegment.StartAngle = 180.0 - RadiansToDegrees(alpha);
                    gerberNet.CircleSegment.EndAngle = 180.0 - RadiansToDegrees(beta);
                    break;

                case 3:
                    gerberNet.CircleSegment.StartAngle = 180.0 + RadiansToDegrees(alpha);
                    gerberNet.CircleSegment.EndAngle = 180.0 + RadiansToDegrees(beta);
                    break;

                case 4:
                    gerberNet.CircleSegment.StartAngle = 360.0 - RadiansToDegrees(alpha);
                    gerberNet.CircleSegment.EndAngle = 360.0 - RadiansToDegrees(beta);
                    break;

                default:
                    break;
            }
        }

        private static void CircleSegmentBound(CircleSegment circleSegment, double x, double y, BoundingBox boundingBox)
        {
            float centreX = (float)circleSegment.CenterX;
            float centreY = (float)circleSegment.CenterY;
            float width = (float)circleSegment.Width;
            float height = (float)circleSegment.Height;
            RectangleF r = new RectangleF(centreX - (width / 2) - (float)x, centreY - (height / 2) + (float)y, width, height);

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(r, (float)circleSegment.StartAngle, (float)circleSegment.SweepAngle);
                path.Flatten();

                PointF[] points = path.PathPoints;
                for (int i = 0; i < points.Length; i++)
                {
                    if (points[i].X < boundingBox.Left)
                        boundingBox.Left = points[i].X;

                    if (points[i].Y > boundingBox.Top)
                        boundingBox.Top = points[i].Y;

                    if (points[i].X > boundingBox.Right)
                        boundingBox.Right = points[i].X;

                    if (points[i].Y < boundingBox.Bottom)
                        boundingBox.Bottom = points[i].Y;
                }
            }
        }

        // Calculate circular interpolation bounding box.
        private static void CircleSegmentBounds(CircleSegment circleSegment, double apertureSizeX1, double apertureSizeY1, BoundingBox boundingBox)
        {
            double x, y, angle1, angle2, stepPi_2;
            double startAngle = circleSegment.StartAngle;
            double endAngle = circleSegment.EndAngle;
            const double MPI_2 = Math.PI / 2;

            // For bounding box calculation only half of aperture size is used.
            apertureSizeX1 /= 2;
            apertureSizeY1 /= 2;

            angle1 = DegreesToRadians(Math.Min(startAngle, endAngle));
            angle2 = DegreesToRadians(Math.Max(startAngle, endAngle));

            // Start arc point.
            x = circleSegment.CenterX + circleSegment.Width * Math.Cos(angle1) / 2;
            y = circleSegment.CenterY + circleSegment.Width * Math.Sin(angle1) / 2;
            UpdateNetBounds(boundingBox, x, y, apertureSizeX1, apertureSizeY1);

            // Middle arc points.
            for (stepPi_2 = (angle1 / MPI_2 + 1) * MPI_2; stepPi_2 < Math.Min(angle2, angle1 + (2 * Math.PI)); stepPi_2 += MPI_2)
            {
                x = circleSegment.CenterX + circleSegment.Width / 2 * Math.Cos(stepPi_2);
                y = circleSegment.CenterY + circleSegment.Width / 2 * Math.Sin(stepPi_2);

                UpdateNetBounds(boundingBox, x, y, apertureSizeX1, apertureSizeY1);
            }

            // Stop arc point.
            x = circleSegment.CenterX + circleSegment.Width * Math.Cos(angle2) / 2;
            y = circleSegment.CenterY + circleSegment.Width * Math.Sin(angle2) / 2;
            UpdateNetBounds(boundingBox, x, y, apertureSizeX1, apertureSizeY1);
        }

        private static PointF[] GetAperturePoints(SimplifiedApertureMacro macro, GerberNet currentNet)
        {
            PointF[] points = null;

            PointD offset = PointD.Empty;
            float centerX, centerY = 0.0f;
            float width, height = 0.0f;
            float radius = 0.0f;
            float rotation = 0.0f;


                switch (macro.ApertureType)
                {
                    case GerberApertureType.MacroCircle:
                        radius = (float)macro.Parameters[(int)CircleParameters.Diameter] / 2;
                        offset.X = macro.Parameters[(int)CircleParameters.CentreX];
                        offset.Y = macro.Parameters[(int)CircleParameters.CentreY];

                        centerX = (float)(currentNet.EndX + offset.X);
                        centerY = (float)(currentNet.EndY + offset.Y);
                        points = new PointF[] { new PointF(centerX - radius, centerY + radius),
                                            new PointF(centerX + radius, centerY + radius),
                                            new PointF(centerX - radius, centerY - radius),
                                            new PointF(centerX + radius, centerY - radius) };
                        break;

                    case GerberApertureType.MacroMoire:
                        float crossHairLength = (float)macro.Parameters[(int)MoireParameters.CrosshairLength];
                        float outsideDiameter = (float)macro.Parameters[(int)MoireParameters.OutsideDiameter];
                        offset.X = macro.Parameters[(int)MoireParameters.CentreX];
                        offset.Y = macro.Parameters[(int)MoireParameters.CentreY];

                        radius = Math.Max(crossHairLength, outsideDiameter) / 2;
                        centerX = (float)(currentNet.EndX + offset.X);
                        centerY = (float)(currentNet.EndY + offset.Y);
                        points = new PointF[] { new PointF(centerX - radius, centerY + radius),
                                                new PointF(centerX + radius, centerY + radius),
                                                new PointF(centerX - radius, centerY - radius),
                                                new PointF(centerX + radius, centerY - radius) };
                        break;

                    case GerberApertureType.MacroThermal:
                        radius = (float)macro.Parameters[(int)ThermalParameters.OutsideDiameter] / 2;
                        offset.X = macro.Parameters[(int)ThermalParameters.CentreX];
                        offset.Y = macro.Parameters[(int)ThermalParameters.CentreY];

                        centerX = (float)(currentNet.EndX + offset.X);
                        centerY = (float)(currentNet.EndY + offset.Y);
                        points = new PointF[] { new PointF(centerX - radius, centerY + radius),
                                                new PointF(centerX + radius, centerY + radius),
                                                new PointF(centerX - radius, centerY - radius),
                                                new PointF(centerX + radius, centerY - radius) };
                        break;

                    case GerberApertureType.MacroPolygon:
                        int numberOfSides = (int)macro.Parameters[(int)PolygonParameters.NumberOfSides];
                        float diameter = (float)macro.Parameters[(int)PolygonParameters.Diameter];
                        rotation = (float)macro.Parameters[(int)PolygonParameters.Rotation];
                        offset.X = (float)macro.Parameters[(int)PolygonParameters.CentreX];
                        offset.Y = (float)macro.Parameters[(int)PolygonParameters.CentreY];

                        apertureMatrix.RotateAt(rotation, new PointF((float)currentNet.EndX, (float)currentNet.EndY));
                        points = new PointF[numberOfSides];
                        points[0] = new PointF(diameter / 2.0f + (float)(offset.X + currentNet.EndX), (float)(offset.Y + currentNet.EndY));
                        for (int i = 1; i < numberOfSides; i++)
                        {
                            double angle = (double)i / numberOfSides * Math.PI * 2.0;
                            points[i] = new PointF((float)(Math.Cos(angle) * diameter / 2.0 + offset.X + currentNet.EndX),
                                                   (float)(Math.Sin(angle) * diameter / 2.0 + offset.Y + currentNet.EndY));
                        }

                        break;

                    case GerberApertureType.MacroOutline:
                        int numberOfPoints = (int)macro.Parameters[(int)OutlineParameters.NumberOfPoints];
                        rotation = (float)macro.Parameters[(numberOfPoints * 2) + (int)OutlineParameters.Rotation];

                        apertureMatrix.RotateAt(rotation, new PointF((float)currentNet.EndX, (float)currentNet.EndY));
                        points = new PointF[numberOfPoints + 1];
                        for (int p = 0; p <= numberOfPoints; p++)
                        {
                            points[p] = new PointF((float)(currentNet.EndX + macro.Parameters[(p * 2) + (int)OutlineParameters.FirstX]),
                                                   (float)(currentNet.EndY + macro.Parameters[(p * 2) + (int)OutlineParameters.FirstY]));
                        }

                        break;

                    case GerberApertureType.MacroLine20:
                        float startX = (float)macro.Parameters[(int)Line20Parameters.StartX];
                        float startY = (float)macro.Parameters[(int)Line20Parameters.StartY];
                        float endX = (float)macro.Parameters[(int)Line20Parameters.EndX];
                        float endY = (float)macro.Parameters[(int)Line20Parameters.EndY];
                        width = (float)macro.Parameters[(int)Line20Parameters.LineWidth] / 2;
                        rotation = (float)macro.Parameters[(int)Line20Parameters.Rotation];

                        apertureMatrix.RotateAt(rotation, new PointF((float)(currentNet.EndX + startX), (float)(currentNet.EndY + startY)));
                        points = new PointF[] { new PointF((float)currentNet.EndX + startX + width, (float)currentNet.EndY + startY - width),
                                                new PointF((float)currentNet.EndX + startX + width, (float)currentNet.EndY + startY + width),
                                                new PointF((float)currentNet.EndX + endX - width, (float)currentNet.EndY + endY - width),
                                                new PointF((float)currentNet.EndX + endX - width, (float)currentNet.EndY + endY + width) };
                        break;

                    case GerberApertureType.MacroLine21:
                        rotation = (float)macro.Parameters[(int)Line21Parameters.Rotation];
                        width = (float)macro.Parameters[(int)Line21Parameters.LineWidth] / 2;
                        height = (float)macro.Parameters[(int)Line21Parameters.LineHeight] / 2;
                        offset.X = (float)macro.Parameters[(int)Line21Parameters.CentreX];
                        offset.Y = (float)macro.Parameters[(int)Line21Parameters.CentreY];

                        apertureMatrix.RotateAt(rotation, new PointF((float)currentNet.EndX, (float)currentNet.EndY));
                        centerX = (float)(currentNet.EndX + offset.X);
                        centerY = (float)(currentNet.EndY + offset.Y);
                        points = new PointF[] { new PointF(centerX - width, centerY - height),
                                                new PointF(centerX + width, centerY - height),
                                                new PointF(centerX + width, centerY + height),
                                                new PointF(centerX - width, centerY + height) };
                        break;

                    case GerberApertureType.MacroLine22:
                        rotation = (float)macro.Parameters[(int)Line22Parameters.Rotation];
                        width = (float)macro.Parameters[(int)Line22Parameters.LineWidth];
                        height = (float)macro.Parameters[(int)Line22Parameters.LineHeight];
                        offset.X = (float)macro.Parameters[(int)Line22Parameters.LowerLeftX];
                        offset.Y = (float)macro.Parameters[(int)Line22Parameters.LowerLeftY];

                        apertureMatrix.RotateAt(rotation, new PointF((float)currentNet.EndX, (float)currentNet.EndY));
                        float lowerLeftX = (float)(currentNet.EndX + offset.X);
                        float lowerLeftY = (float)(currentNet.EndY + offset.Y);
                        points = new PointF[] { new PointF(lowerLeftX, lowerLeftY),
                                                new PointF(lowerLeftX + width, lowerLeftY),
                                                new PointF(lowerLeftX + width, lowerLeftY + height),
                                                new PointF(lowerLeftX, lowerLeftY + height) };
                        break;
                }

            return points;
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

        // Creates a bounding box with the given points.
        private static void UpdateNetBounds(BoundingBox boundingBox, PointF[] points)
        {
            if (points.Length > 0)
            {
                apertureMatrix.TransformPoints(points);

                for (int i = 0; i < points.Length; i++)
                {
                    if (points[i].X < boundingBox.Left)
                        boundingBox.Left = points[i].X;

                    if (points[i].Y > boundingBox.Top)
                        boundingBox.Top = points[i].Y;

                    if (points[i].X > boundingBox.Right)
                        boundingBox.Right = points[i].X;

                    if (points[i].Y < boundingBox.Bottom)
                        boundingBox.Bottom = points[i].Y;
                }
            }
        }
        /// <summary>
        /// Running update of the current net bounding box.
        /// </summary>
        /// <param name="boundingBox">current net's bounding box</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private static void UpdateNetBounds(BoundingBox boundingBox, double x, double y, double width, double height)
        {
            double X1 = x - width, Y1 = y - height;
            double X2 = x + width, Y2 = y + height;
            PointF[] points = new PointF[] { new PointF((float)X1, (float)Y1), new PointF((float)X2, (float)Y2) };

            // Transform the points to the final rendered position, accounting for any scaling, offsets, mirroring, etc.
            apertureMatrix.TransformPoints(points);

            // Check both points against the min/max, since depending on the rotation,
            // mirroring, etc, either point could possibly be a min or max.
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

        /// <summary>
        /// Adds trailing zeros if they are omitted.
        /// </summary>
        /// <param name="integerPart">integer part of the format scale (FS)</param>
        /// <param name="decimalPart">decimal part od the formatscale (FS)</param>
        /// <param name="length">length of coordinate</param>
        /// <param name="coordinate">coordinate value</param>
        private static void AddTrailingZeros(int integerPart, int decimalPart, int length, ref int coordinate)
        {
            int omittedValue = (integerPart + decimalPart) - length;
            if (omittedValue > 0)
            {
                for (int x = 0; x < omittedValue; x++)
                    coordinate *= 10;
            }
        }

        private static double RadiansToDegrees(double radians)
        {
            return radians * (180 / Math.PI);
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        /*private static double Hypotenuse(double x, double y)
        {
            return Math.Sqrt((x * x) + (y * y));
        }*/

    }
}
