// GerberShapes.cs - Handles rendering of gerber images.
// 

/*  Copyright (C) 2015-2021 Milton Neal <milton200954@gmail.com>
 *  Copyright (C) 2022-2023 Patrick H Dussud <phdussud@hotmail.com>
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
using System.Collections.ObjectModel;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using System.Diagnostics;
using NetTopologySuite.Geometries.Utilities;
using GerberVS;
using System.Net;
//using System.Drawing;
using System.Reflection;
using NetTopologySuite.Index.HPRtree;
using System.IO;
using NetTopologySuite.Operation.Overlay;
using System.Linq.Expressions;
using NetTopologySuite.Algorithm;
using System.Runtime.DesignerServices;

namespace IsoCnc
{
    /// <summary>
    /// Render the gerber image to a Topology Geometry object.
    /// https://nettopologysuite.github.io/NetTopologySuite/api/NetTopologySuite.html
    /// </summary>
    public static class GerberShapes
    {
        static Stack<AffineTransformation> transform_stack = new Stack<AffineTransformation>();
        static GeometryFactoryEx factory;
        
        // Renders a gerber image to the specified graphics target.
        public static Geometry CreateGeometry(GerberImage gerberImage, bool ccw_orientation = true, double[]mirror_line = null)
        {
            NetTopologySuite.NtsGeometryServices.Instance = new NetTopologySuite.NtsGeometryServices(
                NetTopologySuite.Geometries.Implementation.CoordinateArraySequenceFactory.Instance,
                new NetTopologySuite.Geometries.PrecisionModel(),
                4326, NetTopologySuite.Geometries.GeometryOverlay.NG,
                new NetTopologySuite.Geometries.CoordinateEqualityComparer());
            //Collection<Polygon> result = new Collection<Polygon>();
            factory = new GeometryFactoryEx();
            factory.OrientationOfExteriorRing = ccw_orientation ? LinearRingOrientation.CCW : LinearRingOrientation.CW;
            AffineTransformation currentTransformation = new AffineTransformation();

            double dx, dy;
            double startX, startY, stopX, stopY;
            double p0, p1, p2, p3, p4;
            int repeatX = 1, repeatY = 1;
            double repeatDistanceX = 0.0f, repeatDistanceY = 0.0f;
            bool invertPolarity = false;
            int netListIndex = 0;
            Collection<SimplifiedApertureMacro> simplifiedMacroList;
            Collection<GerberNet> gerberNetList;
            Aperture[] apertures = gerberImage.ApertureArray();
            Coordinate startPoint, endPoint;
            GerberNet currentNet = null;
            GerberLevel oldLevel = null;
            GerberNetState oldNetState = null;
            //Collection<Polygon>[] polygons = { new Collection<Polygon>(), new Collection<Polygon>() };
            Geometry surface = factory.CreateEmpty(Dimension.Surface);
            bool exposure = true; //false for Clear, true for Dark

            // Apply initial image transformations.
            currentTransformation.Translate(gerberImage.ImageInfo.ImageJustifyOffsetActualA, gerberImage.ImageInfo.ImageJustifyOffsetActualB);
            currentTransformation.Translate(gerberImage.ImageInfo.OffsetA, gerberImage.ImageInfo.OffsetB);
            currentTransformation.Rotate(gerberImage.ImageInfo.ImageRotation * Math.PI / 180);

            if (gerberImage.ImageInfo.Polarity == GerberPolarity.Negative)
                invertPolarity = !invertPolarity;

            gerberNetList = gerberImage.GerberNetList;

            AffineTransformation initialTransformation = new AffineTransformation(currentTransformation);

            for (netListIndex = 0; netListIndex < gerberNetList.Count; GetNextRenderObject(gerberNetList, ref netListIndex))
            {
                //pen.Alignment = PenAlignment.Center;
                currentNet = gerberNetList[netListIndex];
                if (currentNet.Level != oldLevel)
                {
                    // Set the current level polarity.
                    if (currentNet.Level.Polarity == GerberPolarity.Clear ^ invertPolarity)
                        exposure = false;
                    else
                        exposure = true;

                    // Check for changes to step and repeat.
                    repeatX = currentNet.Level.StepAndRepeat.X;
                    repeatY = currentNet.Level.StepAndRepeat.Y;
                    repeatDistanceX = (double)currentNet.Level.StepAndRepeat.DistanceX;
                    repeatDistanceY = (double)currentNet.Level.StepAndRepeat.DistanceY;

                    // Draw any knockout areas.
                    if (currentNet.Level.Knockout.FirstInstance == true)
                    {
                        currentTransformation = new AffineTransformation(initialTransformation);

                        Coordinate pf1 = new Coordinate((double)(currentNet.Level.Knockout.LowerLeftX - currentNet.Level.Knockout.Border),
                                          (double)(currentNet.Level.Knockout.LowerLeftY - currentNet.Level.Knockout.Border));
                        Coordinate pf2 = new Coordinate(pf1.X + (double)(currentNet.Level.Knockout.Width + (currentNet.Level.Knockout.Border * 2)), pf1.Y);
                        Coordinate pf3 = new Coordinate(pf2.X, pf1.Y + (double)(currentNet.Level.Knockout.Height + (currentNet.Level.Knockout.Border * 2)));
                        Coordinate pf4 = new Coordinate(pf1.X, pf3.Y);

                        Coordinate[] points = new Coordinate[] { pf1, pf2, pf3, pf4, pf1 };
                        ExposeGeometry(ref surface, factory.CreatePolygon((LinearRing)currentTransformation.Transform(factory.CreateLinearRing(points))), currentNet.Level.Knockout.Polarity != GerberPolarity.Clear);
                        //polygons[currentNet.Level.Knockout.Polarity == GerberPolarity.Clear ? 0:1].Add(factory.CreatePolygon((LinearRing)currentTransformation.Transform(factory.CreateLinearRing(points))));
                    }

                    oldLevel = currentNet.Level;
                }

                // Check if this is a new netstate.
                if (currentNet.NetState != oldNetState)
                {
                    currentTransformation = new AffineTransformation(initialTransformation);
                    ApplyNetStateTransformation(currentTransformation, currentNet.NetState);
                    oldNetState = currentNet.NetState;
                }



                for (int rx = 0; rx < repeatX; rx++)
                {
                    for (int ry = 0; ry < repeatY; ry++)
                    {
                        double stepAndRepeatX = rx * repeatDistanceX;
                        double stepAndRepeatY = ry * repeatDistanceY;

                        startX = (double)currentNet.StartX + stepAndRepeatX;
                        startY = (double)currentNet.StartY + stepAndRepeatY;
                        stopX = (double)currentNet.EndX + stepAndRepeatX;
                        stopY = (double)currentNet.EndY + stepAndRepeatY;

                        switch (gerberNetList[netListIndex].Interpolation)
                        {
                            case GerberInterpolation.RegionStart:
                                {
                                    var coords = FillRegionPath(currentTransformation, gerberNetList, netListIndex, stepAndRepeatX, stepAndRepeatY);
                                    if (coords[1].Count > 3)
                                        ExposeGeometry(ref surface, factory.CreatePolygon((LinearRing)currentTransformation.Transform(factory.CreateLinearRing(coords[1].ToArray())), null), exposure);
                                        //polygons[exposure].Add(factory.CreatePolygon((LinearRing)currentTransformation.Transform(factory.CreateLinearRing(coords[1].ToArray())), null));
                                    if (coords[0].Count > 3)
                                        ExposeGeometry(ref surface, factory.CreatePolygon((LinearRing)currentTransformation.Transform(factory.CreateLinearRing(coords[0].ToArray())), null), !exposure);
                                }

                                continue;

                            case GerberInterpolation.Deleted:
                                continue;
                        }

                        if (apertures[currentNet.Aperture] != null)
                        {
                            switch (currentNet.ApertureState)
                            {
                                case GerberApertureState.On:
                                    switch (currentNet.Interpolation)
                                    {
                                        case GerberInterpolation.Linear:
                                            //case GerberInterpolation.DrillSlot:
                                            switch (apertures[currentNet.Aperture].ApertureType)
                                            {
                                                case GerberApertureType.Circle:
                                                    {
                                                        var lWidth = (double)apertures[currentNet.Aperture].Parameters()[0];
                                                        startPoint = new Coordinate(startX, startY);
                                                        endPoint = new Coordinate(stopX, stopY);
                                                        var line = currentTransformation.Transform(factory.CreateLineString(new Coordinate[] { startPoint, endPoint }));
                                                        ExposeGeometry(ref surface, line.Buffer(lWidth / 2.0), exposure);
                                                        //polygons[exposure].Add ((Polygon)line.Buffer(lWidth / 2.0));
                                                    }
                                                    break;

                                                case GerberApertureType.Rectangle:
                                                    {
                                                        dx = (double)(apertures[currentNet.Aperture].Parameters()[0] / 2);
                                                        dy = (double)(apertures[currentNet.Aperture].Parameters()[1] / 2);
                                                        if (startX > stopX)
                                                            dx = -dx;

                                                        if (startY > stopY)
                                                            dy = -dy;

                                                        var points = new Coordinate[]{
                                                        new Coordinate(startX - dx, startY - dy),
                                                        new Coordinate(startX - dx, startY + dy),
                                                        new Coordinate(stopX - dx, stopY + dy),
                                                        new Coordinate(stopX + dx, stopY + dy),
                                                        new Coordinate(stopX + dx, stopY - dy),
                                                        new Coordinate(startX + dx, startY - dy),
                                                        new Coordinate(startX - dx, startY - dy)
                                                        };
                                                        ExposeGeometry(ref surface, factory.CreatePolygon((LinearRing)currentTransformation.Transform(factory.CreateLinearRing(points))), exposure);
                                                        //polygons[exposure].Add(factory.CreatePolygon((LinearRing)currentTransformation.Transform(factory.CreateLinearRing(points))));
                                                    }

                                                    break;

                                                // For now, just render ovals and polygons like a circle.
                                                case GerberApertureType.Oval:
                                                case GerberApertureType.Polygon:
                                                    {
                                                        var lWidth = (double)apertures[currentNet.Aperture].Parameters()[0];
                                                        startPoint = new Coordinate(startX, startY);
                                                        endPoint = new Coordinate(stopX, stopY);
                                                        var line = currentTransformation.Transform(factory.CreateLineString(new Coordinate[] { startPoint, endPoint }));
                                                        ExposeGeometry(ref surface, line.Buffer(lWidth / 2.0), exposure);
                                                        //polygons[exposure].Add((Polygon)line.Buffer(lWidth / 2.0));
                                                    }
                                                    break;

                                                // Macros can only be flashed, so ignore any that might be here.
                                                default:
                                                    break;
                                            }
                                            break;

                                        case GerberInterpolation.ClockwiseCircular:
                                        case GerberInterpolation.CounterclockwiseCircular:
                                            double centreX = (double)currentNet.CircleSegment.CenterX;
                                            double centreY = (double)currentNet.CircleSegment.CenterY;
                                            double width = (double)currentNet.CircleSegment.Width;
                                            double height = (double)currentNet.CircleSegment.Height;
                                            double startAngle = (double)currentNet.CircleSegment.StartAngle;
                                            double sweepAngle = (double)currentNet.CircleSegment.SweepAngle;
                                            Debug.Assert(Math.Abs(width - height) < 0.001);
                                            var endcap = NetTopologySuite.Operation.Buffer.EndCapStyle.Round;
                                            if (apertures[currentNet.Aperture].ApertureType == GerberApertureType.Rectangle)
                                                endcap = NetTopologySuite.Operation.Buffer.EndCapStyle.Flat;


                                            //RectangleF arcRectangle = new RectangleF(centreX - (width / 2), centreY - (height / 2), width, height);
                                            var line_width = (double)apertures[currentNet.Aperture].Parameters()[0];
                                            //pen.Alignment = PenAlignment.Inset;
                                            if (sweepAngle != 0.0 && width != 0.0)
                                            {
                                                var arc = CreateArc(currentTransformation, new Coordinate(centreX, centreY), width - line_width/2, startAngle, sweepAngle);
                                                ExposeGeometry(ref surface, factory.CreateLineString(arc).Buffer(line_width / 2, endcap), exposure);
                                                //polygons[exposure].Add((Polygon)factory.CreateLineString(arc).Buffer(line_width/2, endcap));
                                            }

                                            break;

                                        default:
                                            break;
                                    }
                                    break;


                                case GerberApertureState.Flash:
                                    p0 = (double)apertures[currentNet.Aperture].Parameters()[0];
                                    p1 = (double)apertures[currentNet.Aperture].Parameters()[1];
                                    p2 = (double)apertures[currentNet.Aperture].Parameters()[2];
                                    p3 = (double)apertures[currentNet.Aperture].Parameters()[3];
                                    p4 = (double)apertures[currentNet.Aperture].Parameters()[4];
                                    AffineTransformation state = currentTransformation;
                                    currentTransformation = new AffineTransformation(currentTransformation);
                                    currentTransformation.Translate(stopX, stopY);
                                    //using (GraphicsPath path = new GraphicsPath())
                                    {
                                        switch (apertures[currentNet.Aperture].ApertureType)
                                        {
                                            case GerberApertureType.Circle:
                                                {
                                                    //apertureRectangle = new RectangleF(-(p0 / 2), -(p0 / 2), p0, p0);
                                                    var hole = DrawAperatureHole(currentTransformation, p1, p2);
                                                    var circ = CreateCircle(currentTransformation, new Coordinate(0, 0), p0, hole == null ? null : new LinearRing[] { hole });
                                                    ExposeGeometry(ref surface, circ, exposure);
                                                    //polygons[exposure].Add(circ);
                                                }
                                                break;

                                            case GerberApertureType.Rectangle:
                                                {
                                                    //apertureRectangle = new RectangleF(-(p0 / 2), -(p1 / 2), p0, p1);
                                                    var hole = DrawAperatureHole(currentTransformation, p2, p3);
                                                    var coordinates = new Coordinate[] { new Coordinate(-(p0 / 2), -(p1 / 2)),
                                                                                        new Coordinate(-(p0 / 2), (p1 / 2)),
                                                                                        new Coordinate((p0 / 2), (p1 / 2)),
                                                                                        new Coordinate((p0 / 2), -(p1 / 2)),
                                                                                        new Coordinate(-(p0 / 2), -(p1 / 2))
                                                                                        };
                                                    var rec = factory.CreatePolygon((LinearRing)currentTransformation.Transform(factory.CreateLinearRing(coordinates)), hole == null ? null : new LinearRing[] { hole });
                                                    ExposeGeometry(ref surface, rec, exposure);
                                                    //polygons[exposure].Add(rec);
                                                    //path.AddRectangle(apertureRectangle);
                                                    //DrawAperatureHole(path, p2, p3);
                                                }
                                                break;

                                            case GerberApertureType.Oval:
                                                {
                                                    //apertureRectangle = new RectangleF(-(p0 / 2), -(p1 / 2), p0, p1);
                                                    var hole = DrawAperatureHole(currentTransformation, p2, p3);
                                                    var ob = CreateOblong(currentTransformation, p0, p1, hole == null ? null : new LinearRing[] { hole });
                                                    ExposeGeometry(ref surface, ob, exposure);
                                                    //polygons[exposure].Add(ob);
                                                    //DrawAperatureHole(path, p2, p3);
                                                }
                                                break;

                                            case GerberApertureType.Polygon:
                                                {
                                                    var hole = DrawAperatureHole(currentTransformation, p3, p4);
                                                    var pol = CreatePolygon(currentTransformation, p0, p1, p2, hole == null ? null : new LinearRing[] { hole });
                                                    ExposeGeometry(ref surface, pol,exposure);
                                                    //polygons[exposure].Add(pol);
                                                    //DrawAperatureHole(path, p3, p4);
                                                }
                                                break;

                                            case GerberApertureType.Macro:
                                                {
                                                    simplifiedMacroList = apertures[currentNet.Aperture].SimplifiedMacroList;
                                                    var geo = DrawApertureMacro(currentTransformation, simplifiedMacroList, out bool success);
                                                    ExposeGeometry(ref surface, geo, exposure);
                                                    //for (int i = 0; i < geo.NumGeometries; i++)
                                                    //{
                                                    //    polygons[exposure].Add ((Polygon)geo.GetGeometryN(i));
                                                    //}
                                                }
                                                break;

                                            default:
                                                break;
                                        }

                                    }

                                    currentTransformation = state;
                                    break;

                                case GerberApertureState.Deleted:
                                    continue;

                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            if (mirror_line != null)
            {
                currentTransformation = new AffineTransformation();
                currentTransformation.SetToReflection(mirror_line[0], mirror_line[1], mirror_line[2], mirror_line[3]);
                surface = currentTransformation.Transform(surface);
            }
            return surface;
        }

        private static Geometry DrawApertureMacro(AffineTransformation transform, Collection<SimplifiedApertureMacro> simplifiedApertureList, out bool success)
        {
            //Debug.WriteLine("Drawing simplified Aperture macros:");
            success = true;                // Sucessfully processed macro flag.
            transform = new AffineTransformation(transform);
            Coordinate[] points = null;
            double rotation = 0.0;
            bool exposure = false;
            double diameter = 0.0;
            double centreX, centreY = 0.0;
            //List<Polygon>[] polygons = { new List<Polygon>(), new List<Polygon>()};
            var surface = factory.CreateEmpty(Dimension.A);
            foreach (SimplifiedApertureMacro simplifiedAperture in simplifiedApertureList)
            {
                if (simplifiedAperture.ApertureType == GerberApertureType.MacroCircle)
                {
                    exposure = simplifiedAperture.Parameters[(int)CircleParameters.Exposure] > 0.5;
                    centreX = (double)simplifiedAperture.Parameters[(int)CircleParameters.CentreX];
                    centreY = (double)simplifiedAperture.Parameters[(int)CircleParameters.CentreY];
                    diameter = (double)simplifiedAperture.Parameters[(int)CircleParameters.Diameter];
                    var circ = CreateCircle(transform, new Coordinate(centreX, centreY), diameter, null);
                    //polygons[exposure].Add(circ);
                    ExposeGeometry(ref surface, circ, exposure);
                  
                }

                else if (simplifiedAperture.ApertureType == GerberApertureType.MacroMoire)
                {
                    double gapWidth = (double)simplifiedAperture.Parameters[(int)MoireParameters.GapWidth];
                    double crossHairLength = (double)(simplifiedAperture.Parameters[(int)MoireParameters.CrosshairLength]);
                    double circleLineWidth = (double)simplifiedAperture.Parameters[(int)MoireParameters.CircleLineWidth];
                    int numberOfCircles = (int)simplifiedAperture.Parameters[(int)MoireParameters.NumberOfCircles];
                    rotation = (double)simplifiedAperture.Parameters[(int)MoireParameters.Rotation];
                    centreX = (double)simplifiedAperture.Parameters[(int)MoireParameters.CentreX];
                    centreY = (double)simplifiedAperture.Parameters[(int)MoireParameters.CentreY];
                    diameter = (double)simplifiedAperture.Parameters[(int)MoireParameters.OutsideDiameter];

                    // Get crosshair points.
                    points = new Coordinate[] { new Coordinate(centreX - (crossHairLength / 2), centreY),
                                            new Coordinate(centreX + (crossHairLength / 2), centreY),
                                            new Coordinate(centreX, centreY - (crossHairLength / 2)),
                                            new Coordinate(centreX, centreY + (crossHairLength / 2)) };

                    TransformPoints(points, rotation);

                    // Draw target.
                    var width = circleLineWidth;
                    for (int i = 0; i < numberOfCircles; i++)
                    {
                        double targetSize = diameter - (gapWidth + circleLineWidth/2) * 2 * i;
                        if (targetSize > 0)
                        {
                            //polygons[1].Add((Polygon)CreateCircleLine(transform, new Coordinate(centreX, centreY), targetSize).Buffer(circleLineWidth/2));
                            ExposeGeometry(ref surface, CreateCircleLine(transform, new Coordinate(centreX, centreY), targetSize).Buffer(circleLineWidth / 2));
                        }
                    }

                    // Draw crosshairs.
                    var thickness = simplifiedAperture.Parameters[(int)MoireParameters.CrosshairLineWidth];
                    var cross = transform.Transform(factory.CreateLineString(new Coordinate[] { points[0], points[1] })).Union(transform.Transform(factory.CreateLineString(new Coordinate[] { points[2], points[3] })));
                    //polygons[1].Add((Polygon)cross.Buffer(thickness / 2));
                    ExposeGeometry(ref surface, cross.Buffer(thickness / 2));
                }

                else if (simplifiedAperture.ApertureType == GerberApertureType.MacroThermal)
                {
                    double innerDiameter = (double)simplifiedAperture.Parameters[(int)ThermalParameters.InsideDiameter];
                    double outerDiameter = (double)simplifiedAperture.Parameters[(int)ThermalParameters.OutsideDiameter];
                    double crossHairWidth = (double)simplifiedAperture.Parameters[(int)ThermalParameters.CrosshairLineWidth];
                    double gap = outerDiameter - innerDiameter;
                    rotation = (double)simplifiedAperture.Parameters[(int)ThermalParameters.Rotation];
                    centreX = (double)simplifiedAperture.Parameters[(int)ThermalParameters.CentreX];
                    centreY = (double)simplifiedAperture.Parameters[(int)ThermalParameters.CentreY];

                    // Draw the pad.
                    var pad = CreateCircle(transform, new Coordinate(centreX, centreY), outerDiameter).Difference(CreateCircle(transform, new Coordinate(centreX, centreY), innerDiameter));

                    // Draw thermal relief through the pad area.
                    points = new Coordinate[] { new Coordinate(centreX - outerDiameter, centreY),
                                            new Coordinate(centreX + outerDiameter, centreY),
                                            new Coordinate(centreX, centreY - outerDiameter),
                                            new Coordinate(centreX, centreY + outerDiameter) };

                    TransformPoints(points, rotation);

                    var cross = transform.Transform (factory.CreateLineString(new Coordinate[] { points[0], points[1] }).Union(factory.CreateLineString(new Coordinate[] { points[2], points[3] }))).Buffer(crossHairWidth / 2);
                    Geometry thermal = pad.Difference(cross);
                    ExposeGeometry(ref surface, thermal);
                    //for (int i = 0; i < thermal.NumGeometries; i++)
                    //{
                    //    polygons[1].Add((Polygon)thermal.GetGeometryN(i));
                    //}
                }

                else if (simplifiedAperture.ApertureType == GerberApertureType.MacroOutline)
                {
                    int numberOfPoints = (int)simplifiedAperture.Parameters[(int)OutlineParameters.NumberOfPoints];
                    int outlineFirstX = (int)OutlineParameters.FirstX;
                    int outlineFirstY = (int)OutlineParameters.FirstY;
                    exposure = simplifiedAperture.Parameters[(int)OutlineParameters.Exposure] > 0.5;
                    rotation = (double)simplifiedAperture.Parameters[(numberOfPoints * 2) + (int)OutlineParameters.Rotation];

                    points = new Coordinate[numberOfPoints + 1];
                    for (int p = 0; p <= numberOfPoints; p++)
                    {
                        points[p] = new Coordinate((double)(simplifiedAperture.Parameters[(p * 2) + outlineFirstX]),
                                                   (double)(simplifiedAperture.Parameters[(p * 2) + outlineFirstY]));
                    }
                    TransformPoints(points, rotation);
                    ExposeGeometry(ref surface, factory.CreatePolygon((LinearRing)transform.Transform(factory.CreateLinearRing(points))), exposure);
                    //polygons[exposure].Add(factory.CreatePolygon((LinearRing)transform.Transform(factory.CreateLinearRing(points))));
                }

                else if (simplifiedAperture.ApertureType == GerberApertureType.MacroPolygon)
                {
                    int numberOfSides = (int)simplifiedAperture.Parameters[(int)PolygonParameters.NumberOfSides];
                    exposure = (int)simplifiedAperture.Parameters[(int)PolygonParameters.Exposure] > 0.5;
                    rotation = (double)simplifiedAperture.Parameters[(int)PolygonParameters.Rotation];
                    diameter = (double)simplifiedAperture.Parameters[(int)PolygonParameters.Diameter];
                    ExposeGeometry(ref surface, CreatePolygon(transform, diameter, numberOfSides, rotation), exposure);
                    //polygons[exposure].Add(CreatePolygon(transform, diameter, numberOfSides, rotation));
                }

                else if (simplifiedAperture.ApertureType == GerberApertureType.MacroLine20)
                {
                    double startX = (double)simplifiedAperture.Parameters[(int)Line20Parameters.StartX];
                    double startY = (double)simplifiedAperture.Parameters[(int)Line20Parameters.StartY];
                    double endX = (double)simplifiedAperture.Parameters[(int)Line20Parameters.EndX];
                    double endY = (double)simplifiedAperture.Parameters[(int)Line20Parameters.EndY];
                    double lineWidth = (double)simplifiedAperture.Parameters[(int)Line20Parameters.LineWidth];
                    exposure = (int)simplifiedAperture.Parameters[(int)Line20Parameters.Exposure] > 0.5;
                    rotation = (double)simplifiedAperture.Parameters[(int)Line20Parameters.Rotation];

                    points = new Coordinate[] { new Coordinate(startX, startY), new Coordinate(endX, endY) };
                    TransformPoints(points, rotation);
                    ExposeGeometry(ref surface, transform.Transform(factory.CreateLineString(points)).Buffer(lineWidth / 2, NetTopologySuite.Operation.Buffer.EndCapStyle.Flat), exposure);
                    //polygons[exposure].Add((Polygon)transform.Transform(factory.CreateLineString(points)).Buffer(lineWidth/2, NetTopologySuite.Operation.Buffer.EndCapStyle.Flat));
                }

                else if (simplifiedAperture.ApertureType == GerberApertureType.MacroLine21)
                {
                    double halfWidth = (double)(simplifiedAperture.Parameters[(int)Line21Parameters.LineWidth]) / 2.0f;
                    double halfHeight = (double)(simplifiedAperture.Parameters[(int)Line21Parameters.LineHeight]) / 2.0f;
                    exposure = (int)simplifiedAperture.Parameters[(int)Line21Parameters.Exposure] > 0.5;
                    centreX = (double)simplifiedAperture.Parameters[(int)Line21Parameters.CentreX];
                    centreY = (double)simplifiedAperture.Parameters[(int)Line21Parameters.CentreY];
                    rotation = (double)simplifiedAperture.Parameters[(int)Line21Parameters.Rotation];

                    points = new Coordinate[] { new Coordinate(centreX - halfWidth, centreY - halfHeight),
                                            new Coordinate(centreX + halfWidth, centreY - halfHeight),
                                            new Coordinate(centreX + halfWidth, centreY + halfHeight),
                                            new Coordinate(centreX - halfWidth, centreY + halfHeight),
                                            new Coordinate(centreX - halfWidth, centreY - halfHeight)};

                    TransformPoints(points, rotation);
                    ExposeGeometry(ref surface, factory.CreatePolygon((LinearRing)transform.Transform(factory.CreateLinearRing(points))), exposure);
                    //polygons[exposure].Add(factory.CreatePolygon((LinearRing)transform.Transform(factory.CreateLinearRing(points))));

                }

                else if (simplifiedAperture.ApertureType == GerberApertureType.MacroLine22)
                {
                    double width = (double)(simplifiedAperture.Parameters[(int)Line22Parameters.LineWidth]);
                    double height = (double)(simplifiedAperture.Parameters[(int)Line22Parameters.LineHeight]);
                    double lowerLeftX = (double)simplifiedAperture.Parameters[(int)Line22Parameters.LowerLeftX];
                    double lowerLeftY = (double)simplifiedAperture.Parameters[(int)Line22Parameters.LowerLeftY];
                    exposure = (int)simplifiedAperture.Parameters[(int)Line22Parameters.Exposure] > 0.5;
                    rotation = (double)simplifiedAperture.Parameters[(int)Line22Parameters.Rotation];

                    points = new Coordinate[] { new Coordinate(lowerLeftX, lowerLeftY),
                                            new Coordinate(width, lowerLeftY),
                                            new Coordinate(width, height),
                                            new Coordinate(lowerLeftX, height),
                                            new Coordinate(lowerLeftX, lowerLeftY)};

                    TransformPoints(points, rotation);
                    ExposeGeometry(ref surface, factory.CreatePolygon((LinearRing)transform.Transform(factory.CreateLinearRing(points))), exposure);
                    //polygons[exposure].Add(factory.CreatePolygon((LinearRing)transform.Transform(factory.CreateLinearRing(points))));

                }

                else
                    success = false;
            }
            return surface;
        }

        internal static Polygon CreateOblong(AffineTransformation transform, double width, double height, LinearRing[] hole = null)
        {
            double diameter;
            double left = -(width / 2);
            double top = -(height / 2);
            LinearRing polyExt = null;

            if (width > height)
            {
                // Returns a horizontal capsule. 
                diameter = height;
                double dx = (width - diameter) / 2;
                Coordinate[] points = new Coordinate[]
                {
                    new Coordinate(-dx, 0),
                    new Coordinate(dx, 0)
                };

                polyExt = factory.CreateLinearRing(factory.CreateLineString(points).Buffer(diameter / 2).Coordinates);
            }

            else if (width < height)
            {
                // Returns a vertical capsule. 
                diameter = width;
                double dy = (height - diameter) / 2;
                Coordinate[] points = new Coordinate[]
                {
                    new Coordinate(0, -dy),
                    new Coordinate(0, dy)
                };
                polyExt = factory.CreateLinearRing(factory.CreateLineString(points).Buffer(diameter / 2).Coordinates);
            }

            else
            {
                diameter = height;
                polyExt = factory.CreateLinearRing(factory.CreatePoint(new Coordinate(0,0)).Buffer(diameter / 2).Coordinates);
            }

            return factory.CreatePolygon((LinearRing)transform.Transform(polyExt), hole);
        }


        /// <summary>
        /// Creates a path for flashed polygons.
        /// </summary>
        /// <param name="transformation">transformation to use</param>
        /// <param name="diameter">polygon diameter</param>
        /// <param name="numberOfSides">numer of sides in the polygon</param>
        /// <param name="rotation">rotation to apply</param>
        internal static Polygon CreatePolygon(AffineTransformation transformation, double diameter, double numberOfSides, double rotation, LinearRing[] hole = null)
        {

            Coordinate[] points = new Coordinate[(int)numberOfSides+1];
            for (int i = 0; i < numberOfSides; i++)
            {
                double angle = (double)i / numberOfSides * Math.PI * 2.0;
                points[i] = new Coordinate((double)(Math.Cos(angle) * diameter / 2.0), (double)(Math.Sin(angle) * diameter / 2.0));
            }
            points[(int)numberOfSides] = points[0]; //close the line
            TransformPoints(points, rotation);
            return factory.CreatePolygon((LinearRing)transformation.Transform(factory.CreateLinearRing(points)), hole);
        }

        internal static Polygon CreateCircle(AffineTransformation transformation, Coordinate Center, double Diameter, LinearRing[] hole = null)
        {
            Coordinate[] points = new Coordinate[65];
            for (int i = 0; i < 64; i++)
            {
                points[i] = new Coordinate (Center.X + (Diameter / 2) * Math.Cos(Math.PI * i / 32), Center.Y + (Diameter / 2) * Math.Sin(Math.PI * i / 32));
            }
            points[64] = points[0]; //close the lineString
            return factory.CreatePolygon((LinearRing)transformation.Transform(factory.CreateLinearRing(points)), hole);
        }
        internal static LinearRing CreateCircleLine(AffineTransformation transformation, Coordinate Center, double Diameter)
        {
            Coordinate[] points = new Coordinate[65];
            for (int i = 0; i < 64; i++)
            {
                points[i] = new Coordinate(Center.X + (Diameter / 2) * Math.Cos(Math.PI * i / 32), Center.Y + (Diameter / 2) * Math.Sin(Math.PI * i / 32));
            }
            points[64] = points[0]; //close the LineString
            return (LinearRing)transformation.Transform(factory.CreateLinearRing(points));
        }

        internal static Polygon CreateEllipse(AffineTransformation transformation, Coordinate Center, double w, double h)
        {
            Coordinate[] points = new Coordinate[65];
            for (int i = 0; i < 64; i++)
            {
                double r = Math.Sqrt(Math.Pow(w * Math.Cos(Math.PI * i / 32), 2) + Math.Pow(h * Math.Sin(Math.PI * i / 32), 2));
                points[i] = new Coordinate(Center.X + r * Math.Cos(Math.PI * i / 32), Center.Y + r * Math.Sin(Math.PI * i / 32));
            }
            points[64] = points[0]; //close the LineString
            return factory.CreatePolygon((LinearRing)transformation.Transform(factory.CreateLinearRing(points)));
        }

        internal static Coordinate[] CreateArc(AffineTransformation transformation, Coordinate Center, double Diameter, double start_angle, double sweep_angle)
        {
            int n_points = (int)Math.Ceiling(Math.Abs((sweep_angle) * 64 / 360));
            if (n_points > 0)
            {
                Coordinate[] points = new Coordinate[n_points + 1];
                for (int i = 0; i <= n_points; i++)
                {
                    var angle = (Math.PI / 180) * (start_angle + i * sweep_angle / n_points);
                    points[i] = new Coordinate(Center.X + (Diameter / 2) * Math.Cos(angle), Center.Y + (Diameter / 2) * Math.Sin(angle));
                }
                return transformation.Transform(factory.CreateLineString(points)).Coordinates;
            }
            else 
                return new Coordinate[0];
        }

        // Creates a region path to fill from a series of connecting nets.
        internal static List<Coordinate>[] FillRegionPath(AffineTransformation transform, Collection<GerberNet> gerberNetList, int netListIndex, double srX, double srY)
        {
            double stopX, stopY, startX, startY;
            double cpX = 0.0f, cpY = 0.0f;
            double circleWidth = 0.0f, circleHeight = 0.0f;
            double startAngle = 0.0f, sweepAngle = 0.0f;
            bool done = false;
            GerberNet currentNet = null;
            List<Coordinate>[] result = new List<Coordinate>[] { new List<Coordinate>(),new List<Coordinate>()};

            while (netListIndex < gerberNetList.Count)
            {
                currentNet = gerberNetList[netListIndex++];

                // Translate for step and repeat.
                startX = (double)currentNet.StartX + srX;
                startY = (double)currentNet.StartY + srY;
                stopX = (double)currentNet.EndX + srX;
                stopY = (double)currentNet.EndY + srY;

                // Translate circular x,y data as well.
                if (currentNet.CircleSegment != null)
                {
                    cpX = (double)currentNet.CircleSegment.CenterX + srX;
                    cpY = (double)currentNet.CircleSegment.CenterY + srY;
                    circleWidth = (double)currentNet.CircleSegment.Width;
                    circleHeight = (double)currentNet.CircleSegment.Height;
                    startAngle = (double)currentNet.CircleSegment.StartAngle;
                    sweepAngle = (double)currentNet.CircleSegment.SweepAngle;
                    Debug.Assert(Math.Abs(circleHeight - circleWidth) < 0.001);
                }

                switch (currentNet.Interpolation)
                {
                    case GerberInterpolation.Linear:
                        if (currentNet.ApertureState == GerberApertureState.On)
                            result[1].AddRange(new Coordinate[] { new Coordinate(startX, startY), new Coordinate(stopX, stopY) });
                        else
                        {
                            result[0].AddRange(new Coordinate[] { new Coordinate(startX, startY), new Coordinate(stopX, stopY) });
                        }
                        break;

                    case GerberInterpolation.ClockwiseCircular:
                    case GerberInterpolation.CounterclockwiseCircular:
                        result[currentNet.ApertureState == GerberApertureState.On ? 1 : 0].AddRange (CreateArc(transform, new Coordinate(cpX, cpY), circleWidth, startAngle, sweepAngle));
                        break;

                    case GerberInterpolation.RegionEnd:
                        done = true;
                        break;
                }

                if (done)
                    break;
            }
            //result[1].Add(result[1][0]); //make it closed
            //result[0].Add(result[0][0]);
            return result;
        }

        // return a transformed aperture hole  
        private static LinearRing DrawAperatureHole(AffineTransformation transformation, double parameter1, double parameter2)
        {

            if (parameter1 == 0.0f)
                return null;

            if (parameter1 > 0.0f && parameter2 > 0.0f)
            {

                var coordinates = new Coordinate[] { new Coordinate(-(parameter1 / 2), -(parameter2 / 2)),
                                                     new Coordinate(-(parameter1 / 2), (parameter2 / 2)),
                                                     new Coordinate((parameter1 / 2), (parameter2 / 2)),
                                                   };
                return (LinearRing)transformation.Transform(new LinearRing(coordinates)); 
            }

            else
            {
                return (LinearRing)CreateCircle(transformation, new Coordinate(0, 0), parameter1).ExteriorRing;
            }
        }

        // Finds the next renderable object in the net list.
        private static void GetNextRenderObject(Collection<GerberNet> gerberNetList, ref int currentIndex)
        {
            GerberNet currentNet = gerberNetList[currentIndex];

            if (currentNet.Interpolation == GerberInterpolation.RegionStart)
            {
                // If it's a region, step to the next non-region net.
                for (; currentIndex < gerberNetList.Count; currentIndex++)
                {
                    currentNet = gerberNetList[currentIndex];
                    if (currentNet.Interpolation == GerberInterpolation.RegionEnd)
                        break;
                }

                currentIndex++;
                return;
            }

            currentIndex++;
        }

        private static AffineTransformation ApplyNetStateTransformation(AffineTransformation transform, GerberNetState netState)
        {
             // Apply scale factor.
            transform.Scale((double)netState.ScaleA, (double)netState.ScaleB);
            // Apply offset.
            transform.Translate((double)netState.OffsetA, (double)netState.OffsetB);

            // Apply axis select.
            if (netState.AxisSelect == GerberAxisSelect.SwapAB)
            {
                // Do this by rotating 270 degrees counterclockwise, then mirroring the Y axis.
                transform.Scale(1.0f, -1.0f);
                transform.Rotate(270 * Math.PI / 180);

            }
            // Apply mirror.
            switch (netState.MirrorState)
            {
                case GerberMirrorState.FlipA:
                    transform.Scale(-1.0f, 1.0f);
                    break;

                case GerberMirrorState.FlipB:
                    transform.Scale(1.0f, -1.0f);
                    break;

                case GerberMirrorState.FlipAB:
                    transform.Scale(-1.0f, -1.0f);
                    break;

                default:
                    break;
            }


            return transform;

        }

        private static void TransformPoints(Coordinate[] points, double rotation, Coordinate offset = null)
        {
            if (rotation == 0.0 && offset == null)
                return;

            AffineTransformation apertureMatrix = new AffineTransformation();
            {   if (offset != null)
                    apertureMatrix.Translate(offset.X, offset.Y);
                apertureMatrix.Rotate(rotation*Math.PI / 180);
                for (int i = 0; i < points.Length; i++)
                {
                    apertureMatrix.Transform(points[i], points[i]);
                }
            }
        }

        private static void ExposeGeometry ( ref Geometry surface, Geometry item, bool exposed = true)
        {
            if (exposed)
                surface = surface.Union(item);
            else
                surface = surface.Difference(item);  
        }
    }
}
