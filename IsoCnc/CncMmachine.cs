using NetTopologySuite.Noding;
using System;
using System.Collections.Generic;
using System.IO;
using Config;
using static Config.Configuration;
using IsoCnc;

namespace Cnc
{
    public class Machine
    {
        //config variables
        string rapid_move_code = "G0";
        string linear_move_code = "G1";
        static string drill_code = "G82 X{x:0.0000} Y{y:0.0000} Z{depth:0.0000} F{feed_rate:0.00} R{drill_lift:0.0000} P{drill_pause:0.00}";
        static Dictionary<string, int> drill_code_map = new Dictionary<string, int>()
        {
            {"x", 0 },
            {"y", 1 },
            {"depth", 2 },
            {"feed_rate", 4 },
            {"drill_lift", 3 },
            {"drill_pause", 5 }
        };
        bool generate_relative_code = false;
        bool generate_metric = true;
        //end of config variables
        double x_coord = 0;
        double y_coord = 0;
        double z_coord = 0;
        double drill_depth = 0;
        double drill_lift = 2;
        double drill_feed_rate = 100;
        double drill_pause = 0;
        string drill_format;
        bool input_unit_metric = false;
        double input_scale = 25.4;
        double output_scale = 1.0;
        TextWriter tw;
        public Machine(TextWriter tw, ConfigDictionary config)
        {
            this.tw = tw;
            if (config != null ) 
            {
                config.string_get_value("rapid_move_code", ref rapid_move_code, true);
                config.string_get_value("linear_move_code", ref linear_move_code, true);
                config.string_get_value("drill_code", ref drill_code, true);
                config.BooleanGetValue("generate_relative_code", ref generate_relative_code, true);
                config.BooleanGetValue("generate_metric", ref generate_metric, true);
                config.DoubleGetValue("drill_feed_rate", ref drill_feed_rate, true);
            }
            CodeTemplate drill_template = new CodeTemplate(drill_code_map);
            drill_format = drill_template.CreateFormatString(drill_code);
        }
        public void setInputUnit(bool metric)
        {
            input_unit_metric = metric;
            input_scale = metric ? 1.0 : 25.4;
        }
        private void Scale (ref double? scalar)
        {   if (scalar.HasValue)
                scalar = scalar * input_scale * output_scale;
        }
        private void Scale(ref double scalar)
        {
            scalar = scalar * input_scale * output_scale;
        }
        public void move(double? x_abs = null, double? y_abs = null, double? z_abs = null, double? feed_rate = null)
        {
            Scale(ref x_abs); Scale(ref y_abs); Scale(ref z_abs); Scale(ref feed_rate);
            if (generate_relative_code)
            {
                tw.WriteLine(linear_move_code + (x_abs.HasValue ? " X{0:##0.####}" : "") + (y_abs.HasValue ? " Y{1:##0.####}" : "") + (z_abs.HasValue ? " Z{2:##0.####}" : "") + (feed_rate.HasValue ? " F{3}" : ""),
                                (x_abs ?? x_coord) - x_coord, (y_abs ?? y_coord) - y_coord, (z_abs ?? z_coord) - z_coord, feed_rate ?? 0);
            }
            else
                tw.WriteLine(linear_move_code + (x_abs.HasValue ? " X{0:##0.####}" : "") + (y_abs.HasValue ? " Y{1:##0.####}" : "") + (z_abs.HasValue ? " Z{2:##0.####}" : "") + (feed_rate.HasValue ? " F{3}" : ""),
                                x_abs ?? x_coord, y_abs ?? y_coord, z_abs ?? z_coord, feed_rate ?? 0);
            x_coord = x_abs ?? x_coord;
            y_coord = y_abs ?? y_coord;
            z_coord = z_abs ?? z_coord;
        }
        public void rapidMove(double? x_abs = null, double? y_abs = null, double? z_abs = null, double? feed_rate = null)
        {
            Scale(ref x_abs); Scale(ref y_abs); Scale(ref z_abs); Scale(ref feed_rate);
            if (generate_relative_code)
            {
                tw.WriteLine(rapid_move_code + (x_abs.HasValue ? " X{0:##0.####}" : "") + (y_abs.HasValue ? " Y{1:##0.####}" : "") + (z_abs.HasValue ? " Z{2:##0.####}" : "") + (feed_rate.HasValue ? " F{3}" : ""),
                                (x_abs ?? x_coord) - x_coord, (y_abs ?? y_coord) - y_coord, (z_abs ?? z_coord) - z_coord, feed_rate ?? 0);
            }
            else
                tw.WriteLine(rapid_move_code + (x_abs.HasValue ? " X{0:##0.####}" : "") + (y_abs.HasValue ? " Y{1:##0.####}" : "") + (z_abs.HasValue ? " Z{2:##0.####}" : "") + (feed_rate.HasValue ? " F{3}" : ""),
                                x_abs ?? x_coord, y_abs ?? y_coord, z_abs ?? z_coord, feed_rate ?? 0);
            x_coord = x_abs ?? x_coord;
            y_coord = y_abs ?? y_coord;
            z_coord = z_abs ?? z_coord;
        }
        public void drillMove(double x_abs, double y_abs, double? z_depth = null, double? z_lift = null, double? feed_rate = null, double? pause = null)
        {
            Scale(ref x_abs); Scale(ref y_abs); Scale(ref z_depth); Scale(ref z_lift);  Scale(ref feed_rate);
            drill_depth = z_depth ?? drill_depth;
            drill_lift = z_lift ?? drill_lift;
            drill_feed_rate = feed_rate ?? drill_feed_rate;
            drill_pause = pause ?? drill_pause;

            if (generate_relative_code)
                tw.WriteLine(drill_format, x_abs - x_coord, y_abs - y_coord, drill_depth, drill_lift, drill_feed_rate, drill_pause);
            else
                tw.WriteLine(drill_format, x_abs, y_abs, drill_depth, drill_lift, drill_feed_rate, drill_pause);
            x_coord = x_abs;
            y_coord = y_abs;
        }
        public void relativeMode(bool v)
        {
            generate_relative_code = v;
            if (v)
                tw.WriteLine("G91 (relative mode)");
            else
                tw.WriteLine("G90 (absolute mode)");
        }
        public void metricMode(bool v)
        {
            generate_metric = v;
            if (v)
            {
                output_scale = 1.0;
                tw.WriteLine("G21 (Metric mode)");
            }
            else
            {
                output_scale = 1 / 25.4;
                tw.WriteLine("G20 (Imperial mode)");
            }
        }
        static char[] delimiters = new char[] {'\r', '\n' };
        public void insertCode (string code)
        {
            var lines = code.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                tw.WriteLine(line);
            }
          
        }
    }
}