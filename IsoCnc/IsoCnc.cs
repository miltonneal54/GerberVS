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
using System.Diagnostics.Contracts;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Operation.Overlay;

namespace IsoCnc
{
    internal class IsoCnc
    {
        static bool mirrorX = false;
        static bool metric_mode = true;
        static double iso_tool_size = 0.189;
        static double iso_path_overlap = 0.2;
        static double iso_isolation_min = 0.6;
        static bool iso_milling_conventional = true;
        static double unit_coefficient = 25.4;

        static void Main(string[] args)
        {
            //arg1 gerber file to load
            //arg2 output cnc file
            //arg3 config file specifying Options
            //arg4 optional boolean for mirrorX
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: isocnc <gerber input file> <nc output file> <options config file> [mirrorX = false]");
                System.Environment.Exit(-1);
            }
            var fullPathName = args[0];
            var configPath = args[2];
            if (args.Length > 3)
            {
                if (Boolean.TryParse(args[3], out bool res))
                    mirrorX = res;
            }
            var config = Configuration.ReadConfig(configPath);

            if (Gerber.IsGerberRS427X(fullPathName))
            {
                double[] mirror_line = null;
                if (mirrorX)
                    mirror_line = new double[4] { 0, 0, 0, 1 };
                if (config != null)
                {
                    config.BooleanGetValue("metric_mode", ref metric_mode);
                    config.DoubleGetValue("iso_tool_size", ref iso_tool_size, false);
                    config.DoubleGetValue("iso_path_overlap", ref iso_path_overlap, true);
                    config.DoubleGetValue("iso_isolation_min", ref iso_isolation_min, true);
                    config.BooleanGetValue("iso_milling_conventional", ref iso_milling_conventional, true);
                    unit_coefficient = metric_mode ? 25.4 : 1.0;
                }
                var parsedImage = Gerber.ParseGerber(fullPathName);
                var geometries = GerberShapes.CreateGeometry(parsedImage, iso_milling_conventional, mirror_line);
                /*
                                GMLWriter writer = new GMLWriter();
                                Stream file = new FileStream("foo.gml", FileMode.Create);
                                writer.Write(geometries, file);
                                file.Close();
                */
                using (StreamWriter streamWriter = new StreamWriter(args[1], false, Encoding.ASCII))
                {
                    CncIsoWriter cncWriter = new CncIsoWriter(config);
                    var last_path = false;
                    for (int i = 0; !last_path; i++)
                    {
                        var dist = ((iso_tool_size / 2) + i * (1 - iso_path_overlap) * iso_tool_size);
                        var path = geometries.Buffer(dist / unit_coefficient);
                        last_path = (dist + (iso_tool_size / 2)) >= iso_isolation_min;
                        var options = new CncIsoWriter.Options() { ccw = iso_milling_conventional, path_number = i, tool_diameter = iso_tool_size, mirrorX = mirrorX, last_tool = true, last_path = last_path };
                        cncWriter.Write(path, options, streamWriter);
                    }
                }
            }
            else if (Drill.IsDrillFile(fullPathName))
            {
                var parsedImage = Drill.ParseDrillFile(fullPathName);
                using (StreamWriter streamWriter = new StreamWriter(args[1], false, Encoding.ASCII))
                {
                    CncDrillWriter cncDrillWriter= new CncDrillWriter(config);
                    var options = new CncDrillWriter.Options() { mirrorX = mirrorX };
                    cncDrillWriter.Write(parsedImage, options, streamWriter);
                }
            }
            else
                throw new GerberFileException("Unknown file type: " + Path.GetFileName(fullPathName));
        }




     
    }

}
