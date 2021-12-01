using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace GerberVS
{
    /// <summary>
    /// Test for hits in the underlying graphics objects.
    /// </summary>
    internal static class GerberSelectRegion
    {
        /// <summary>
        /// Adds an object to the selection list if the test point is within it's bounds.
        /// </summary>
        /// <param name="graphics">target graphics</param>
        /// <param name="selectionInfo">selection data</param>
        /// <param name="index">current net index</param>
        internal static void PointInObject(Graphics graphics, SelectionInformation selectionInfo, ref int index)
        {
            bool inSelect = false;
            GerberImage image = selectionInfo.FileInfo.Image;
            GerberNet currentNet = image.GerberNetList[index];
            float x1 = (float)selectionInfo.LowerLeftX;
            float y1 = (float)selectionInfo.LowerLeftY;

            using (Matrix apertureMatrix = new Matrix())
            using (GraphicsPath path = new GraphicsPath())
            {
                if (currentNet.BoundingBox == null)
                    return;

                apertureMatrix.Rotate((float)selectionInfo.FileInfo.Image.ImageInfo.ImageRotation);// <----
                apertureMatrix.Rotate((float)selectionInfo.FileInfo.UserTransform.Rotation);

                float startX, startY, stopX, stopY;
                PointD offSet = LibGerberVS.GetImageOffsets(image);

                if(currentNet.Level.StepAndRepeat.X == 1 && currentNet.Level.StepAndRepeat.Y == 1)
                {
                    if (!currentNet.BoundingBox.Contains(new PointD(x1, y1)))
                        return;
                }

                // Check through the step and repeats.
                for (int rx = 0; rx < currentNet.Level.StepAndRepeat.X; rx++)
                {
                    for (int ry = 0; ry < currentNet.Level.StepAndRepeat.Y; ry++)
                    {
                        float stepAndRepeatX = rx * (float)currentNet.Level.StepAndRepeat.DistanceX;
                        float stepAndRepeatY = ry * (float)currentNet.Level.StepAndRepeat.DistanceY;
                        startX = (float)(currentNet.StartX + stepAndRepeatX + offSet.X);
                        startY = (float)(currentNet.StartY + stepAndRepeatY + offSet.Y);
                        stopX = (float)(currentNet.EndX + stepAndRepeatX + offSet.X);
                        stopY = (float)(currentNet.EndY + stepAndRepeatY + offSet.Y);

                        if (currentNet.ApertureState == GerberApertureState.Flash)
                        {
                            int aperture = currentNet.Aperture;
                            RectangleF objRectangle = RectangleF.Empty;
                            GerberApertureType type = selectionInfo.FileInfo.Image.ApertureArray()[aperture].ApertureType;
                            switch (type)
                            {
                                case GerberApertureType.Circle:
                                case GerberApertureType.Rectangle:
                                case GerberApertureType.Oval:
                                case GerberApertureType.Polygon:
                                    objRectangle = currentNet.BoundingBox.ToRectangle();
                                    objRectangle.X += stepAndRepeatX;
                                    objRectangle.Y += stepAndRepeatY;
                                    path.AddRectangle(objRectangle);
                                    break;

                                case GerberApertureType.Macro:
                                    GerberApertureType macroType = selectionInfo.FileInfo.Image.ApertureArray()[aperture].SimplifiedMacroList[0].ApertureType;
                                    SimplifiedApertureMacro sam = selectionInfo.FileInfo.Image.ApertureArray()[aperture].SimplifiedMacroList[0];
                                    x1 -= (float)currentNet.EndX;
                                    y1 -= (float)currentNet.EndY;

                                    if (macroType == GerberApertureType.MacroCircle)
                                    {
                                        float centreX = (float)sam.Parameters[(int)CircleParameters.CentreX];
                                        float centreY = (float)sam.Parameters[(int)CircleParameters.CentreY];
                                        float diameter = (float)sam.Parameters[(int)CircleParameters.Diameter];

                                        objRectangle = new RectangleF(centreX - (diameter / 2), centreY - (diameter / 2), diameter, diameter);
                                        path.AddEllipse(objRectangle);
                                    }

                                    else if (macroType == GerberApertureType.MacroMoire)
                                    {
                                        float centreX = (float)sam.Parameters[(int)MoireParameters.CentreX];
                                        float centreY = (float)sam.Parameters[(int)MoireParameters.CentreY];
                                        float diameter = (float)(sam.Parameters[(int)MoireParameters.OutsideDiameter]);
                                        float rotation = (float)sam.Parameters[(int)MoireParameters.Rotation];
                                        float width = (float)sam.Parameters[(int)MoireParameters.CircleLineWidth];

                                        objRectangle = new RectangleF(centreX - (diameter / 2), centreY - (diameter / 2), diameter, diameter);
                                        path.AddEllipse(objRectangle);
                                    }

                                    else if (macroType == GerberApertureType.MacroThermal)
                                    {
                                        float centreX = (float)sam.Parameters[(int)ThermalParameters.CentreX];
                                        float centreY = (float)sam.Parameters[(int)ThermalParameters.CentreY];
                                        float diameter = (float)sam.Parameters[(int)ThermalParameters.OutsideDiameter];
                                        float rotation = (float)sam.Parameters[(int)ThermalParameters.Rotation];

                                        objRectangle = new RectangleF(centreX - (diameter / 2), centreY - (diameter / 2), diameter, diameter);
                                        path.AddEllipse(objRectangle);
                                    }

                                    else if (macroType == GerberApertureType.MacroOutline)
                                    {
                                        int outlineFirstX = (int)OutlineParameters.FirstX;
                                        int outlineFirstY = (int)OutlineParameters.FirstY;
                                        int numberOfPoints = (int)sam.Parameters[(int)OutlineParameters.NumberOfPoints];
                                        float rotation = (float)sam.Parameters[(numberOfPoints * 2) + (int)OutlineParameters.Rotation];

                                        numberOfPoints += 1;
                                        PointF[] points = new PointF[numberOfPoints];
                                        for (int p = 0; p < numberOfPoints; p++)
                                        {
                                            points[p] = new PointF((float)sam.Parameters[(p * 2) + outlineFirstX],
                                                                   (float)sam.Parameters[(p * 2) + outlineFirstY]);
                                        }

                                        apertureMatrix.Rotate(rotation);
                                        apertureMatrix.TransformPoints(points);
                                        path.AddPolygon(points);
                                    }

                                    else if (macroType == GerberApertureType.MacroPolygon)
                                    {
                                        int numberOfSides = (int)sam.Parameters[(int)PolygonParameters.NumberOfSides];
                                        float diameter = (float)sam.Parameters[(int)PolygonParameters.Diameter];
                                        float rotation = (float)sam.Parameters[(int)PolygonParameters.Rotation];
                                        GerberDraw.CreatePolygonPath(path, diameter, numberOfSides, rotation);
                                    }

                                    else if (macroType == GerberApertureType.MacroLine20)
                                    {
                                        float rotation = (float)sam.Parameters[(int)Line20Parameters.Rotation];
                                        float halfWidth = (float)sam.Parameters[(int)Line20Parameters.LineWidth] / 2;
                                        float sX = (float)sam.Parameters[(int)Line20Parameters.StartX];
                                        float sY = (float)sam.Parameters[(int)Line20Parameters.StartY];
                                        float endX = (float)sam.Parameters[(int)Line20Parameters.EndX];
                                        float endY = (float)sam.Parameters[(int)Line20Parameters.EndY];

                                        PointF[] points = new PointF[] { new PointF(sX, sY + halfWidth),
                                                                         new PointF(endX, endY + halfWidth),
                                                                         new PointF(endX, endY - halfWidth),
                                                                         new PointF(sX, sY - halfWidth) };

                                        apertureMatrix.Rotate(rotation);
                                        apertureMatrix.TransformPoints(points);
                                        path.AddPolygon(points);
                                    }

                                    else if (macroType == GerberApertureType.MacroLine21)
                                    {
                                        float halfWidth = (float)(sam.Parameters[(int)Line21Parameters.LineWidth]) / 2.0f;
                                        float halfHeight = (float)(sam.Parameters[(int)Line21Parameters.LineHeight]) / 2.0f;
                                        float centreX = (float)sam.Parameters[(int)Line21Parameters.CentreX];
                                        float centreY = (float)sam.Parameters[(int)Line21Parameters.CentreY];
                                        float rotation = (float)sam.Parameters[(int)Line21Parameters.Rotation];

                                        PointF[] points = new PointF[] { new PointF(centreX - halfWidth, centreY - halfHeight),
                                                                         new PointF(centreX + halfWidth, centreY - halfHeight),
                                                                         new PointF(centreX + halfWidth, centreY + halfHeight),
                                                                         new PointF(centreX - halfWidth, centreY + halfHeight) };

                                        apertureMatrix.Rotate(rotation);
                                        apertureMatrix.TransformPoints(points);
                                        path.AddPolygon(points);
                                    }

                                    else if (macroType == GerberApertureType.MacroLine22)
                                    {
                                        float width = (float)(sam.Parameters[(int)Line22Parameters.LineWidth]);
                                        float height = (float)(sam.Parameters[(int)Line22Parameters.LineHeight]);
                                        float lowerLeftX = (float)sam.Parameters[(int)Line22Parameters.LowerLeftX];
                                        float lowerLeftY = (float)sam.Parameters[(int)Line22Parameters.LowerLeftY];
                                        float rotation = (float)sam.Parameters[(int)Line22Parameters.Rotation];

                                        PointF[] points = new PointF[] { new PointF(lowerLeftX, lowerLeftY),
                                                                         new PointF(width, lowerLeftY),
                                                                         new PointF(width, height),
                                                                         new PointF(lowerLeftX, height) };

                                        apertureMatrix.Rotate(rotation);
                                        apertureMatrix.TransformPoints(points);
                                        path.AddPolygon(points);
                                    }
                                    break;
                            }

                            if (path.IsVisible(new PointF(x1, y1), graphics))
                                inSelect = true;
                        }

                        else if (currentNet.ApertureState == GerberApertureState.On)
                        {
                            switch (currentNet.Interpolation)
                            {
                                case GerberInterpolation.RegionStart:
                                    GerberDraw.FillRegionPath(path, image.GerberNetList, index, stepAndRepeatX, stepAndRepeatY);
                                    path.Transform(apertureMatrix);
                                    if (path.IsVisible(new PointF(x1, y1), graphics))
                                        inSelect = true;

                                    break;

                                case GerberInterpolation.Linear:
                                //case GerberInterpolation.DrillSlot:
                                    using (Pen pen = new Pen(Color.Black))
                                    {
                                        pen.Width = (float)image.ApertureArray()[currentNet.Aperture].Parameters()[0];
                                        pen.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);
                                        PointF[] points = new PointF[] { new PointF(startX, startY), new PointF(stopX, stopY) };
                                        path.AddLine(points[0], points[1]);
                                        path.Transform(apertureMatrix);
                                        if (path.IsOutlineVisible(new PointF(x1, y1), pen, graphics))
                                            inSelect = true;
                                    }
                                    break;

                                case GerberInterpolation.ClockwiseCircular:
                                case GerberInterpolation.CounterclockwiseCircular:
                                    using (Pen pen = new Pen(Color.Black))
                                    {
                                        float centreX = (float)currentNet.CircleSegment.CenterX;
                                        float centreY = (float)currentNet.CircleSegment.CenterY;
                                        float width = (float)currentNet.CircleSegment.Width;
                                        float height = (float)currentNet.CircleSegment.Height;
                                        float startAngle = (float)currentNet.CircleSegment.StartAngle;
                                        float sweepAngle = (float)currentNet.CircleSegment.SweepAngle;

                                        pen.Width = (float)image.ApertureArray()[currentNet.Aperture].Parameters()[0];
                                        if (image.ApertureArray()[currentNet.Aperture].ApertureType == GerberApertureType.Rectangle)
                                            pen.SetLineCap(LineCap.Square, LineCap.Square, DashCap.Flat);

                                        else
                                            pen.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);

                                        RectangleF arcRectangle = new RectangleF(centreX - (width / 2), centreY - (height / 2), width, height);
                                        path.AddArc(arcRectangle, startAngle, sweepAngle);
                                        path.Transform(apertureMatrix);
                                        if (path.IsOutlineVisible(new PointF(x1, y1), pen, graphics))
                                            inSelect = true;
                                    }
                                    break;
                            }
                        }
                    }
                    // If we have a hit, then no need to test other step and repeats.
                    if (inSelect)
                        break;
                }
            }

            if (inSelect)
                AddSelection(selectionInfo, image, ref index);
        }

        /// <summary>
        /// Adds an object to the selection list if it's bounds lies within the selection bounds.
        /// </summary>
        /// <param name="graphics">target graphics</param>
        /// <param name="selectionInfo">selection data</param>
        /// <param name="index">current net index</param>
        internal static void ObjectsInRegion(Graphics graphics, SelectionInformation selectionInfo, ref int index)
        {
            GerberImage image = selectionInfo.FileInfo.Image;
            GerberNet currentNet = image.GerberNetList[index];
            float x1 = (float)selectionInfo.LowerLeftX, y1 = (float)selectionInfo.LowerLeftY;
            float x2 = (float)selectionInfo.UpperRightX, y2 = (float)selectionInfo.UpperRightY;

            if (currentNet.BoundingBox != null)
            {
                double left = Math.Min(x1, x2);
                double right = Math.Max(x1, x2);
                double top = Math.Min(y1, y2);
                double bottom = Math.Max(y1, y2);

                BoundingBox selectionBox = new BoundingBox(left, bottom, right, top);
                if (!selectionBox.Contains(currentNet.BoundingBox))
                    return;

                if (currentNet.ApertureState == GerberApertureState.On || currentNet.ApertureState == GerberApertureState.Flash)
                {
                    if (selectionBox.Contains(currentNet.BoundingBox))
                        AddSelection(selectionInfo, image, ref index);
                }
            }
        }

        // Add the net to the selection list.
        private static void AddSelection(SelectionInformation selectionInfo, GerberImage image, ref int index)
        {
            GerberNet currentNet = image.GerberNetList[index];

            selectionInfo.SelectedNodeArray.SelectedNetList.Add(currentNet);
            selectionInfo.SelectedNodeArray.SelectedNetIndex.Add(index);
            // Add all the polygon area points.
            if (currentNet.Interpolation == GerberInterpolation.RegionStart)
            {
                do
                {
                    index++;
                    currentNet = image.GerberNetList[index];
                    selectionInfo.SelectedNodeArray.SelectedNetList.Add(currentNet);
                } while (currentNet.Interpolation != GerberInterpolation.RegionEnd);
            }
        }
    }
}
