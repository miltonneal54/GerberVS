// GerberDraw.cs - Handles rendering of gerber and drill images.

/*  Copyright (C) 2015-2019 Milton Neal <milton200954@gmail.com>
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace GerberVS
{
    internal static class GerberDraw
    {
        public static void DrawImageToTarget(Graphics graphics, SelectionInformation selectionInfo, GerberUserTransform userTransform,
            Color foreGroundColor, Color backGroundColor)
        {
            bool invert = false;
            RenderToTarget(graphics, selectionInfo.SelectionImage, selectionInfo.SelectedNetList, userTransform, foreGroundColor, backGroundColor, invert);
        }


        public static void DrawImageToTarget(Graphics graphics, GerberFileInformation fileInfo, GerberUserTransform userTransform, 
            Color foreGroundColor, Color backGroundColor)
        {
            bool invert = fileInfo.Inverted;
            RenderToTarget(graphics, fileInfo.Image,fileInfo.Image.GerberNetList, userTransform, foreGroundColor, backGroundColor, invert);
        }

        // Renders a gerber image to the specified graphics target.
        private static void RenderToTarget(Graphics graphics, GerberImage gerberImage, Collection<GerberNet> gerberNetList, GerberUserTransform userTransform,
            Color foreGroundColor, Color backGroundColor, bool invert)
        {
            float dx, dy;
            float startX, startY, stopX, stopY;
            float p1, p2, p3, p4, p5;
            int repeatX = 1, repeatY = 1;
            float repeatDistanceX = 0.0f, repeatDistanceY = 0.0f;

            Collection<SimplifiedApertureMacro> simplifiedMacroList;
            PointF startPoint, endPoint;
            RectangleF apertureRectangle;

            int netListIndex = 0;
            GerberNet currentNet = null;
            GerberLevel oldLevel = null;
            GerberNetState oldState = null;
            bool useClearOperator = false;
            bool invertPolarity = false;

            SolidBrush brush = new SolidBrush(foreGroundColor);
            Pen pen = new Pen(foreGroundColor);

            // Apply user supplied transforations.
            double scaleX = userTransform.ScaleX;
            double scaleY = userTransform.ScaleY;
            if (userTransform.MirrorAroundX)
                scaleY *= -1;

            if (userTransform.MirrorAroundY)
                scaleX *= -1;

            graphics.TranslateTransform((float)userTransform.TranslateX, (float)userTransform.TranslateY);
            graphics.ScaleTransform((float)scaleX, (float)scaleY);
            graphics.RotateTransform((float)userTransform.Rotation);

            // Apply initial image transformations.
            graphics.TranslateTransform((float)gerberImage.ImageInfo.ImageJustifyOffsetActualA, (float)gerberImage.ImageInfo.ImageJustifyOffsetActualB);
            graphics.TranslateTransform((float)gerberImage.ImageInfo.OffsetA, (float)gerberImage.ImageInfo.OffsetB);
            graphics.RotateTransform((float)gerberImage.ImageInfo.ImageRotation);

            invertPolarity = invert;
            if (gerberImage.ImageInfo.Polarity == GerberPolarity.Negative)
                invertPolarity = !invertPolarity;

            if(invertPolarity)
                graphics.Clear(foreGroundColor);

            else
                pen.Color = brush.Color = foreGroundColor;

            for (netListIndex = 0; netListIndex < gerberNetList.Count; GetNextRenderObject(gerberNetList, ref netListIndex))
            {
                currentNet = gerberNetList[netListIndex];
                if (currentNet.Level != oldLevel)
                {
                    // Set the current net transformation and polarity.
                    graphics.RotateTransform((float)currentNet.Level.Rotation);
                    if (currentNet.Level.Polarity == GerberPolarity.Clear ^ invertPolarity)
                        pen.Color = brush.Color = backGroundColor;

                    else
                        pen.Color = brush.Color = foreGroundColor;

                    // Check for changes to step and repeat.
                    repeatX = currentNet.Level.StepAndRepeat.X;
                    repeatY = currentNet.Level.StepAndRepeat.Y;
                    repeatDistanceX = (float)currentNet.Level.StepAndRepeat.DistanceX;
                    repeatDistanceY = (float)currentNet.Level.StepAndRepeat.DistanceY;

                    // Draw any knockout areas.
                    if (currentNet.Level.Knockout.FirstInstance == true)
                    {
                        Color oldColor = foreGroundColor;
                        if (currentNet.Level.Knockout.Polarity == GerberPolarity.Clear)
                            pen.Color = brush.Color = backGroundColor;

                        else
                            pen.Color = brush.Color = foreGroundColor;

                        GraphicsPath knockoutPath = new GraphicsPath();
                        PointF pf1 = new PointF((float)(currentNet.Level.Knockout.LowerLeftX - currentNet.Level.Knockout.Border),
                                      (float)(currentNet.Level.Knockout.LowerLeftY - currentNet.Level.Knockout.Border));
                        PointF pf2 = new PointF(pf1.X + (float)(currentNet.Level.Knockout.Width + (currentNet.Level.Knockout.Border * 2)), pf1.Y);
                        PointF pf3 = new PointF(pf2.X, pf1.Y + (float)(currentNet.Level.Knockout.Height + (currentNet.Level.Knockout.Border * 2)));
                        PointF pf4 = new PointF(pf1.X, pf3.Y);

                        PointF[] points = new PointF[] { pf1, pf2, pf3, pf4 };
                        knockoutPath.AddLines(points);
                        knockoutPath.CloseFigure();
                        graphics.FillPath(brush, knockoutPath);

                        // Restore the polarity.
                        pen.Color = brush.Color = oldColor;
                    }

                    ApplyNetStateTransformation(graphics, currentNet.NetState);
                    oldLevel = currentNet.Level;
                }

                // Check if this is a new netstate.
                if (currentNet.NetState != oldState)
                {
                    // A new state, so recalculate the new transformation matrix.
                    ApplyNetStateTransformation(graphics, currentNet.NetState);
                    oldState = currentNet.NetState;
                }

                for (int rx = 0; rx < repeatX; rx++)
                {
                    for (int ry = 0; ry < repeatY; ry++)
                    {
                        float stepAndRepeatX = rx * repeatDistanceX;
                        float stepAndRepeatY = ry * repeatDistanceY;

                        startX = (float)currentNet.StartX + stepAndRepeatX;
                        startY = (float)currentNet.StartY + stepAndRepeatY;
                        stopX = (float)currentNet.StopX + stepAndRepeatX;
                        stopY = (float)currentNet.StopY + stepAndRepeatY;

                        switch (gerberNetList[netListIndex].Interpolation)
                        {
                            case GerberInterpolation.PolygonAreaStart:
                                FillPolygonArea(graphics, brush, gerberNetList, netListIndex, stepAndRepeatX, stepAndRepeatY);
                                continue;

                            case GerberInterpolation.Deleted:
                                continue;
                        }

                        switch (currentNet.ApertureState)
                        {
                            case GerberApertureState.On:
                                switch (currentNet.Interpolation)
                                {
                                    case GerberInterpolation.LinearX10:
                                    case GerberInterpolation.LinearX01:
                                    case GerberInterpolation.LinearX001:
                                    case GerberInterpolation.LinearX1:
                                        pen.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);
                                        switch (gerberImage.ApertureArray[currentNet.Aperture].ApertureType)
                                        {
                                            case GerberApertureType.Circle:
                                                pen.Width = (float)gerberImage.ApertureArray[currentNet.Aperture].Parameters[0];
                                                startPoint = new PointF(startX, startY);
                                                endPoint = new PointF(stopX, stopY);
                                                graphics.DrawLine(pen, startPoint, endPoint);
                                                break;

                                            case GerberApertureType.Rectangle:
                                                dx = (float)(gerberImage.ApertureArray[currentNet.Aperture].Parameters[0] / 2);
                                                dy = (float)(gerberImage.ApertureArray[currentNet.Aperture].Parameters[1] / 2);
                                                if (startX > stopX)
                                                    dx = -dx;

                                                if (startY > stopY)
                                                    dy = -dy;

                                                using (GraphicsPath path = new GraphicsPath())
                                                {
                                                    path.AddLine(startX - dx, startY - dy, startX - dx, startY + dy);
                                                    path.AddLine(startX - dx, startY + dy, stopX - dx, stopY + dy);
                                                    path.AddLine(stopX - dx, stopY + dy, stopX + dx, stopY + dy);
                                                    path.AddLine(stopX + dx, stopY + dy, stopX + dx, stopY - dy);
                                                    path.AddLine(stopX + dx, stopY - dy, startX + dx, startY - dy);
                                                    graphics.FillPath(brush, path);
                                                }
                                                break;

                                            // For now, just render ovals or polygons like a circle.
                                            case GerberApertureType.Oval:
                                            case GerberApertureType.Polygon:
                                                pen.Width = (float)gerberImage.ApertureArray[currentNet.Aperture].Parameters[0];
                                                startPoint = new PointF(startX, startY);
                                                endPoint = new PointF(stopX, stopY);
                                                graphics.DrawLine(pen, startPoint, endPoint);
                                                break;

                                            // Macros can only be flashed, so ignore any that might be here.
                                            default:
                                                break;
                                        }
                                        break;

                                    case GerberInterpolation.ClockwiseCircular:
                                    case GerberInterpolation.CounterClockwiseCircular:
                                        float centerX = (float)currentNet.CircleSegment.CenterX;
                                        float centerY = (float)currentNet.CircleSegment.CenterY;
                                        float width = (float)currentNet.CircleSegment.Width;
                                        float height = (float)currentNet.CircleSegment.Height;
                                        float startAngle = (float)currentNet.CircleSegment.StartAngle;
                                        float sweepAngle = (float)currentNet.CircleSegment.SweepAngle;
                                        if (gerberImage.ApertureArray[currentNet.Aperture].ApertureType == GerberApertureType.Rectangle)
                                            pen.SetLineCap(LineCap.Square, LineCap.Square, DashCap.Flat);

                                        else
                                            pen.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);

                                        RectangleF arcRectangle = new RectangleF(centerX - (width / 2), centerY - (height / 2), width, height);
                                        pen.Width = (float)gerberImage.ApertureArray[currentNet.Aperture].Parameters[0];
                                        if (arcRectangle != RectangleF.Empty)
                                            graphics.DrawArc(pen, arcRectangle, startAngle, sweepAngle);

                                        break;

                                    default:
                                        break;
                                }
                                break;

                            case GerberApertureState.Flash:
                                p1 = (float)gerberImage.ApertureArray[currentNet.Aperture].Parameters[0];
                                p2 = (float)gerberImage.ApertureArray[currentNet.Aperture].Parameters[1];
                                p3 = (float)gerberImage.ApertureArray[currentNet.Aperture].Parameters[2];
                                p4 = (float)gerberImage.ApertureArray[currentNet.Aperture].Parameters[3];
                                p5 = (float)gerberImage.ApertureArray[currentNet.Aperture].Parameters[4];

                                GraphicsState state = graphics.Save();
                                graphics.TranslateTransform(stopX, stopY);
                                using (GraphicsPath path = new GraphicsPath())
                                {
                                    switch (gerberImage.ApertureArray[currentNet.Aperture].ApertureType)
                                    {
                                        case GerberApertureType.Circle:
                                            apertureRectangle = new RectangleF(-(p1 / 2), -(p1 / 2), p1, p1);
                                            path.AddEllipse(apertureRectangle);
                                            DrawAperatureHole(path, p2, p3);
                                            break;

                                        case GerberApertureType.Rectangle:
                                            apertureRectangle = new RectangleF(-(p1 / 2), -(p2 / 2), p1, p2);
                                            path.AddRectangle(apertureRectangle);
                                            DrawAperatureHole(path, p3, p4);
                                            break;

                                        case GerberApertureType.Oval:
                                            apertureRectangle = new RectangleF(-(p1 / 2), -(p2 / 2), p1, p2);
                                            CreateOblongPath(path, p1, p2);
                                            DrawAperatureHole(path, p3, p4);
                                            break;

                                        case GerberApertureType.Polygon:
                                            CreatePolygon(graphics, path, p1, p2, p3);
                                            DrawAperatureHole(path, p4, p5);
                                            break;

                                        case GerberApertureType.Macro:
                                            simplifiedMacroList = gerberImage.ApertureArray[currentNet.Aperture].SimplifiedMacroList;
                                            useClearOperator = gerberImage.ApertureArray[currentNet.Aperture].Parameters[0] == 1.0 ? true : false;
                                            DrawApertureMacro(graphics, simplifiedMacroList, brush.Color, backGroundColor, useClearOperator);
                                            break;

                                        default:
                                            break;
                                    }

                                    graphics.FillPath(brush, path); // Fill the path.
                                    graphics.Restore(state);
                                }

                                break;

                            default:
                                break;
                        }
                    }
                }
            }

            pen.Dispose();
            brush.Dispose();
        }

        private static void FillPolygonArea(Graphics graphics, SolidBrush brush, Collection<GerberNet> gerberNetList, int netListIndex, float srX, float srY)
        {
            float stopX, stopY, startX, startY;
            float cpX = 0.0f, cpY = 0.0f;
            float circleWidth = 0.0f, circleHeight = 0.0f;
            float startAngle = 0.0f, sweepAngle = 0.0f;
            bool done = false;
            RectangleF arcRectangle = RectangleF.Empty;
            GerberNet currentNet = null;
            using (GraphicsPath path = new GraphicsPath())
            {
                while (netListIndex < gerberNetList.Count)
                {
                    currentNet = gerberNetList[netListIndex++];

                    // Translate for step and repeat.
                    startX = (float)currentNet.StartX + srX;
                    startY = (float)currentNet.StartY + srY;
                    stopX = (float)currentNet.StopX + srX;
                    stopY = (float)currentNet.StopY + srY;

                    // Translate circular x,y data as well.
                    if (currentNet.CircleSegment != null)
                    {
                        cpX = (float)currentNet.CircleSegment.CenterX + srX;
                        cpY = (float)currentNet.CircleSegment.CenterY + srY;
                        circleWidth = (float)currentNet.CircleSegment.Width;
                        circleHeight = (float)currentNet.CircleSegment.Height;
                        startAngle = (float)currentNet.CircleSegment.StartAngle;
                        sweepAngle = (float)currentNet.CircleSegment.SweepAngle;
                        arcRectangle = new RectangleF(cpX - circleWidth / 2, cpY - circleHeight / 2, circleWidth, circleHeight);
                    }

                    switch (currentNet.Interpolation)
                    {
                        case GerberInterpolation.LinearX10:
                        case GerberInterpolation.LinearX01:
                        case GerberInterpolation.LinearX001:
                        case GerberInterpolation.LinearX1:
                            if (currentNet.ApertureState == GerberApertureState.On)
                                path.AddLine(startX, startY, stopX, stopY);

                            break;

                        case GerberInterpolation.ClockwiseCircular:
                        case GerberInterpolation.CounterClockwiseCircular:
                            if (arcRectangle != RectangleF.Empty)
                                path.AddArc(arcRectangle, startAngle, sweepAngle);

                            break;

                        case GerberInterpolation.PolygonAreaEnd:
                            if (path.PointCount > 0)
                                graphics.FillPath(brush, path);

                            done = true;
                            break;
                    }

                    if (done)
                        break;
                }
            }
        }

        private static bool DrawApertureMacro(Graphics graphics, Collection<SimplifiedApertureMacro> simplifiedApertureList,
            Color layerColor, Color backColor, bool useClearOperator)
        {
            Color apertureColor = layerColor;
            bool success = true;                // Sucessfully processed macro flag.
            bool first = true;                  // Flag to indicates first simplified macro in the list.
            //Debug.WriteLine("Drawing simplified Aperture macros:");

            GraphicsState state = graphics.Save();
            using (GraphicsPath graphicsPath = new GraphicsPath())
            {
                foreach (SimplifiedApertureMacro simplifiedAperture in simplifiedApertureList)
                {
                    // This handles the exposure thing in the Aperture macro
                    // The exposure is always the first element on stack independent of Aperture macro.
                    if (simplifiedAperture.ApertureType == GerberApertureType.MacroCircle)
                    {
                        float centerX = (float)simplifiedAperture.Parameters[(int)GerberCircleParameters.CentreX];
                        float centerY = (float)simplifiedAperture.Parameters[(int)GerberCircleParameters.CentreY];
                        float diameter = (float)simplifiedAperture.Parameters[(int)GerberCircleParameters.Diameter];

                        RectangleF objectRectangle = new RectangleF(centerX - (diameter / 2.0f), centerY - (diameter / 2.0f), diameter, diameter);
                        graphics.TranslateTransform(centerX, centerY);
                        if (first)
                            UpdateMacroExposure(ref apertureColor, backColor, layerColor, (float)simplifiedAperture.Parameters[(int)GerberCircleParameters.Exposure]);

                        graphicsPath.AddEllipse(objectRectangle);
                    }

                    else if (simplifiedAperture.ApertureType == GerberApertureType.MacroOutline)
                    {
                        int numberOfPoints = (int)simplifiedAperture.Parameters[(int)GerberOutlineParameters.NumberOfPoints];
                        float outlineFirstX = (float)simplifiedAperture.Parameters[(int)GerberOutlineParameters.FirstX];
                        float outlineFirstY = (float)simplifiedAperture.Parameters[(int)GerberOutlineParameters.FirstY];

                        graphics.TranslateTransform(outlineFirstX, outlineFirstY);
                        graphics.RotateTransform((float)simplifiedAperture.Parameters[(numberOfPoints * 2) + (int)GerberOutlineParameters.Rotation]);
                        // Number of points parameter does not include the start point, so we add one to include the start point.
                        if (first)
                            UpdateMacroExposure(ref apertureColor, backColor, layerColor, (float)simplifiedAperture.Parameters[(int)GerberOutlineParameters.Exposure]);

                        numberOfPoints++;
                        for (int p = 0; p < numberOfPoints; p++)
                        {
                            PointF point1 = new PointF((float)(simplifiedAperture.Parameters[(p * 2) + (int)GerberOutlineParameters.FirstX]),
                                                       (float)(simplifiedAperture.Parameters[(p * 2) + (int)GerberOutlineParameters.FirstY]));
                            p++;
                            PointF point2 = new PointF((float)(simplifiedAperture.Parameters[(p * 2) + (int)GerberOutlineParameters.FirstX]),
                                                       (float)(simplifiedAperture.Parameters[(p * 2) + (int)GerberOutlineParameters.FirstY]));
                            graphicsPath.AddLine(point1, point2);
                        }
                    }

                    else if (simplifiedAperture.ApertureType == GerberApertureType.MacroPolygon)
                    {
                        graphics.TranslateTransform((float)simplifiedAperture.Parameters[(int)GerberPolygonParameters.CenterX],
                                                    (float)simplifiedAperture.Parameters[(int)GerberPolygonParameters.CenterY]);
                        if (first)
                            UpdateMacroExposure(ref apertureColor, backColor, layerColor, (float)simplifiedAperture.Parameters[(int)GerberPolygonParameters.Exposure]);

                        using (SolidBrush brush = new SolidBrush(apertureColor))
                        using (GraphicsPath path = new GraphicsPath())
                        {
                            CreatePolygon(graphics, path, (float)simplifiedAperture.Parameters[(int)GerberPolygonParameters.Diameter],
                                          (float)simplifiedAperture.Parameters[(int)GerberPolygonParameters.NumberOfPoints],
                                          (float)simplifiedAperture.Parameters[(int)GerberPolygonParameters.Rotation]);
                            graphics.FillPath(brush, path);
                        }
                    }

                    else if (simplifiedAperture.ApertureType == GerberApertureType.MacroMoire)
                    {
                        float centerX = (float)simplifiedAperture.Parameters[(int)GerberMoireParameters.CenterX];
                        float centerY = (float)simplifiedAperture.Parameters[(int)GerberMoireParameters.CenterY];

                        float diameter = (float)(simplifiedAperture.Parameters[(int)GerberMoireParameters.OutsideDiameter] -
                                   simplifiedAperture.Parameters[(int)GerberMoireParameters.CircleLineWidth]);
                        float diameterDifference = (float)(2 * (simplifiedAperture.Parameters[(int)GerberMoireParameters.GapWidth] +
                                                  simplifiedAperture.Parameters[(int)GerberMoireParameters.CircleLineWidth]));

                        graphics.TranslateTransform(centerX, centerY);
                        graphics.RotateTransform((float)simplifiedAperture.Parameters[(int)GerberMoireParameters.Rotation]);

                        using (Pen pen = new Pen(layerColor))
                        {
                            // Draw target.
                            pen.Width = (float)simplifiedAperture.Parameters[(int)GerberMoireParameters.CircleLineWidth];
                            for (int i = 0; i < (int)simplifiedAperture.Parameters[(int)GerberMoireParameters.NumberOfCircles]; i++)
                            {
                                float targetSize = diameter - (diameterDifference * i);
                                RectangleF targetRectangle = new RectangleF(centerX - (targetSize / 2), centerY - (targetSize / 2), targetSize, targetSize);
                                if (!targetRectangle.IsEmpty)
                                    graphics.DrawEllipse(pen, targetRectangle);
                            }

                            // Draw crosshairs.
                            pen.Width = (float)simplifiedAperture.Parameters[(int)GerberMoireParameters.CrosshairLineWidth];
                            float crosshairRadius = (float)((simplifiedAperture.Parameters[(int)GerberMoireParameters.CrosshairLength] / 2.0));
                            graphics.DrawLine(pen, centerX, centerY - crosshairRadius, centerX, centerY + crosshairRadius);
                            graphics.DrawLine(pen, centerX - crosshairRadius, centerY, centerX + crosshairRadius, centerY);
                        }
                    }

                    else if (simplifiedAperture.ApertureType == GerberApertureType.MacroThermal)
                    {
                        float centerX = (float)simplifiedAperture.Parameters[(int)GerberThermalParameters.CenterX];
                        float centerY = (float)simplifiedAperture.Parameters[(int)GerberThermalParameters.CenterY];
                        float innerDiameter = (float)simplifiedAperture.Parameters[(int)GerberThermalParameters.InsideDiameter];
                        float outerDiameter = (float)simplifiedAperture.Parameters[(int)GerberThermalParameters.OutsideDiameter];
                        float crossHairWidth = (float)simplifiedAperture.Parameters[(int)GerberThermalParameters.CrosshairLineWidth];
                        float rotation = (float)simplifiedAperture.Parameters[(int)GerberThermalParameters.Rotation];

                        double startAngle1 = Math.Atan(crossHairWidth / innerDiameter);
                        double endAngle1 = Math.PI / 2 - startAngle1;
                        double startAngle2 = Math.Atan(crossHairWidth / outerDiameter);
                        double endAngle2 = Math.PI / 2 - startAngle2;

                        // Convert radians to degrees.
                        startAngle1 *= (180 / Math.PI);
                        endAngle1 *= (180 / Math.PI);
                        startAngle2 *= (180 / Math.PI);
                        endAngle2 *= (180 / Math.PI);

                        RectangleF innerRectangle = new RectangleF(centerX - (innerDiameter / 2), centerY - (innerDiameter / 2),
                                                                   innerDiameter, innerDiameter);
                        RectangleF outerRectangle = new RectangleF(centerX - (outerDiameter / 2), centerY - (outerDiameter / 2),
                                                                   outerDiameter, outerDiameter);
                        graphics.TranslateTransform(centerX, centerY);
                        graphics.RotateTransform(rotation);

                        // Use a path to fill and render each segment of the thermal.
                        using (SolidBrush brush = new SolidBrush(layerColor))
                        using (GraphicsPath path = new GraphicsPath())
                        {
                            path.AddArc(innerRectangle, -(float)endAngle1, (float)(endAngle1 - startAngle1));
                            path.AddLine(centerX + (innerDiameter / 2), centerY - (crossHairWidth / 2),
                                         centerX + (outerDiameter / 2), centerY - (crossHairWidth / 2));
                            path.AddArc(outerRectangle, (float)startAngle2, -(float)(endAngle2 - startAngle2));
                            path.AddLine(centerX + (crossHairWidth / 2), centerY - (outerDiameter / 2),
                                         centerX + (crossHairWidth / 2), centerY - (innerDiameter / 2));
                            // Draw each quadrant, rotating each one 90 degrees as we go.
                            for (int i = 0; i < 4; i++)
                            {
                                graphics.RotateTransform(90.0f * i);
                                graphics.FillPath(brush, path);
                            }
                        }
                    }

                    else if (simplifiedAperture.ApertureType == GerberApertureType.MacroLine20)
                    {
                        UpdateMacroExposure(ref apertureColor, backColor, layerColor, (float)simplifiedAperture.Parameters[(int)GerberLine20Parameters.Exposure]);
                        using (Pen pen = new Pen(apertureColor))
                        {
                            pen.Width = (float)simplifiedAperture.Parameters[(int)GerberLine20Parameters.LineWidth];
                            pen.StartCap = LineCap.Square;
                            pen.EndCap = LineCap.Square;

                            graphics.RotateTransform((float)simplifiedAperture.Parameters[(int)GerberLine20Parameters.Rotation]);
                            graphics.DrawLine(pen, (float)simplifiedAperture.Parameters[(int)GerberLine20Parameters.StartX],
                                                   (float)simplifiedAperture.Parameters[(int)GerberLine20Parameters.StartY],
                                                   (float)simplifiedAperture.Parameters[(int)GerberLine20Parameters.EndX],
                                                   (float)simplifiedAperture.Parameters[(int)GerberLine20Parameters.EndY]);

                        }
                    }

                    else if (simplifiedAperture.ApertureType == GerberApertureType.MarcoLine21)
                    {
                        float width = (float)(simplifiedAperture.Parameters[(int)GerberLine21Parameters.LineWidth]);
                        float height = (float)(simplifiedAperture.Parameters[(int)GerberLine21Parameters.LineHeight]);
                        float halfWidth = width / 2.0f;
                        float halfHeight = height / 2.0f;
                        RectangleF rectangle = new RectangleF(-halfWidth, -halfHeight, width, height);

                        graphics.TranslateTransform((float)simplifiedAperture.Parameters[(int)GerberLine21Parameters.CenterX],
                                                    (float)simplifiedAperture.Parameters[(int)GerberLine21Parameters.CenterY]);

                        graphics.RotateTransform((float)simplifiedAperture.Parameters[(int)GerberLine21Parameters.Rotation]);
                        if (first)
                            UpdateMacroExposure(ref apertureColor, backColor, layerColor, (float)simplifiedAperture.Parameters[(int)GerberLine21Parameters.Exposure]);

                        PointF[] points = new PointF[] { new PointF(-halfWidth, -halfHeight),
                                                     new PointF(-halfWidth + width, -halfHeight),
                                                     new PointF(-halfWidth + width, halfHeight), new PointF(-halfWidth, halfHeight) };

                        graphicsPath.AddPolygon(points);
                    }

                    else if (simplifiedAperture.ApertureType == GerberApertureType.MacroLine22)
                    {
                        float width = (float)(simplifiedAperture.Parameters[(int)GerberLine22Parameters.LineWidth]);
                        float height = (float)(simplifiedAperture.Parameters[(int)GerberLine22Parameters.LineHeight]);
                        graphics.TranslateTransform((float)simplifiedAperture.Parameters[(int)GerberLine22Parameters.LowerLeftX],
                                                    (float)simplifiedAperture.Parameters[(int)GerberLine22Parameters.LowerLeftY]);
                        graphics.RotateTransform((float)simplifiedAperture.Parameters[(int)GerberLine22Parameters.Rotation]);
                        if (first)
                            UpdateMacroExposure(ref apertureColor, backColor, layerColor, (float)simplifiedAperture.Parameters[(int)GerberLine22Parameters.Exposure]);

                        PointF[] points = new PointF[] { new PointF(0.0f, 0.0f),
                                                     new PointF(width, 0.0f),
                                                     new PointF(width, height),
                                                     new PointF(0.0f, height) };
                        graphicsPath.AddPolygon(points);
                    }

                    else
                        success = false;

                    first = false;
                }

                if (graphicsPath.PointCount > 0)
                    graphics.FillPath(new SolidBrush(apertureColor), graphicsPath);
            }

            graphics.Restore(state);
            return success;
        }

        private static void CreatePolygon(Graphics graphics, GraphicsPath path, float outsideDiameter, float numberOfSides, float rotation)
        {
            // Skip first point, since we've moved there already.
            // Include last point, since we may be drawing an Aperture hole next
            // and cairo may not correctly close the path itself.
            PointF point1 = new PointF(outsideDiameter / 2.0f, 0.0f);
            PointF point2;

            graphics.RotateTransform(rotation);
            for (int i = 1; i <= (int)numberOfSides; i++)
            {
                double angle = (double)i / numberOfSides * Math.PI * 2.0;
                point2 = new PointF((float)(Math.Cos(angle) * outsideDiameter / 2.0), (float)(Math.Sin(angle) * outsideDiameter / 2.0));
                path.AddLine(point1, point2);
                point1 = point2;
            }
        }

        // Draw an aperture hole.
        private static void DrawAperatureHole(GraphicsPath path, float dimensionX, float dimensionY)
        {
            RectangleF holeRectangle = RectangleF.Empty;

            if (dimensionX > 0.0f)
            {
                if (dimensionY > 0.0f)
                {
                    holeRectangle = new RectangleF(-(dimensionX / 2), -(dimensionY / 2), dimensionX, dimensionY);
                    path.AddRectangle(holeRectangle);
                }

                else
                {
                    holeRectangle = new RectangleF(-(dimensionX / 2), -(dimensionX / 2), dimensionX, dimensionX);
                    path.AddEllipse(holeRectangle);
                }
            }

            return;
        }

        // Finds the next renderable object in the net list.
        private static void GetNextRenderObject(Collection<GerberNet> gerberNetList, ref int currentIndex)
        {
            GerberNet currentNet = gerberNetList[currentIndex];

            if (currentNet.Interpolation == GerberInterpolation.PolygonAreaStart)
            {
                // If it's a polygon, step to the next non-polygon net.
                for (; currentIndex < gerberNetList.Count; currentIndex++)
                {
                    currentNet = gerberNetList[currentIndex];
                    if (currentNet.Interpolation == GerberInterpolation.PolygonAreaEnd)
                        break;
                }

                currentIndex++;
                return;
            }

            currentIndex++;
        }

        private static void UpdateMacroExposure(ref Color apertureColor, Color backColor, Color layerColor, double exposureSetting)
        {
            if (exposureSetting == 0.0)
                apertureColor = backColor;

            else if (exposureSetting == 1.0)
                apertureColor = layerColor;

            else
            {
                if (apertureColor == backColor)
                    apertureColor = layerColor;

                else
                    apertureColor = backColor;
            }
        }

        private static void CreateOblongPath(GraphicsPath path, float param1, float param2)
        {
            float diameter;
            float left = -(param1 / 2);
            float top = -(param2 / 2);
            PointF location = new PointF(left, top);

            if (param1 > param2)
            {
                // Returns a horizontal capsule. 
                diameter = param2;
                SizeF sizeF = new SizeF(diameter, diameter);
                RectangleF arc = new RectangleF(location, sizeF);
                path.AddArc(arc, 90, 180);
                arc.X = (left + param1) - diameter;
                path.AddArc(arc, 270, 180);
            }

            else if (param1 < param2)
            {
                // Returns a vertical capsule. 
                diameter = param1;
                SizeF sizeF = new SizeF(diameter, diameter);
                RectangleF arc = new RectangleF(location, sizeF);
                path.AddArc(arc, 180, 180);
                arc.Y = (top + param2) - diameter;
                path.AddArc(arc, 0, 180);
            }

            else
                path.AddEllipse(left, top, param1, param2);

            path.CloseFigure();
        }

        private static void ApplyNetStateTransformation(Graphics graphics, GerberNetState netState)
        {
            // Apply scale factor.
            graphics.ScaleTransform((float)netState.ScaleA, (float)netState.ScaleB);
            // Apply offset.
            graphics.TranslateTransform((float)netState.OffsetA, (float)netState.OffsetB);
            // Apply mirror.
            switch (netState.MirrorState)
            {
                case GerberMirrorState.FlipA:
                    graphics.ScaleTransform(-1.0f, 1.0f);
                    break;

                case GerberMirrorState.FlipB:
                    graphics.ScaleTransform(1.0f, -1.0f);
                    break;

                case GerberMirrorState.FlipAB:
                    graphics.ScaleTransform(-1.0f, -1.0f);
                    break;

                default:
                    break;
            }

            // Finally, apply axis select.
            if (netState.AxisSelect == GerberAxisSelect.SwapAB)
            {
                // Do this by rotating 270 degrees counterclockwise, then mirroring the Y axis.
                graphics.RotateTransform(90);
                graphics.ScaleTransform(1.0f, -1.0f);
            }
        }
    }
}
