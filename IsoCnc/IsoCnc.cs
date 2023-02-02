using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GerberVS;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.IO.GML2;

namespace IsoCnc
{
    internal class IsoCnc
    {
        static bool mirrorX = false;
        static bool conventional = true;
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
            GerberImage parsedImage = null;

            if (Gerber.IsGerberRS427X(fullPathName))
                parsedImage = Gerber.ParseGerber(fullPathName);

            else if (Drill.IsDrillFile(fullPathName))
                parsedImage = Drill.ParseDrillFile(fullPathName);

            else
                throw new GerberFileException("Unknown file type: " + Path.GetFileName(fullPathName));
            double[] mirror_line =null;
            if (mirrorX)
                mirror_line = new double[4] { 0, 0, 0, 1 };
            var geometries = GerberShapes.CreateGeometry(parsedImage, conventional, mirror_line);
            GMLWriter writer = new GMLWriter();
            Stream file = new FileStream("foo.gml", FileMode.Create);
            writer.Write(geometries, file);
            file.Close();
            StreamWriter streamWriter= new StreamWriter("foo.nc", false);
            CncWriter cncWriter = new CncWriter();
            for (int i = 0;i < 4;i++)
            {
                var dist = ((0.189 / 2) + i * 0.8 * 0.189)/25.4;
                var path = geometries.Buffer(dist);
                cncWriter.Write(path, conventional, streamWriter);
            }
            streamWriter.Close();
        }
    }
}
