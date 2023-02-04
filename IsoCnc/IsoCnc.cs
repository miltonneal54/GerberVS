using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnc;
using GerberVS;
using Config;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.IO.GML2;
using System.Net;

namespace IsoCnc
{
    internal class IsoCnc
    {
        static bool mirrorX = false;
        static bool conventional = true;
        static double iso_tool_size = 0.189;
        static double iso_trace_overlap = 0.2;

        static bool drill_slots = false;
        static double drill_depth = -2.5;
        static double drill_lift = 3.0;
        static double drill_feed_rate = 400;
        static double drill_pause = 0.05;
        static double mill_feed_rate = 100;

        static void Main(string[] args)
        {
            //arg1 gerber file to load
            //arg2 output cnc file
            //arg3 config file specifying options
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: isocnc <gerber input file> <nc output file> <options config file>");
                System.Environment.Exit(-1);
            }
            var fullPathName = args[0];
            var configPath = args[2];
            var config = Configuration.ReadConfig(configPath);
            if (config != null)
            {
                config.DoubleGetValue("drill_depth", ref drill_depth, true);
                config.DoubleGetValue("drill_lift", ref drill_lift, true);
                config.DoubleGetValue("drill_feed_rate", ref drill_feed_rate, true);
                config.DoubleGetValue("mill_feed_rate", ref mill_feed_rate, true);
            }
            if (Gerber.IsGerberRS427X(fullPathName))
            {
                double[] mirror_line = null;
                var parsedImage = Gerber.ParseGerber(fullPathName);
                if (mirrorX)
                    mirror_line = new double[4] { 0, 0, 0, 1 };
                var geometries = GerberShapes.CreateGeometry(parsedImage, conventional, mirror_line);
                /*
                                GMLWriter writer = new GMLWriter();
                                Stream file = new FileStream("foo.gml", FileMode.Create);
                                writer.Write(geometries, file);
                                file.Close();
                */
                using (StreamWriter streamWriter = new StreamWriter("foo.nc", false, Encoding.ASCII))
                {
                    CncWriter cncWriter = new CncWriter(config);
                    for (int i = 0; i < 4; i++)
                    {
                        var dist = ((iso_tool_size / 2) + i * (1 - iso_trace_overlap) * iso_tool_size) / 25.4;
                        var path = geometries.Buffer(dist);
                        cncWriter.Write(path, conventional, streamWriter);
                    }
                }
            }
            else if (Drill.IsDrillFile(fullPathName))
            {
                //TODO: handle mirroring of drills
                var parsedImage = Drill.ParseDrillFile(fullPathName);
                DrillFileFromImage(args[1], parsedImage, config);
            }

            else
                throw new GerberFileException("Unknown file type: " + Path.GetFileName(fullPathName));
        }




        /// <summary>
        /// Export a gerber image to NC file format with user tranformations.
        /// </summary>
        /// <param name="fullPathName">Full path name to write file to</param>
        /// <param name="inputImage">gerber image to export</param>
        /// <returns></returns>
        public static void DrillFileFromImage(string fullPathName, GerberImage inputImage, ConfigDictionary config)
        {

            double DecimalCoeffient = 25.4;
            List<int> apertureList = new List<int>();
            GerberNet currentNet;
            try
            {
                using (StreamWriter streamWriter = new StreamWriter(fullPathName, false, Encoding.ASCII))
                {
                    var machine = new Machine(streamWriter, config);
                    // Copy the image, cleaning it in the process.
                    GerberImage newImage = inputImage; //GerberImage.Copy(inputImage);
                    // Write header info.
                    machine.setInputUnit(true);
                    machine.metricMode(true);
                    machine.relativeMode(false);

                    StringBuilder tools = new StringBuilder();
                    tools.AppendLine("( Tool| Size )");
                    // Define all apertures.
                    Aperture currentAperture;
                    for (int i = 0; i < Gerber.MaximumApertures; i++)
                    {
                        currentAperture = newImage.ApertureArray()[i];
                        if (currentAperture == null)
                            continue;

                        switch (currentAperture.ApertureType)
                        {
                            case GerberApertureType.Circle:
                                double coefficient = currentAperture.Unit == GerberUnit.Inch ? 25.4 : 1.0;
                                double size = currentAperture.Parameters()[0];
                                tools.AppendLine(string.Format("( T{0:00} | {1:0.0000}mm {2:0.0000}in )", i, size*coefficient, size*coefficient / 25.4));
                                // Add the "approved" aperture to our valid list.
                                apertureList.Add(i);
                                break;
                            default:
                                break;
                        }
                    }

                    machine.insertCode(tools.ToString());

                    machine.insertCode("(end of header)");    // End of header.
                    // Write rest of image
                    for (int i = 0; i < apertureList.Count; i++)
                    {
                        int apertureIndex = apertureList[i];

                        // Write tool change.
                        machine.insertCode(String.Format("M6 T{0}", apertureIndex));

                        // Run through all nets and look for drills using this aperture.
                        for (int netIndex = 0; netIndex < newImage.GerberNetList.Count; netIndex++)
                        {
                            currentNet = newImage.GerberNetList[netIndex];
                            if (currentNet.Aperture != apertureIndex)
                                continue;

                            switch (currentNet.ApertureState)
                            {
                                case GerberApertureState.Flash:
                                    machine.drillMove(currentNet.StartX * DecimalCoeffient, currentNet.StartY * DecimalCoeffient, drill_depth, drill_lift, drill_feed_rate, drill_pause);
                                    break;

                                case GerberApertureState.On:    // Cut slot.
                                    if (currentNet.Interpolation == GerberInterpolation.Linear)
                                    {
                                        if (drill_slots)
                                        {
                                            machine.insertCode(string.Format("(drill slot from X{0:0.0000} Y{1:0.0000} to X{2:0.0000} Y{3:0.0000})",
                                                                               currentNet.StartX * DecimalCoeffient,
                                                                               currentNet.StartY * DecimalCoeffient,
                                                                               currentNet.EndX * DecimalCoeffient,
                                                                               currentNet.EndY * DecimalCoeffient));
                                        }
                                        else
                                        {
                                            machine.rapidMove(currentNet.StartX * DecimalCoeffient, currentNet.StartY * DecimalCoeffient);
                                            machine.move(z_abs:drill_depth, feed_rate:drill_feed_rate);
                                            machine.move(currentNet.EndX * DecimalCoeffient, currentNet.EndY * DecimalCoeffient);
                                            machine.rapidMove(z_abs:drill_lift);
                                        }
                                    }
                                    break;

                                default:
                                    break;
                            }
                        }
                    }

                    // Write footer.
                    streamWriter.WriteLine("M30");
                }
            }

            catch (Exception ex)
            {
                throw new GerberExportException(Path.GetFileName(fullPathName), ex);
            }
        }
    }

}
