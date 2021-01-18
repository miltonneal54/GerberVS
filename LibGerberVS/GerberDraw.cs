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
        private static bool invert;

        internal static void DrawImageToTarget(Graphics graphics, SelectionInformation selectionInfo, Color foreGroundColor, Color backGroundColor)
        {
            invert = false;
            GerberFileInformation fileInfo = selectionInfo.SelectedFileInfo;
            RenderToTarget(graphics, fileInfo.Image, selectionInfo.SelectedNodeArray.SelectedNetList, fileInfo.UserTransform, foreGroundColor, backGroundColor);
        }


        internal static void DrawImageToTarget(Graphics graphics, GerberFileInformation fileInfo, Color foreGroundColor, Color backGroundColor)
        {
            invert = fileInfo.UserTransform.Inverted;
            RenderToTarget(graphics, fileInfo.Image, fileInfo.Image.GerberNetList, fileInfo.UserTransform, foreGroundColor, backGroundColor);
        }

        // Renders a gerber image to the specified graphics target.
        private static void RenderToTarget(Graphics graphics, GerberImage gerberImage, Collection<GerberNet> gerberNetList, GerberUserTransform userTransform,
            Color foreGroundColor, Color backGroundColor)
        {
            float dx, dy;
            float startX, startY, stopX, stopY;
            float p0, p1, p2, p3, p4;
            int repeatX = 1, repeatY = 1;
            float repeatDistanceX = 0.0f, repeatDistanceY = 0.0f;

            Collection<SimplifiedApertureMacro> simplifiedMacroList;
            PointF startPoint, endPoint;
            RectangleF apertureRectangle;

            GerberNet currentNet = null;
            GerberLevel oldLevel = null;
            GerberNetState oldState = null;
            bool invertPolarity = false;
            int netListIndex = 0;

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

            if (invertPolarity)
                graphics.Clear(foreGroundColor);

            GraphicsState gState = graphics.Save();
            for (netListIndex = 0; netListIndex < gerberNetList.Count; GetNextRenderObject(gerberNetList, ref netListIndex))
            {
                currentNet = gerberNetList[netListIndex];
                if (currentNet.Level != oldLevel)
                {
                    graphics.Restore(gState);
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

                    oldLevel = currentNet.Level;
                    ApplyNetStateTransformation(graphics, currentNet.NetState);
                    oldState = currentNet.NetState;
                }

                // Check if this is a new netstate.
                if (currentNet.NetState != oldState)
                {
                    // A new state, so recalculate the new transformation matrix.
                    graphics.Restore(gState);
                    graphics.RotateTransform((float)currentNet.Level.Rotation);
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
                                using (GraphicsPath path = new GraphicsPath())
                                {
                                    FillPolygonAreaPath(path, gerberNetList, netListIndex, stepAndRepeatX, stepAndRepeatY);
                                    if (path.PointCount > 0)
                                        graphics.FillPath(brush, path);
                                }

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

                                            // For now, just render ovals and polygons like a circle.
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
                                        float centreX = (float)currentNet.CircleSegment.CenterX;
                                        float centreY = (float)currentNet.CircleSegment.CenterY;
                                        float width = (float)currentNet.CircleSegment.Width;
                                        float height = (float)currentNet.CircleSegment.Height;
                                        float startAngle = (float)currentNet.CircleSegment.StartAngle;
                                        float sweepAngle = (float)currentNet.CircleSegment.SweepAngle;
                                        if (gerberImage.ApertureArray[currentNet.Aperture].ApertureType == GerberApertureType.Rectangle)
                                            pen.SetLineCap(LineCap.Square, LineCap.Square, DashCap.Flat);

                                        else
                                            pen.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);

                                        RectangleF arcRectangle = new RectangleF(centreX - (width / 2), centreY - (height / 2), width, height);
                                        pen.Width = (float)gerberImage.ApertureArray[currentNet.Aperture].Parameters[0];
                                        if (arcRectangle != RectangleF.Empty)
                                            graphics.DrawArc(pen, arcRectangle, startAngle, sweepAngle);

                                        break;

                                    default:
                                        break;
                                }
                                break;

                            case GerberApertureState.Flash:
                                p0 = (float)gerberImage.ApertureArray[currentNet.Aperture].Parameters[0];
                                p1 = (float)gerberImage.ApertureArray[currentNet.Aperture].Parameters[1];
                                p2 = (float)gerberImage.ApertureArray[currentNet.Aperture].Parameters[2];
                                p3 = (float)gerberImage.ApertureArray[currentNet.Aperture].Parameters[3];
                                p4 = (float)gerberImage.ApertureArray[currentNet.Aperture].Parameters[4];

                                GraphicsState state = graphics.Save();
                                graphics.TranslateTransform(stopX, stopY);
                                using (GraphicsPath path = new GraphicsPath())
                                {
                                    switch (gerberImage.ApertureArray[currentNet.Aperture].ApertureType)
                                    {
                                        case GerberApertureType.Circle:
                                            apertureRectangle = new RectangleF(-(p0 / 2), -(p0 / 2), p0, p0);
                                            path.AddEllipse(apertureRectangle);
                                            DrawAperatureHole(path, p1, p2);
                                            break;

                                        case GerberApertureType.Rectangle:
                                            apertureRectangle = new RectangleF(-(p0 / 2), -(p1 / 2), p0, p1);
                                            path.AddRectangle(apertureRectangle);
                                            DrawAperatureHole(path, p2, p3);
                                            break;

                                        case GerberApertureType.Oval:
                                            apertureRectangle = new RectangleF(-(p0 / 2), -(p1 / 2), p0, p1);
                                            CreateOblongPath(path, p0, p1);
                                            DrawAperatureHole(path, p2, p3);
                                            break;

                                        case GerberApertureType.Polygon:
                                            CreatePolygonPath(path, p0, p1, p2);
                                            DrawAperatureHole(path, p3, p4);
                                            break;

                                        case GerberApertureType.Macro:
                                            simplifiedMacroList = gerberImage.ApertureArray[currentNet.Aperture].SimplifiedMacroList;
                                            DrawApertureMacro(graphics, simplifiedMacroList, brush.Color, backGroundColor);
                                            break;

                                        default:
                                            break;
                                    }

                                    graphics.FillPath(brush, path); // Fill the path.
                                }

                                graphics.Restore(state);
                                break;

                            case GerberApertureState.Deleted:
                                continue;

                            default:
                                break;
                        }
                    }
                }
            }

            pen.Dispose();
            brush.Dispose();
            graphics.Restore(gState);
        }

        

        private static bool DrawApertureMacro(Graphics graphics, Collection<SimplifiedApertureMacro> simplifiedApertureList, Color layerColor, Color backColor)
        {
            //Debug.WriteLine("Drawing simplified Aperture macros:");
            bool success = true;                // Sucessfully processed macro flag.
            GraphicsState state = graphics.Save();

            using (GraphicsPath graphicsPath = new GraphicsPath())
            {
                foreach (SimplifiedApertureMacro simplifiedAperture in simplifiedApertureList)
                {
                    graphicsPath.FillMode = FillMode.Alternate;
                    // This handles the exposure thing in the Aperture macro
                    // The exposure is always the first element on stack independent of Aperture macro.
                    if (simplifiedAperture.ApertureType == GerberApertureType.MacroCircle)
                    {
                        float centreX = (float)simplifiedAperture.Parameters[(int)CircleParameters.CentreX];
                        float centreY = (float)simplifiedAperture.Parameters[(int)CircleParameters.CentreY];
                        float diameter = (float)simplifiedAperture.Parameters[(int)CircleParameters.Diameter];
                        float exposure = (float)simplifiedAperture.Parameters[(int)CircleParameters.Exposure];
                        float radius = diameter / 2.0f;

                        RectangleF objectRectangle = new RectangleF(centreX - radius, centreY - radius, diameter, diameter);

                        if (exposure > 0)
                            graphicsPath.FillMode = FillMode.Winding;

                        graphicsPath.AddEllipse(objectRectangle);
                    }

                    else if (simplifiedAperture.ApertureType == GerberApertureType.MacroOutline)
                    {
                        float exposure = (float)simplifiedAperture.Parameters[(int)OutlineParameters.Exposure];
                        int numberOfPoints = (int)simplifiedAperture.Parameters[(int)OutlineParameters.NumberOfPoints];
                        float outlineFirstX = (float)simplifiedAperture.Parameters[(int)OutlineParameters.FirstX];
                        float outlineFirstY = (float)simplifiedAperture.Parameters[(int)OutlineParameters.FirstY];
                        float rotation = (float)simplifiedAperture.Parameters[(numberOfPoints * 2) + (int)OutlineParameters.Rotation];

                        numberOfPoints += 1;
                        PointF[] points = new PointF[numberOfPoints];
                        for (int p = 0; p < numberOfPoints; p++)
                        {
                            points[p] = new PointF((float)(simplifiedAperture.Parameters[(p * 2) + (int)OutlineParameters.FirstX]),
                                                   (float)(simplifiedAperture.Parameters[(p * 2) + (int)OutlineParameters.FirstY]));
                        }

                        if (rotation != 0)
                            ApertureTransformPoints(points, rotation);

                        if (exposure > 0)
                            graphicsPath.FillMode = FillMode.Winding;

                        graphicsPath.AddPolygon(points);
                    }

                    else if (simplifiedAperture.ApertureType == GerberApertureType.MacroPolygon)
                    {
                        float exposure = (float)simplifiedAperture.Parameters[(int)PolygonParameters.Exposure];
                        int numberOfSides = (int)simplifiedAperture.Parameters[(int)PolygonParameters.NumberOfSides];
                        float centreX = (float)simplifiedAperture.Parameters[(int)PolygonParameters.CentreX];
                        float centreY = (float)simplifiedAperture.Parameters[(int)PolygonParameters.CentreY];
                        float diameter = (float)simplifiedAperture.Parameters[(int)PolygonParameters.Diameter];
                        float rotation = (float)simplifiedAperture.Parameters[(int)PolygonParameters.Rotation];

                        if (exposure > 0)
                            graphicsPath.FillMode = FillMode.Winding;

                        CreatePolygonPath(graphicsPath, diameter, numberOfSides, rotation);
                    }

                    else if (simplifiedAperture.ApertureType == GerberApertureType.MacroMoire)
                    {
                        float centreX = (float)simplifiedAperture.Parameters[(int)MoireParameters.CentreX];
                        float centreY = (float)simplifiedAperture.Parameters[(int)MoireParameters.CentreY];
                        float diameter = (float)(simplifiedAperture.Parameters[(int)MoireParameters.OutsideDiameter]);
                        float diameterDifference = (float)(simplifiedAperture.Parameters[(int)MoireParameters.GapWidth]) * 2;
                        float rotation = (float)simplifiedAperture.Parameters[(int)MoireParameters.Rotation];
                        float crossHairLength = (float)(simplifiedAperture.Parameters[(int)MoireParameters.CrosshairLength]);
                        float circleLineWidth = (float)simplifiedAperture.Parameters[(int)MoireParameters.CircleLineWidth];
                        int numberOfCircles = (int)simplifiedAperture.Parameters[(int)MoireParameters.NumberOfCircles];

                        // Get crosshair points.
                        float halfCrossHairLength = (crossHairLength / 2) + (circleLineWidth / 2);
                        PointF[] points = new PointF[] { new PointF(centreX - halfCrossHairLength, centreY), new PointF(centreX + halfCrossHairLength, centreY),
                                                         new PointF(centreX, centreY - halfCrossHairLength), new PointF(centreX, centreY + halfCrossHairLength) };

                        if (rotation != 0)
                            ApertureTransformPoints(points, rotation);

                        using (Pen pen = new Pen(layerColor))
                        {
                            // Draw target.
                            pen.Width = circleLineWidth;
                            for (int i = 0; i < numberOfCircles; i++)
                            {
                                float targetSize = diameter - (diameterDifference * i);
                                RectangleF targetRectangle = new RectangleF(centreX - (targetSize / 2), centreY - (targetSize / 2), targetSize, targetSize);
                                if (!targetRectangle.IsEmpty)
                                    graphics.DrawEllipse(pen, targetRectangle);
                            }

                            // Draw crosshairs.
                            pen.Width = (float)simplifiedAperture.Parameters[(int)MoireParameters.CrosshairLineWidth];
                            graphics.DrawLine(pen, points[0], points[1]);
                            graphics.DrawLine(pen, points[2], points[3]);
                        }
                    }

                    else if (simplifiedAperture.ApertureType == GerberApertureType.MacroThermal)
                    {
                        float centreX = (float)simplifiedAperture.Parameters[(int)ThermalParameters.CentreX];
                        float centreY = (float)simplifiedAperture.Parameters[(int)ThermalParameters.CentreY];
                        float innerDiameter = (float)simplifiedAperture.Parameters[(int)ThermalParameters.InsideDiameter];
                        float outerDiameter = (float)simplifiedAperture.Parameters[(int)ThermalParameters.OutsideDiameter];
                        float crossHairWidth = (float)simplifiedAperture.Parameters[(int)ThermalParameters.CrosshairLineWidth];
                        float rotation = (float)simplifiedAperture.Parameters[(int)ThermalParameters.Rotation];
                        float gap = outerDiameter - innerDiameter;

                        // Draw the pad.
                        RectangleF objectRectangle = new RectangleF(centreX - (gap / 2), centreY - (gap / 2), gap, gap);
                        using (Pen pen = new Pen(layerColor))
                        {
                            pen.Width = gap / 2;
                            graphics.DrawEllipse(pen, objectRectangle);
                        }

                        // Draw thermal relief through the pad area.
                        PointF[] points = new PointF[] { new PointF(centreX - gap, centreY), new PointF(centreX - (innerDiameter / 2), centreY),
                                                         new PointF(centreX + (innerDiameter / 2), centreY), new PointF(centreX + gap, centreY),
                                                         new PointF(centreX, centreY - gap), new PointF(centreX, centreY - (innerDiameter / 2)),
                                                         new PointF(centreX, centreY + (innerDiameter / 2)), new PointF(centreX, centreY + gap)};

                        if (rotation != 0)
                            ApertureTransformPoints(points, rotation);

                        using (Pen pen = new Pen(backColor))
                        {
                            pen.Width = crossHairWidth;
                            pen.StartCap = LineCap.Square;
                            pen.EndCap = LineCap.Square;
                            graphics.DrawLine(pen, points[0], points[1]);
                            graphics.DrawLine(pen, points[2], points[3]);
                            graphics.DrawLine(pen, points[4], points[5]);
                            graphics.DrawLine(pen, points[6], points[7]);
                        }
                    }

                    else if (simplifiedAperture.ApertureType == GerberApertureType.MacroLine20)
                    {
                        float exposure = (float)simplifiedAperture.Parameters[(int)Line20Parameters.Exposure];
                        float startX = (float)simplifiedAperture.Parameters[(int)Line20Parameters.StartX];
                        float startY = (float)simplifiedAperture.Parameters[(int)Line20Parameters.StartY];
                        float endX = (float)simplifiedAperture.Parameters[(int)Line20Parameters.EndX];
                        float endY = (float)simplifiedAperture.Parameters[(int)Line20Parameters.EndY];
                        float lineWidth = (float)simplifiedAperture.Parameters[(int)Line20Parameters.LineWidth];
                        float rotation = (float)simplifiedAperture.Parameters[(int)Line20Parameters.Rotation];

                        PointF[] points = new PointF[] { new PointF(startX, startY), new PointF(endX, endY) };

                        if (rotation != 0)
                            ApertureTransformPoints(points, rotation);

                        if (exposure > 0)
                            graphicsPath.FillMode = FillMode.Winding;

                        using (Pen pen = new Pen(layerColor))
                        {
                            pen.Width = lineWidth;
                            pen.StartCap = LineCap.Square;
                            pen.EndCap = LineCap.Square;
                            graphics.DrawLine(pen, points[0], points[1]);
                        }
                    }

                    else if (simplifiedAperture.ApertureType == GerberApertureType.MacroLine21)
                    {
                        float exposure = (float)simplifiedAperture.Parameters[(int)Line21Parameters.Exposure];
                        float halfWidth = (float)(simplifiedAperture.Parameters[(int)Line21Parameters.LineWidth]) / 2.0f;
                        float halfHeight = (float)(simplifiedAperture.Parameters[(int)Line21Parameters.LineHeight]) / 2.0f;
                        float centreX = (float)simplifiedAperture.Parameters[(int)Line21Parameters.CentreX];
                        float centreY = (float)simplifiedAperture.Parameters[(int)Line21Parameters.CentreY];
                        float rotation = (float)simplifiedAperture.Parameters[(int)Line21Parameters.Rotation];

                        PointF[] points = new PointF[] { new PointF(centreX - halfWidth, centreY - halfHeight),
                                                         new PointF(centreX + halfWidth, centreY - halfHeight),
                                                         new PointF(centreX + halfWidth, centreY + halfHeight),
                                                         new PointF(centreX - halfWidth, centreY + halfHeight) };

                        if (rotation != 0)
                            ApertureTransformPoints(points, rotation);

                        if (exposure > 0)
                            graphicsPath.FillMode = FillMode.Winding;

                        graphicsPath.AddPolygon(points);
                    }

                    else if (simplifiedAperture.ApertureType == GerberApertureType.MacroLine22)
                    {
                        float exposure = (float)simplifiedAperture.Parameters[(int)Line22Parameters.Exposure];
                        float width = (float)(simplifiedAperture.Parameters[(int)Line22Parameters.LineWidth]);
                        float height = (float)(simplifiedAperture.Parameters[(int)Line22Parameters.LineHeight]);
                        float lowerLeftX = (float)simplifiedAperture.Parameters[(int)Line22Parameters.LowerLeftX];
                        float lowerLeftY = (float)simplifiedAperture.Parameters[(int)Line22Parameters.LowerLeftY];
                        float rotation = (float)simplifiedAperture.Parameters[(int)Line22Parameters.Rotation];

                        PointF[] points = new PointF[] { new PointF(lowerLeftX, lowerLeftY),
                                                         new PointF(width, lowerLeftY),
                                                         new PointF(width, height),
                                                         new PointF(lowerLeftX, height) };

                        if (rotation != 0)
                            ApertureTransformPoints(points, rotation);

                        if (exposure > 0)
                            graphicsPath.FillMode = FillMode.Winding;

                        graphicsPath.AddPolygon(points);
                    }

                    else
                        success = false;
                }

                if (graphicsPath.PointCount > 0)
                    graphics.FillPath(new SolidBrush(layerColor), graphicsPath);
            }

            graphics.Restore(state);
            return success;
        }

        private static void CreateOblongPath(GraphicsPath path, float width, float height)
        {
            float diameter;
            float left = -(width / 2);
            float top = -(height / 2);
            PointF location = new PointF(left, top);

            if (width > height)
            {
                // Returns a horizontal capsule. 
                diameter = height;
                SizeF sizeF = new SizeF(diameter, diameter);
                RectangleF arc = new RectangleF(location, sizeF);
                path.AddArc(arc, 90, 180);
                arc.X = (left + width) - diameter;
                path.AddArc(arc, 270, 180);
            }

            else if (width < height)
            {
                // Returns a vertical capsule. 
                diameter = width;
                SizeF sizeF = new SizeF(diameter, diameter);
                RectangleF arc = new RectangleF(location, sizeF);
                path.AddArc(arc, 180, 180);
                arc.Y = (top + height) - diameter;
                path.AddArc(arc, 0, 180);
            }

            else
                path.AddEllipse(left, top, width, height);

            path.CloseFigure();
        }

        // Creates a path for flashed polygons.
        private static void CreatePolygonPath(GraphicsPath path, float diameter, float numberOfSides, float rotation)
        {
            // Skip first point, since we've moved there already.
            // Include last point, since we may be drawing an Aperture hole next.

            PointF[] points = new PointF[(int)numberOfSides];
            points[0] = new PointF(diameter / 2.0f, 0.0f);

            for (int i = 1; i < numberOfSides; i++)
            {
                double angle = (double)i / numberOfSides * Math.PI * 2.0;
                points[i] = new PointF((float)(Math.Cos(angle) * diameter / 2.0), (float)(Math.Sin(angle) * diameter / 2.0));
            }

            if (rotation != 0)
                ApertureTransformPoints(points, rotation);

            path.AddPolygon(points);
        }

        // Creates a polgon path to fill from a series of connecting nets.
        internal static void FillPolygonAreaPath(GraphicsPath path, Collection<GerberNet> gerberNetList, int netListIndex, float srX, float srY)
        {
            float stopX, stopY, startX, startY;
            float cpX = 0.0f, cpY = 0.0f;
            float circleWidth = 0.0f, circleHeight = 0.0f;
            float startAngle = 0.0f, sweepAngle = 0.0f;
            bool done = false;
            RectangleF arcRectangle = RectangleF.Empty;
            GerberNet currentNet = null;

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
                    arcRectangle = new RectangleF(cpX - (circleWidth / 2), cpY - (circleHeight / 2), circleWidth, circleHeight);
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
                        done = true;
                        break;
                }

                if (done)
                    break;
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

        /*private static void UpdateMacroExposure(ref Color apertureColor, Color backColor, Color layerColor, double exposureSetting)
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
        }*/

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

        private static void ApertureTransformPoints(PointF[] points, double rotation)
        {
            using (Matrix apertureMatrix = new Matrix())
            {

                apertureMatrix.Rotate((float)rotation);
                apertureMatrix.TransformPoints(points);
            }
            //apertureMatrix.Dispose();
        }
    }
}
