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

namespace IsoCnc
{
    internal class CncWriter
    {
        Machine mchn;
        ConfigDictionary config;
        public CncWriter(ConfigDictionary config) 
        {
            this.config = config;
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
                    mchn.rapidMove(c.X, c.Y);
                    mchn.move(null, null, -0.07/25.4, 60/25.4); //plunge
                    mchn.move(null, null, null, 750 / 25.4); //set feed rate for cuts
                    continue;
                }
                mchn.move(c.X, c.Y);
            }
            mchn.rapidMove(null, null, 2 / 25.4); //lift Z
        }
        public void Write(Geometry shape, bool ccw , TextWriter stream) 
        {
            mchn = new Machine(stream, config);
            mchn.setInputUnit(false);
            mchn.metricMode(true);
            mchn.relativeMode(false);
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
        }
    }
}
