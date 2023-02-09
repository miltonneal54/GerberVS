/*  Copyright (C) 2022-2023 Patrick H Dussud <Patrick.Dussud@outlook.com>
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Cnc;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using Config;
using GerberVS;
using System.Globalization;

namespace IsoCnc
{
    internal class CncIsoWriter
    {
        public struct Options
        {
            public bool mirrorX;
            public bool ccw;
            public double tool_diameter;
            public int path_number;
            public bool last_tool;
            public bool last_path;
        };
        Machine machine;
        double current_tool_diameter = 0;
        bool start_path = true;
        ConfigDictionary config;
        bool metric_mode = true;
        double unit_factor = 25.4; //becomes 1 if metric_mode is false; 
        bool generate_relative_code = false;
        double iso_cut_depth;
        double iso_feed_rate;
        double iso_plunge_rate;
        double iso_lift = 2.0;
        double iso_spindle_speed;
        string file_end_code;
        string coordinate_system;
        string coordinate_system_mirror;
        string tool_change_code;
        double tool_change_height;
        Dictionary<string, int> tool_change_map = new Dictionary<string, int>()
        {
            {"tool_number", 0 },
            {"tool_diameter", 1 },
            {"tool_lift", 2 },
            {"tool_change_height", 3 },
            {"spindle_speed", 4 },
         };
        string tool_change_format;


        public CncIsoWriter(ConfigDictionary config, StreamWriter stream) 
        {
            machine = new Machine(stream, config);
            CodeTemplate tool_change_template = new CodeTemplate(tool_change_map);
            this.config = config;
            config.BooleanGetValue("metric_mode", ref metric_mode);
            config.DoubleGetValue("iso_cut_depth", ref iso_cut_depth, false);
            config.DoubleGetValue("iso_feed_rate", ref iso_feed_rate, false);
            config.DoubleGetValue("iso_plunge_rate", ref iso_plunge_rate, false);
            config.DoubleGetValue("iso_lift", ref iso_lift, true);
            config.DoubleGetValue("iso_spindle_speed", ref iso_spindle_speed, false);
            config.string_get_value("file_end_code", ref file_end_code, false);
            config.string_get_value("coordinate_system", ref coordinate_system, false);
            config.string_get_value("coordinate_system_mirror", ref coordinate_system_mirror, false);
            config.string_get_value("tool_change_code", ref tool_change_code, true);
            config.DoubleGetValue("tool_change_height", ref tool_change_height);
            tool_change_format = tool_change_template.CreateFormatString(tool_change_code);
            unit_factor = metric_mode ? 25.4 : 1.0;

        }
        private void write_ring(Coordinate[] ring, bool ccw)
        {
            bool first = true;
            IEnumerable<Coordinate> coordinates = ring;
            if (ccw ^ Orientation.IsCCW(ring))
            {
                coordinates = ring.Reverse();
            }
            foreach (Coordinate c in coordinates)
            {
                if (first)
                {
                    first = false;
                    machine.rapidMove(c.X, c.Y);
                    machine.move(null, null, iso_cut_depth / unit_factor, iso_plunge_rate / unit_factor); //plunge
                    machine.move(null, null, null, iso_feed_rate / unit_factor); //set feed rate for cuts
                    continue;
                }
                machine.move(c.X, c.Y);
            }
            machine.rapidMove(null, null, iso_lift / unit_factor); //lift Z
        }
        public void Write(Geometry shape, Options options) 
        {
            bool ccw = options.ccw;
            bool mirrorX = options.mirrorX;
            if (start_path)
            {
                start_path = false;
                machine.insertCode(string.Format("(  Tool Size)\n({0:0.000} )", options.tool_diameter));
                machine.setInputUnit(false);
                if (mirrorX)
                    machine.insertCode(coordinate_system_mirror);
                else
                    machine.insertCode(coordinate_system);
                machine.metricMode(metric_mode);
                machine.relativeMode(generate_relative_code);
                machine.SpindleOn(iso_spindle_speed);
                machine.rapidMove(null, null, iso_lift / unit_factor); //lift Z
            } else if (Math.Abs(current_tool_diameter - options.tool_diameter) > 0.001)
            {
                //change tool 
                machine.insertCode(String.Format(tool_change_format, 2, options.tool_diameter, iso_lift, tool_change_height, iso_spindle_speed));
            }
            current_tool_diameter = options.tool_diameter;
            for (int i = 0; i < shape.NumGeometries; i++) 
            {
                if (shape.GetGeometryN(i) is Polygon)
                {
                    var poly = shape.GetGeometryN(i) as Polygon;
                    write_ring(poly.ExteriorRing.Coordinates, ccw);
                    foreach (var r in poly.InteriorRings)
                    {
                        write_ring(r.Coordinates, ccw);
                    }
                }
                else
                {
                    Debug.Assert(false);
                }
            }
            if (options.last_tool && options.last_path)
                machine.insertCode(file_end_code);
        }
    }
    internal class CncDrillWriter
    {
        public struct Options
        {
            public bool mirrorX;
        };
        Machine machine;
        ConfigDictionary config;
        //settings
        double unit_factor = 25.4;
        bool generate_relative_code = false; //because of insertCode relative mode isn't possible
        bool metric_mode = true;
        bool drill_slots = false;
        double drill_depth = -2.5;
        double drill_lift = 3.0;
        double drill_feed_rate = 400;
        double drill_pause = 0.05;
        double mill_feed_rate = 100;
        double tool_change_height = 40;
        double drill_spindle_speed = 26000;
        double slot_drill_overlap = 0.25;
        string tool_change_code = "M5\nG0 Z{tool_change_height}\nG0 X0 Y0\nM6 T{tool_number} ({tool_diameter} mm)\nM0\nM03 S{spindle_speed}\nG0 Z{tool_lift}";
        Dictionary<string, int> tool_change_map = new Dictionary<string, int>()
        {
            {"tool_number", 0 },
            {"tool_diameter", 1 },
            {"tool_lift", 2 },
            {"tool_change_height", 3 },
            {"spindle_speed", 4 },
        };
        string tool_change_format;
        string file_end_code;
        string coordinate_system;
        string coordinate_system_mirror;
        public CncDrillWriter(ConfigDictionary config, TextWriter streamWriter)
        {
            machine = new Machine(streamWriter, config);
            this.config = config;
            CodeTemplate tool_change_template = new CodeTemplate(tool_change_map);

            if (config != null)
            {
                config.BooleanGetValue("metric_mode", ref metric_mode);
                config.BooleanGetValue("drill_slots", ref drill_slots, false);
                config.DoubleGetValue("drill_depth", ref drill_depth, true);
                config.DoubleGetValue("drill_lift", ref drill_lift, true);
                config.DoubleGetValue("drill_feed_rate", ref drill_feed_rate, true);
                config.DoubleGetValue("slot_drill_overlap", ref slot_drill_overlap, true);
                config.DoubleGetValue("mill_feed_rate", ref mill_feed_rate, true);
                config.DoubleGetValue("tool_change_height", ref tool_change_height, true);
                config.DoubleGetValue("drill_spindle_speed", ref drill_spindle_speed, true);
                config.string_get_value("tool_change_code", ref tool_change_code, true);
                config.string_get_value("file_end_code", ref file_end_code, false);
                config.string_get_value("coordinate_system", ref coordinate_system, false);
                config.string_get_value("coordinate_system_mirror", ref coordinate_system_mirror, false);
            }
            tool_change_format = tool_change_template.CreateFormatString(tool_change_code);
            unit_factor = metric_mode ? 25.4 : 1.0;
        }
        public void Write(GerberImage inputImage, Options options)
        {
            bool mirrorX = options.mirrorX;
            List<int> apertureList = new List<int>();
            GerberNet currentNet;

            // Copy the image, cleaning it in the process.
            GerberImage newImage = inputImage; //GerberImage.Copy(inputImage);
                                               // Write header info.
            machine.setInputUnit(false);
            if (mirrorX)
                machine.insertCode(coordinate_system_mirror);
            else
                machine.insertCode(coordinate_system);

            machine.metricMode(metric_mode);
            machine.relativeMode(generate_relative_code);

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
                        tools.AppendLine(string.Format("( T{0:00} | {1:0.0000}mm {2:0.0000}in )", i, size * coefficient, size * coefficient / 25.4));
                        // Add the "approved" aperture to our valid list.
                        apertureList.Add(i);
                        break;
                    default:
                        break;
                }
            }

            machine.insertCode(tools.ToString());

            machine.insertCode("(end of header)");

            machine.SpindleOn(drill_spindle_speed);
            machine.rapidMove(null, null, drill_lift / unit_factor); //lift Z

            for (int i = 0; i < apertureList.Count; i++)
            {
                int apertureIndex = apertureList[i];
                var currAperture = newImage.ApertureArray()[apertureIndex];
                double diameter_mm = currAperture.Parameters()[0] * (currAperture.Unit == GerberUnit.Inch ? 25.4 : 1.0);

                // Write tool change.
                machine.insertCode(String.Format(tool_change_format, apertureIndex, diameter_mm / (metric_mode ? 1.0 : 25.4), drill_lift, tool_change_height, drill_spindle_speed));

                // Run through all nets and look for holes using this aperture.
                for (int netIndex = 0; netIndex < newImage.GerberNetList.Count; netIndex++)
                {
                    currentNet = newImage.GerberNetList[netIndex];
                    if (currentNet.Aperture != apertureIndex)
                        continue;
                    if (mirrorX)
                    {
                        currentNet.StartX = -currentNet.StartX;
                        currentNet.EndX = -currentNet.EndX;
                    }
                    switch (currentNet.ApertureState)
                    {
                        case GerberApertureState.Flash:
                            machine.drillMove(currentNet.StartX, currentNet.StartY, drill_depth / unit_factor, drill_lift /unit_factor, drill_feed_rate /unit_factor, drill_pause);
                            break;

                        case GerberApertureState.On:    // Cut slot.
                            if (currentNet.Interpolation == GerberInterpolation.Linear)
                            {
                                if (drill_slots)
                                {
                                    double slot_drill_length = (1 - slot_drill_overlap) * diameter_mm / unit_factor;
                                    LineSegment slot = new LineSegment(currentNet.StartX, currentNet.StartY,
                                                                       currentNet.EndX, currentNet.EndY);
                                    var s_length = slot.Length;
                                    var n_drills_min = Math.Round(s_length / slot_drill_length);
                                    var n_drills = n_drills_min;
                                    if (n_drills_min > 0)
                                    {
                                        slot_drill_length = s_length / n_drills_min;
                                        n_drills = s_length / slot_drill_length;
                                    }
                                    for (int d = 0; d <= n_drills; d++)
                                    {
                                        Coordinate a = slot.PointAlong(d * slot_drill_length / s_length);
                                        machine.drillMove(a.X, a.Y, drill_depth / unit_factor, drill_lift / unit_factor, drill_feed_rate / unit_factor, drill_pause);
                                    }
                                }
                                else
                                {
                                    machine.rapidMove(currentNet.StartX, currentNet.StartY);
                                    machine.move(z_abs: drill_depth / unit_factor, feed_rate: drill_feed_rate / unit_factor);
                                    machine.move(currentNet.EndX, currentNet.EndY);
                                    machine.rapidMove(z_abs: drill_lift / unit_factor);
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }
            }

            // Write footer.
            machine.insertCode(file_end_code);
        }
    }
}
