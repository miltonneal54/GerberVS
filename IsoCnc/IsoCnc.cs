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
using System.IO;
using System.Text;
using GerberVS;
using Config;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index;

namespace IsoCnc
{
    internal class IsoCnc
    {
        static double unit_coefficient = 25.4;

        static void Main(string[] args)
        {
            //arg0 gerber file to load
            //arg1 output cnc file
            //arg2 config file specifying Options
            //arg3 optional boolean for mirrorX

            bool mirrorX = false;

            if (args.Length < 3)
            {
                Console.WriteLine("Usage: isocnc <gerber input file> <nc output file> <options config file> [mirrorX = false]");
                System.Environment.Exit(-1);
            }
            var fullPathName = args[0];
            var outputPathName = args[1];
            var configPath = args[2];
            if (args.Length > 3)
            {
                if (Boolean.TryParse(args[3], out bool res))
                    mirrorX = res;
            }
            string borderPathName = null;
            if (args.Length > 4)
            {
                borderPathName = args[4]; //this is experimental and not documented.
            }
            var config = Configuration.ReadConfig(configPath);

            if (Gerber.IsGerberRS427X(fullPathName))
            {            
                bool metric_mode = true;
                double iso_tool_size = 0.189;
                double iso_path_overlap = 0.2;
                double iso_isolation_min = 0.6;
                double iso_isolation_max = 0;
                bool iso_milling_conventional = true;
                double[] mirror_line = null;
                double iso_tool2_size = 0;
                if (mirrorX)
                    mirror_line = new double[] { 0, 0, 0, 1 };
                if (config != null)
                {
                    config.BooleanGetValue("metric_mode", ref metric_mode, true);
                    config.DoubleGetValue("iso_tool_size", ref iso_tool_size, false);
                    config.DoubleGetValue("iso_path_overlap", ref iso_path_overlap, true);
                    config.DoubleGetValue("iso_isolation_min", ref iso_isolation_min, false);
                    config.DoubleGetValue("iso_isolation_max", ref iso_isolation_max, true);
                    config.BooleanGetValue("iso_milling_conventional", ref iso_milling_conventional, true);
                    config.DoubleGetValue("iso_tool2_size", ref iso_tool2_size, true);
                    unit_coefficient = metric_mode ? 25.4 : 1.0;
                    if (iso_isolation_max < iso_isolation_min)
                        iso_isolation_max = iso_isolation_min;
                }
                var parsedImage = Gerber.ParseGerber(fullPathName);
                var geometry = GerberShapes.CreateGeometry(parsedImage, iso_milling_conventional, mirror_line);
                Geometry border = null;
                if (borderPathName != null)
                {
                    var boder_image = Gerber.ParseGerber(borderPathName);
                    var border_polygon = GerberShapes.CreateGeometry(boder_image, iso_milling_conventional, mirror_line);
                    border = make_border_mask(border_polygon);
                }
                using (StreamWriter streamWriter = new StreamWriter(outputPathName, false, Encoding.ASCII))
                {
                    CncIsoWriter cncWriter = new CncIsoWriter(config, streamWriter);
                    var last_path = false;
                    var tool_diameter = iso_tool_size;
                    var dist = tool_diameter / 2;
                    for (int i = 0; !last_path; i++)
                    {
                        var last_tool = !(tool_diameter < iso_tool2_size);
                        last_path = (dist + (tool_diameter / 2)) >= (last_tool ? (border == null ? iso_isolation_max : 1E6): iso_isolation_min);
                        var path = geometry.Buffer(dist / unit_coefficient);
                        //check if all of the polygons are isolated by the first pass
                        if (i == 0)
                        {
                            if (path.NumGeometries < geometry.NumGeometries)
                            {
                                Console.WriteLine("The tool diam. is too large. Not all of the copper islands are isolated");
                            }
                        }
                        if (border != null)
                        {
                            var new_path = path.Difference(border);
                            if ((new_path.NumGeometries == 1) && (new_path.GetGeometryN(1) is Polygon p))
                            {
                                if ((p.NumInteriorRings == 0) && path.Intersects(border))
                                {
                                    last_path = true;
                                }
                            }
                            path = new_path;
                        }
                        var options = new CncIsoWriter.Options() { ccw = iso_milling_conventional, path_number = i, tool_diameter = tool_diameter, mirrorX = mirrorX, last_tool = last_tool, last_path = last_path };
                        cncWriter.Write(path, options);
                        if (last_path && (tool_diameter == iso_tool_size) && (iso_tool2_size > iso_tool_size)) //switch to larger tool
                        {
                            last_path = false;
                            var old_tool_diameter = tool_diameter;
                            tool_diameter = iso_tool2_size;
                            dist += (tool_diameter / 2) - (1 - iso_path_overlap) * (old_tool_diameter / 2);
                        }
                        else
                        {
                            dist += (1 - iso_path_overlap) * tool_diameter;
                        }
                    }
                }
            }
            else if (Drill.IsDrillFile(fullPathName))
            {
                var parsedImage = Drill.ParseDrillFile(fullPathName);
                using (StreamWriter streamWriter = new StreamWriter(outputPathName, false, Encoding.ASCII))
                {
                    CncDrillWriter cncDrillWriter= new CncDrillWriter(config, streamWriter);
                    var options = new CncDrillWriter.Options() { mirrorX = mirrorX };
                    cncDrillWriter.Write(parsedImage, options);
                }
            }
            else
                throw new GerberFileException("Unknown file type: " + Path.GetFileName(fullPathName));
        }

        static private Polygon make_border_mask(Geometry border)
        {
            var factory = border.Factory;
            Geometry inner = factory.CreateEmpty(Dimension.Surface);
            LinearRing inner_ring;
            for (int i = 0; i < border.NumGeometries; i++)
            {
                if (border.GetGeometryN(i) is Polygon pol)
                {
                    foreach (var h in pol.Holes)
                    {
                        inner = inner.Union(factory.CreatePolygon(h));
                    }
                }
            }
            if (inner.OgcGeometryType == OgcGeometryType.Polygon)
            {
                inner_ring = ((Polygon)inner).Shell;
            }
            else
                return null;
            var shell = ((Polygon)inner_ring.Buffer(2)).Shell;
            return factory.CreatePolygon(shell, new LinearRing[] { inner_ring });
        }
     
    }

}
