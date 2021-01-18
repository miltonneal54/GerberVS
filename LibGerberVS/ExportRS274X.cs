// ExportRS274X.cs - Export a gerber image to a RS274X file.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GerberVS
{
    /// <summary>
    /// Create a RS274X file from a gerber image.
    /// </summary>
    public static class ExportGerberRS274X
    {
        const double GERBV_PRECISION_ANGLE_RAD = 1e-6;
        const double GERBV_PRECISION_LINEAR_INCH = 1e-6;

        /// <summary>
        /// Export a gerber image to RS274X file format.
        /// </summary>
        /// <param name="fullPathName">Full path name to write file to</param>
        /// <param name="inputImage">gerber image to export</param>
        /// <returns></returns>
        public static bool RS274xFromImage(string fullPathName, GerberImage inputImage)
        {
            GerberUserTransform transform = new GerberUserTransform(0, 0, 1, 1, 0, false, false, false);
            return RS274xFromImage(fullPathName, inputImage, transform);
        }

        /// <summary>
        /// Export a gerber image to RS274X file format with user tranformations.
        /// </summary>
        /// <param name="fullPathName">Full path name to write file to</param>
        /// <param name="inputImage">gerber image to export</param>
        /// <param name="transform">apply the user transformations</param>
        /// <returns></returns>
        public static bool RS274xFromImage(string fullPathName, GerberImage inputImage, GerberUserTransform transform)
        {
            const double decimalCoeff = 1e4;
            GerberLevel oldLevel = null;
            GerberNetState oldState = null;
            bool insidePolygon = false;

            try
            {
                using (StreamWriter streamWriter = new StreamWriter(fullPathName, false, Encoding.ASCII))
                {
                    // Duplicate the image, cleaning it in the process.
                    GerberImage imageCopy = GerberImage.Copy(inputImage);
                    // Write header info.
                    streamWriter.WriteLine("G04 This is an RS-274x file exported by *");
                    streamWriter.WriteLine("G04 GerberView Version {0} *", Assembly.GetEntryAssembly().GetName().Version);
                    streamWriter.WriteLine("G04 --End of header info--*");
                    streamWriter.WriteLine("%MOIN*%");
                    streamWriter.WriteLine("%FSLAX34Y34*%");

                    // Check the image info struct for any non-default settings.
                    // Image offset.
                    if ((imageCopy.ImageInfo.OffsetA > 0.0) || (imageCopy.ImageInfo.OffsetB > 0.0))
                        streamWriter.WriteLine("%IOA{0}B{1}*%\n", imageCopy.ImageInfo.OffsetA, imageCopy.ImageInfo.OffsetB);

                    // Image polarity.
                    if (imageCopy.ImageInfo.Polarity == GerberPolarity.Clear)
                        streamWriter.WriteLine("%IPNEG*%\n");

                    else
                        streamWriter.Write("%IPPOS*%\n");

                    // Image name.
                    if (!String.IsNullOrEmpty(imageCopy.ImageInfo.ImageName))
                        streamWriter.Write("%IN{0}*%", imageCopy.ImageInfo.ImageName);

                    // Plotter film.
                    if (!String.IsNullOrEmpty(imageCopy.ImageInfo.PlotterFilm))
                        streamWriter.Write("%PF{0}*%", imageCopy.ImageInfo.PlotterFilm);

                    // Image rotation.
                    if ((imageCopy.ImageInfo.ImageRotation != 0.0) || (transform.Rotation != 0.0))
                        streamWriter.Write("%IR{0}*%", (int)((imageCopy.ImageInfo.ImageRotation + transform.Rotation)) % 360);

                    if ((imageCopy.ImageInfo.ImageJustifyTypeA != GerberImageJustifyType.None) || (imageCopy.ImageInfo.ImageJustifyTypeB != GerberImageJustifyType.None))
                    {
                        streamWriter.Write("%IJA");
                        if (imageCopy.ImageInfo.ImageJustifyTypeA == GerberImageJustifyType.Centre)
                            streamWriter.Write("C");

                        else
                            streamWriter.Write("%{0:0000}", imageCopy.ImageInfo.ImageJustifyOffsetA);

                        streamWriter.Write("B");
                        if (imageCopy.ImageInfo.ImageJustifyTypeB == GerberImageJustifyType.Centre)
                            streamWriter.Write("C");

                        else
                            streamWriter.Write("%{0:0000}", imageCopy.ImageInfo.ImageJustifyOffsetB);

                        streamWriter.WriteLine("*%");

                    }
                    // Handle scale user orientation transforms.
                    if (Math.Abs(transform.ScaleX - 1) > GERBV_PRECISION_LINEAR_INCH || Math.Abs(transform.ScaleY - 1) > GERBV_PRECISION_LINEAR_INCH)
                        streamWriter.WriteLine("%SFA{0:0.0000}B{1:0.0000}*%", transform.ScaleX, transform.ScaleY);

                    // Handle mirror image user orientation transform.
                    if ((transform.MirrorAroundX) || (transform.MirrorAroundY))
                        streamWriter.WriteLine("%MIA{0}dB{1}*%", transform.MirrorAroundY, transform.MirrorAroundX);

                    // Define all apertures.
                    streamWriter.Write("G04 --Define apertures--*\n");
                    WriteApertures(streamWriter, imageCopy);

                    // Write rest of image.
                    streamWriter.WriteLine("G04 --Start main section--*");
                    int currentAperture = 0;
                    GerberNet currentNet;

                    // Skip the first net, since it's always zero due to the way we parse things.
                    for (int netIndex = 1; netIndex < imageCopy.GerberNetList.Count; netIndex++)
                    {
                        currentNet = imageCopy.GerberNetList[netIndex];
                        // Check for "level" changes (RS274X commands)
                        if (currentNet.Level != oldLevel)
                            WriteLevelChange(streamWriter, oldLevel, currentNet.Level);

                        // Check for new "netstate" (more RS274X commands)
                        if (currentNet.NetState != oldState)
                            WriteStateChange(streamWriter, oldState, currentNet.NetState);

                        // Check for "tool" changes.
                        // Also, make sure the aperture number is a valid one, since sometimes the loaded file may refer to invalid apertures.
                        if ((currentNet.Aperture != currentAperture) && (imageCopy.ApertureArray[currentNet.Aperture] != null))
                        {
                            streamWriter.WriteLine("G54D{0}*", currentNet.Aperture);
                            currentAperture = currentNet.Aperture;
                        }

                        oldLevel = currentNet.Level;
                        oldState = currentNet.NetState;

                        int xVal, yVal, endX, endY, centerX, centerY;
                        switch (currentNet.Interpolation)
                        {
                            case GerberInterpolation.LinearX1:
                            case GerberInterpolation.LinearX10:
                            case GerberInterpolation.LinearX01:
                            case GerberInterpolation.LinearX001:
                                // See if we need to write an "aperture off" line to get the pen to the right start point.
                                if ((!insidePolygon) && (currentNet.ApertureState == GerberApertureState.On))
                                {
                                    xVal = (int)Math.Round(currentNet.StartX * decimalCoeff);
                                    yVal = (int)Math.Round(currentNet.StartY * decimalCoeff);
                                    streamWriter.WriteLine("G01X{0:0000000}Y{1:0000000}D02*", xVal, yVal);
                                }

                                xVal = (int)Math.Round(currentNet.StopX * decimalCoeff);
                                yVal = (int)Math.Round(currentNet.StopY * decimalCoeff);
                                streamWriter.Write("G01X{0:0000000}Y{1:0000000}", xVal, yVal);
                                // and finally, write the esposure value.
                                if (currentNet.ApertureState == GerberApertureState.Off)
                                    streamWriter.WriteLine("D02*");

                                else if (currentNet.ApertureState == GerberApertureState.On)
                                    streamWriter.WriteLine("D01*");

                                else
                                    streamWriter.WriteLine("D03*");
                                break;

                            case GerberInterpolation.ClockwiseCircular:
                            case GerberInterpolation.CounterClockwiseCircular:
                                // See if we need to write an "aperture off" line to get the pen to the right start point.
                                if ((!insidePolygon) && (currentNet.ApertureState == GerberApertureState.On))
                                {
                                    xVal = (int)Math.Round(currentNet.StartX * decimalCoeff);
                                    yVal = (int)Math.Round(currentNet.StartY * decimalCoeff);
                                    streamWriter.WriteLine("G01X{0:0000000}Y{1:0000000}D02*", xVal, yVal);
                                }

                                centerX = (int)Math.Round((currentNet.CircleSegment.CenterX - currentNet.StartX) * decimalCoeff);
                                centerY = (int)Math.Round((currentNet.CircleSegment.CenterY - currentNet.StartY) * decimalCoeff);
                                endX = (int)Math.Round(currentNet.StopX * decimalCoeff);
                                endY = (int)Math.Round(currentNet.StopY * decimalCoeff);

                                // Always use multi-quadrant, since it's much easier to export and most all software should support it.
                                streamWriter.WriteLine("G75*");

                                if (currentNet.Interpolation == GerberInterpolation.ClockwiseCircular)
                                    streamWriter.Write("G02");	// Clockwise.

                                else
                                    streamWriter.Write("G03");	// Counter clockwise.

                                // Don't write the I and J values if the exposure is off.
                                if (currentNet.ApertureState == GerberApertureState.On)
                                    streamWriter.Write("X{0:000000}Y{1:0000000}I{2:0000000}J{3:0000000}", endX, endY, centerX, centerY);
                                else
                                    streamWriter.Write("X{0:0000000}Y{1:0000000}", endX, endY);
                                // And finally, write the esposure value.
                                if (currentNet.ApertureState == GerberApertureState.Off)
                                    streamWriter.WriteLine("D02*");

                                else if (currentNet.ApertureState == GerberApertureState.On)
                                    streamWriter.WriteLine("D01*");

                                else
                                    streamWriter.WriteLine("D03*");

                                break;

                            case GerberInterpolation.PolygonAreaStart:
                                streamWriter.WriteLine("G36*");
                                insidePolygon = true;
                                break;

                            case GerberInterpolation.PolygonAreaEnd:
                                streamWriter.WriteLine("G37*");
                                insidePolygon = false;
                                break;

                            default:
                                break;
                        }


                    }

                    streamWriter.WriteLine("M02*");
                    return true;
                }
            }

            catch (Exception ex)
            {
                throw new GerberExportException(Path.GetFileName(fullPathName), ex);
            }
        }

        private static void WriteMacro(StreamWriter streamWriter, Aperture currentAperture, int apertureNumber)
        {
            streamWriter.WriteLine("%AMMACRO{0}*", apertureNumber);
            foreach (SimplifiedApertureMacro sam in currentAperture.SimplifiedMacroList)
            {
                switch (sam.ApertureType)
                {
                    case GerberApertureType.MacroCircle:
                        streamWriter.WriteLine("1,{0},{1:0.000000},{2:0.000000},{3:0.000000}*",
                                       sam.Parameters[(int)CircleParameters.Exposure],
                                       sam.Parameters[(int)CircleParameters.Diameter],
                                       sam.Parameters[(int)CircleParameters.CentreX],
                                       sam.Parameters[(int)CircleParameters.CentreY]);
                        break;

                    case GerberApertureType.MacroOutline:
                        int points;
                        int numberOfPoints = (int)sam.Parameters[(int)OutlineParameters.NumberOfPoints];
                        streamWriter.Write("4,{0},{1},", sam.Parameters[(int)OutlineParameters.Exposure], numberOfPoints);
                        for (points = 0; points <= numberOfPoints; points++)
                            streamWriter.Write("{0:0.000000},{1:0.000000},",
                                       sam.Parameters[points * 2 + (int)OutlineParameters.FirstX],
                                       sam.Parameters[points * 2 + (int)OutlineParameters.FirstY]);

                        streamWriter.WriteLine("{0:0.000000}*", sam.Parameters[points * 2 + 2]);
                        break;

                    case GerberApertureType.MacroPolygon:
                        streamWriter.WriteLine("5,{0},{1},{2:0.0000000},{3:0.0000000},{4:0.0000000},{5:0.0000000}*",
                                       sam.Parameters[(int)PolygonParameters.Exposure],
                                       sam.Parameters[(int)PolygonParameters.NumberOfSides],
                                       sam.Parameters[(int)PolygonParameters.CentreY],
                                       sam.Parameters[(int)PolygonParameters.CentreY],
                                       sam.Parameters[(int)PolygonParameters.Diameter],
                                       sam.Parameters[(int)PolygonParameters.Rotation]);
                        break;

                    case GerberApertureType.MacroMoire:
                        streamWriter.WriteLine("6,{0:0.000000},{1:0.000000},{2:0.000000},{3:0.000000},{4:0.000000},{5:0.000000},{6:0.000000},{7:0.000000},{8:0.000000}*",
                                       sam.Parameters[(int)MoireParameters.CentreX],
                                       sam.Parameters[(int)MoireParameters.CentreY],
                                       sam.Parameters[(int)MoireParameters.OutsideDiameter],
                                       sam.Parameters[(int)MoireParameters.CircleLineWidth],
                                       sam.Parameters[(int)MoireParameters.GapWidth],
                                       sam.Parameters[(int)MoireParameters.NumberOfCircles],
                                       sam.Parameters[(int)MoireParameters.CrosshairLineWidth],
                                       sam.Parameters[(int)MoireParameters.CrosshairLength],
                                       sam.Parameters[(int)MoireParameters.Rotation]);
                        break;

                    case GerberApertureType.MacroThermal:
                        streamWriter.WriteLine("7,{0:0.000000},{1:0.000000},{2:0.000000},{3:0.000000},{4:0.000000},{5:0.000000}*",
                                       sam.Parameters[(int)ThermalParameters.CentreX],
                                       sam.Parameters[(int)ThermalParameters.CentreY],
                                       sam.Parameters[(int)ThermalParameters.OutsideDiameter],
                                       sam.Parameters[(int)ThermalParameters.InsideDiameter],
                                       sam.Parameters[(int)ThermalParameters.CrosshairLineWidth],
                                       sam.Parameters[(int)ThermalParameters.Rotation]);
                        break;

                    case GerberApertureType.MacroLine20:
                        streamWriter.WriteLine("20,{0},{1:0.000000},{2:0.000000},{3:0.000000},{4:0.000000},{5:0.000000},{6:0.000000}*",
                                        sam.Parameters[(int)Line20Parameters.Exposure],
                                        sam.Parameters[(int)Line20Parameters.LineWidth],
                                        sam.Parameters[(int)Line20Parameters.StartX],
                                        sam.Parameters[(int)Line20Parameters.StartY],
                                        sam.Parameters[(int)Line20Parameters.EndX],
                                        sam.Parameters[(int)Line20Parameters.EndY],
                                        sam.Parameters[(int)Line20Parameters.Rotation]);
                        break;

                    case GerberApertureType.MacroLine21:
                        streamWriter.WriteLine("21,{0},{1:0.000000},{2:0.000000},{3:0.000000},{4:0.000000},{5:0.000000}*",
                                       sam.Parameters[(int)Line21Parameters.Exposure],
                                       sam.Parameters[(int)Line21Parameters.LineWidth],
                                       sam.Parameters[(int)Line21Parameters.LineHeight],
                                       sam.Parameters[(int)Line21Parameters.CentreX],
                                       sam.Parameters[(int)Line21Parameters.CentreY],
                                       sam.Parameters[(int)Line21Parameters.Rotation]);
                        break;

                    case GerberApertureType.MacroLine22:
                        streamWriter.WriteLine("22,{0},{1:0.000000},{2:0.000000},{3:0.000000},{4:0.000000},{5:0.000000}*",
                                       sam.Parameters[(int)Line22Parameters.Exposure],
                                       sam.Parameters[(int)Line22Parameters.LineWidth],
                                       sam.Parameters[(int)Line22Parameters.LineHeight],
                                       sam.Parameters[(int)Line22Parameters.LowerLeftX],
                                       sam.Parameters[(int)Line22Parameters.LowerLeftY],
                                       sam.Parameters[(int)Line22Parameters.Rotation]);
                        break;
                }
            }

            streamWriter.WriteLine("%");
            streamWriter.WriteLine("%ADD{0}MACRO{1}*%", apertureNumber, apertureNumber);
        }

        private static void WriteApertures(StreamWriter streamWriter, GerberImage image)
        {
            Aperture currentAperture;
            bool writeAperture = true;
            int numberOfRequiredParameters = 0, numberOfOptionalParameters = 0;

            for (int i = Gerber.MinimumAperture; i < Gerber.MaximumApertures; i++)
            {
                writeAperture = true;
                currentAperture = image.ApertureArray[i];
                if (currentAperture == null)
                    continue;

                switch (currentAperture.ApertureType)
                {
                    case GerberApertureType.Circle:
                        streamWriter.Write("%ADD{0}", i);
                        streamWriter.Write("C,");
                        numberOfRequiredParameters = 1;
                        numberOfOptionalParameters = 2;
                        break;

                    case GerberApertureType.Rectangle:
                        streamWriter.Write("%ADD{0}", i);
                        streamWriter.Write("R,");
                        numberOfRequiredParameters = 2;
                        numberOfOptionalParameters = 2;
                        break;
                    case GerberApertureType.Oval:
                        streamWriter.Write("%ADD{0}", i);
                        streamWriter.Write("O,");
                        numberOfRequiredParameters = 2;
                        numberOfOptionalParameters = 2;
                        break;

                    case GerberApertureType.Polygon:
                        streamWriter.Write("%ADD{0}", i);
                        streamWriter.Write("P,");
                        numberOfRequiredParameters = 2;
                        numberOfOptionalParameters = 3;
                        break;

                    case GerberApertureType.Macro:
                        WriteMacro(streamWriter, currentAperture, i);
                        writeAperture = false;
                        break;

                    default:
                        writeAperture = false;
                        break;
                }

                if (writeAperture)
                {
                    // Write the parameter list.
                    for (int j = 0; j < (numberOfRequiredParameters + numberOfOptionalParameters); j++)
                    {
                        if ((j < numberOfRequiredParameters) || (currentAperture.Parameters[j] != 0))
                        {
                            // Print the "X" character to separate the parameters.
                            if (j > 0)
                                streamWriter.Write("X");

                            streamWriter.Write("{0:0.000}", currentAperture.Parameters[j]);
                        }
                    }

                    streamWriter.WriteLine("*%");
                }
            }
        }

        private static void WriteLevelChange(StreamWriter file, GerberLevel oldLevel, GerberLevel newLevel)
        {
            if (oldLevel == null)
                return;

            if (oldLevel.Polarity != newLevel.Polarity)
            {
                // Polarity changed.
                if ((newLevel.Polarity == GerberPolarity.Clear))
                    file.WriteLine("%LPC*%");

                else
                    file.WriteLine("%LPD*%");
            }
        }

        private static void WriteStateChange(StreamWriter file, GerberNetState oldState, GerberNetState newState)
        {
        }
    }
}

