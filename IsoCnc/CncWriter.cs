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

namespace IsoCnc
{
    internal class CncWriter
    {
        Machine mchn; 
        public CncWriter() 
        {

        }
        public void Write(Geometry shape, bool ccw , TextWriter stream) 
        {
            mchn = new Machine(stream, false);
            mchn.setInputUnit(false);
            mchn.MetricMode(true);
            mchn.RelativeMode(false);
            for (int i = 0; i < shape.NumGeometries; i++) 
            {
                if (shape.GetGeometryN(i) is Polygon)
                {
                    var poly = shape.GetGeometryN(i) as Polygon;
                    {
                        bool first = true;
                        var ring = poly.ExteriorRing.Coordinates;
                        IEnumerable<Coordinate> coordinates = ring;
                        if (ccw ^ Orientation.IsCCW(ring))
                        {
                            coordinates = ring.Reverse();
                        }
                        foreach (Coordinate c in coordinates)
                        {
                            if (first)
                            {
                                mchn.rapid_move(c.X, c.Y);
                                first = false;
                                continue;
                            }
                            mchn.move(c.X, c.Y);
                        }
                    }
                    foreach (var r in poly.InteriorRings)
                    {
                        bool first = true;
                        var ring = poly.ExteriorRing.Coordinates;
                        IEnumerable<Coordinate> coordinates = ring;
                        if (ccw ^ Orientation.IsCCW(ring))
                        {
                            coordinates = ring.Reverse();
                        }
                        foreach (var c in r.Coordinates)
                        {
                            if (first)
                            {
                                mchn.rapid_move(c.X, c.Y);
                                first = false;
                                continue;
                            }
                            mchn.move(c.X, c.Y);
                        }
                    }
                }
                else
                {
                    Debug.Assert(false);
                }
            }
        }
    }
}
