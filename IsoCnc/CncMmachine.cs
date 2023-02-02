using NetTopologySuite.Noding;
using System.IO;

namespace Cnc
{
    public class Machine
    {
        const string rapid_move_code = "G0";
        const string Linear_move_code = "G1";
        double x_coord = 0;
        double y_coord = 0;
        double z_coord = 0;
        double feed_rate = 100;
        bool generate_relative_code = false;
        bool generate_metric = true;
        bool input_unit_metric = false;
        bool transpose_xy = false;
        double input_scale = 25.4;
        double output_scale = 1.0;
        TextWriter tw;
        public Machine(TextWriter tw, bool transpose_xy = false)
        {
            this.tw = tw;
            this.transpose_xy = transpose_xy;
        }
        public void setInputUnit(bool metric)
        {
            input_unit_metric = metric;
            input_scale = metric ? 1.0 : 25.4;
        }
        public void setFeedRate(double rate)
        {
            feed_rate= rate;
        }
        private void Scale (ref double? scalar)
        {   if (scalar.HasValue)
                scalar = scalar * input_scale * output_scale;
        }
        public void move(double? x_abs = null, double? y_abs = null, double? z_abs = null, double? feed_rate = null)
        {
            Scale(ref x_abs); Scale(ref y_abs); Scale(ref z_abs); Scale(ref feed_rate);
            if (transpose_xy)
            {
                var tmp = y_abs;
                y_abs = x_abs;
                x_abs = tmp;
            }
            if (generate_relative_code)
            {
                tw.WriteLine(Linear_move_code + (x_abs.HasValue ? " X{0:##0.####}" : "") + (y_abs.HasValue ? " Y{1:##0.####}" : "") + (z_abs.HasValue ? " Z{2:##0.####}" : "") + (feed_rate.HasValue ? " F{3}" : ""),
                                (x_abs ?? x_coord) - x_coord, (y_abs ?? y_coord) - y_coord, (z_abs ?? z_coord) - z_coord, feed_rate ?? 0);
            }
            else
                tw.WriteLine(Linear_move_code + (x_abs.HasValue ? " X{0:##0.####}" : "") + (y_abs.HasValue ? " Y{1:##0.####}" : "") + (z_abs.HasValue ? " Z{2:##0.####}" : "") + (feed_rate.HasValue ? " F{3}" : ""),
                                x_abs ?? x_coord, y_abs ?? y_coord, z_abs ?? z_coord, feed_rate ?? 0);
            x_coord = x_abs ?? x_coord;
            y_coord = y_abs ?? y_coord;
            z_coord = z_abs ?? z_coord;
        }
        public void rapid_move(double? x_abs = null, double? y_abs = null, double? z_abs = null, double? feed_rate = null)
        {
            Scale(ref x_abs); Scale(ref y_abs); Scale(ref z_abs); Scale(ref feed_rate);
            if (transpose_xy)
            {
                var tmp = y_abs;
                y_abs = x_abs;
                x_abs = tmp;
            }
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
        public void RelativeMode(bool v)
        {
            generate_relative_code = v;
            if (v)
                tw.WriteLine("G91 (relative mode)");
            else
                tw.WriteLine("G90 (absolute mode)");
        }
        public void MetricMode(bool v)
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
        public void Comment (string comment)
        {
            var lines = comment.Split(delimiters);
            foreach (var line in lines)
            {
                tw.WriteLine("( " + line + " )");
            }
          
        }
    }
}