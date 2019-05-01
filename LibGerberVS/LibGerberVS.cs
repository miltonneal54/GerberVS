using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

[assembly: CLSCompliant(true)]
namespace GerberVS
{
    public class LibGerberVS
    {
        private RectangleF rect;
        private const int NumberOfDefaultColors = 18;
        private static int defaultColorIndex = 0;
        private static int[,] defaultColors = {
	        {177, 115, 115, 222},
	        {177, 255, 127, 115},
	        {177, 193, 0, 224},
	        {177, 117, 242, 103},
	        {177, 0, 195, 195},
	        {177, 213, 253, 51},
	        {177, 209, 27, 104},
	        {177, 255, 197, 51},
	        {177, 186, 186, 186},
	        {177, 211, 211, 255},
	        {177, 253, 210, 206},
	        {177, 236, 194, 242},
	        {177, 208, 249, 204},
	        {177, 183, 255, 255},
	        {177, 241, 255, 183},
	        {177, 255, 202, 225},
	        {177, 253, 238, 197},
	        {177, 226, 226, 226} };

        private Color backgroundColor;

        /// <summary>
        /// Creates a new Gerber Project.
        /// </summary>
        /// <returns></returns>
        public GerberProject CreateNewProject()
        {
            GerberProject project = new GerberProject();
            project.Path = Directory.GetCurrentDirectory();
            project.CurrentIndex = -1;
            return project;
        }

        /// <summary>
        /// Removes a file from the project at the specified index.
        /// </summary>
        /// <param name="project">project containing the file info to remove</param>
        /// <param name="index">index to remove at</param>
        public void UnloadLayer(GerberProject project, int index)
        {
            int fileCount = project.FileInfo.Count;

            // Move all later layers down to fill the empty slot.
            for (int i = index; i < fileCount - 1; i++)
                project.FileInfo[i] = project.FileInfo[i + 1];

            // Make sure the final spot is clear.
            project.FileInfo.Remove(project.FileInfo[fileCount - 1]);
        }

        /// <summary>
        /// Removes all the files in the project.
        /// </summary>
        /// <param name="project">project containing the file list.</param>
        public void UnloadAllLayers(GerberProject project)
        {
            int fileIndex = project.FileInfo.Count - 1;
            // Must count down since UnloadLayer collapses layers down. Otherwise, layers slide past the index,
            for (int index = fileIndex; index >= 0; index--)
            {
                if (project.FileInfo[index] != null && !String.IsNullOrEmpty(project.FileInfo[index].FileName))
                    UnloadLayer(project, index);
            }
        }

        /// <summary>
        /// Changes the order that file layers are rendered.
        /// </summary>
        /// <param name="project">project containing the file list</param>
        /// <param name="oldPosition">from position</param>
        /// <param name="newPosition">to position</param>
        public void ChangeLayerOrder(GerberProject project, int oldPosition, int newPosition)
        {
            GerberFileInformation tempFileInfo = project.FileInfo[oldPosition];

            if (oldPosition < newPosition)
            {
                for (int index = oldPosition; index < newPosition; index++)
                    project.FileInfo[index] = project.FileInfo[index + 1];
            }

            else
            {
                for (int index = oldPosition; index > newPosition; index--)
                    project.FileInfo[index] = project.FileInfo[index - 1];
            }

            project.FileInfo[newPosition] = tempFileInfo;
        }

        /// <summary>
        /// Opens a layer file and appends it to the project.
        /// </summary>
        /// <param name="project">project to append file</param>
        /// <param name="fullPathName">file to open</param>
        /// <returns></returns>
        public void OpenLayerFromFilename(GerberProject project, string fullPathName)
        {
            bool reload = false;

            try
            {
                OpenImage(project, fullPathName, reload, -1);
            }

            catch (GerberFileException ex)
            {
                throw new GerberDLLException(ex.Message);
            }

        }

        /// <summary>
        /// Opens a layer file and appends it to the project using the specified color and alpha level.
        /// </summary>
        /// <param name="project">project</param>
        /// <param name="fullPathName">file path</param>
        /// <param name="color">layer color</param>
        /// <param name="alpha">alpha level</param>
        public void OpenLayerFromFilenameAndColor(GerberProject project, string fullPathName, Color color, int alpha)
        {
            int fileIndex = project.FileInfo.Count - 1;
            try
            {
                OpenImage(project, fullPathName, false, -1);
                project.FileInfo[fileIndex].LayerDirty = false;
                project.FileInfo[fileIndex].Color = Color.FromArgb(color.R, color.G, color.B);
                project.FileInfo[fileIndex].Alpha = alpha;
            }

            catch (GerberFileException ex)
            {
                throw new GerberDLLException(ex.Message);
            }
        }

        public void ReloadFile(GerberProject project, int index)
        {
            if (OpenImage(project, project.FileInfo[index].FullPathName, true, index))
                project.FileInfo[index].LayerDirty = false;
        }

        /// <summary>
        /// Calculates the overall image bounds of the project.
        /// </summary>
        /// <param name="project">project containing file list</param>
        public BoundingBox GetProjectBounds(GerberProject project)
        {
            double x1 = double.MaxValue, y1 = double.MaxValue, x2 = double.MinValue, y2 = double.MinValue;
            GerberImageInfo imageInfo;
            float minX, minY, maxX, maxY;
            int fileCount = project.FileInfo.Count;

            for (int i = 0; i < fileCount; i++)
            {
                if ((project.FileInfo[i] != null) && (project.FileInfo[i].IsVisible))
                {
                    imageInfo = project.FileInfo[i].Image.ImageInfo;
                    // Find the biggest image and use as a size reference.
                    minX = (float)imageInfo.MinX;
                    minY = (float)imageInfo.MinY;
                    maxX = (float)imageInfo.MaxX;
                    maxY = (float)imageInfo.MaxY;

                    if (!IsNormal(minX) || !IsNormal(minY) || !IsNormal(maxX) || !IsNormal(maxY))
                        continue;

                    // Transform the bounding box based on the user supplied transformations.
                    using (Matrix fullMatrix = new Matrix(1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f))
                    {
                        // Don't use mirroring for the scale matrix
                        double scaleX = project.UserTransform.ScaleX;
                        double scaleY = project.UserTransform.ScaleY;
                        if (project.UserTransform.MirrorAroundX)
                            scaleY *= -1;

                        if (project.UserTransform.MirrorAroundY)
                            scaleX *= -1;

                        fullMatrix.Scale((float)scaleX, (float)scaleY);
                        fullMatrix.Rotate((float)project.UserTransform.Rotation);
                        fullMatrix.Translate((float)project.UserTransform.TranslateX, (float)project.UserTransform.TranslateY);
                        PointF[] points = new PointF[] { new PointF(minX, minY), new PointF(maxX, maxY) };
                        fullMatrix.TransformPoints(points);
                        // Compare both min and max, since a mirror transform may have made "max" smaller than "min".
                        x1 = Math.Min(x1, points[0].X);
                        x1 = Math.Min(x1, points[1].X);
                        y1 = Math.Min(y1, points[0].Y);
                        y1 = Math.Min(y1, points[1].Y);
                        x2 = Math.Max(x2, points[0].X);
                        x2 = Math.Max(x2, points[1].X);
                        y2 = Math.Max(y2, points[0].Y);
                        y2 = Math.Max(y2, points[1].Y);
                    }
                }
            }

            BoundingBox boundingbox = new BoundingBox(x1, y2, x2, y1);
            return boundingbox;
        }

        /// <summary>
        /// Translates the rendered image to the centre of the display area.
        /// </summary>
        /// <param name="project">project details</param>
        /// <param name="renderInfo">render information</param>
        public void TranslateToCentreDisplay(GerberProject project, RenderInformation renderInfo)
        {
            BoundingBox projectBounds = GetProjectBounds(project);
            if (!projectBounds.IsValid())
                return;

            double left = projectBounds.Left - 0.25f;
            double bottom = projectBounds.Bottom - 0.25f;
            double right = projectBounds.Right + 0.25f;
            double top = projectBounds.Top + 0.25f;
            double imageWidth = right - left;
            double imageHeight = top - bottom;

            renderInfo.ImageWidth = imageWidth;
            renderInfo.ImageHeight = imageHeight;
            //renderInfo.Left = -((renderInfo.DisplayWidth - imageWidth) / 2) + left;
            //renderInfo.Bottom = ((renderInfo.DisplayHeight - imageHeight) / 2) + bottom;

            renderInfo.Left = ((renderInfo.DisplayWidth - imageWidth) / 2) - left;
            renderInfo.Bottom = -((renderInfo.DisplayHeight + imageHeight) / 2) - bottom;
            if (imageWidth > renderInfo.DisplayWidth)
                //renderInfo.Left = left;
                renderInfo.Left = -left;

            if (imageHeight > renderInfo.DisplayHeight)
                //renderInfo.Bottom = bottom;
                renderInfo.Bottom = -(imageHeight + bottom);

            renderInfo.ScaleFactorX = 1.0;
            renderInfo.ScaleFactorY = 1.0;
        }

        /// <summary>
        /// Translates the rendered image to the top left of the display area.
        /// </summary>
        /// <param name="project">project information</param>
        /// <param name="renderInfo">render information</param>
        public void TranslateToFitDisplay(GerberProject project, RenderInformation renderInfo)
        {
            double width, height;
            BoundingBox projectBounds = GetProjectBounds(project);
            if (!projectBounds.IsValid())
                return;

            double left = projectBounds.Left - 0.25f;
            double bottom = projectBounds.Bottom - 0.25f;
            double right = projectBounds.Right + 0.25f;
            double top = projectBounds.Top + 0.25f;

            width = right - left;
            height = top - bottom;
            renderInfo.ImageWidth = width;
            renderInfo.ImageHeight = height;
            renderInfo.Left = left;
            renderInfo.Bottom = right;
        }

        /// <summary>
        /// Creates the Gerber image holding all the geometry of the layer.
        /// </summary>
        /// <param name="filePath">filename containing the layer geometry</param>
        /// <returns>gerber image</returns>
        public static GerberImage CreateRS274XImageFromFile(string filePath)
        {
            GerberImage returnImage;

            returnImage = Gerber.ParseGerber(filePath);
            return returnImage;
        }

        /// <summary>
        /// Draws all the visible layers contained in the project.
        /// </summary>
        /// <param name="graphics">surface to render the image</param>
        /// <param name="project">project containing the files to render</param>
        /// <param name="renderInfo">information for positioning, scaling and translating</param>
        public void RenderAllLayers(Graphics graphics, GerberProject project, RenderInformation renderInfo)
        {
            int fileIndex = project.FileInfo.Count - 1;
            backgroundColor = project.BackgroundColor;
            graphics.Clear(backgroundColor);

            for (int i = fileIndex; i >= 0; i--)
            {
                if (project.FileInfo[i] != null && project.FileInfo[i].IsVisible)
                    RenderLayer(graphics, project, renderInfo, i);
            }
        }

        /// <summary>
        /// Draw the user selection layer;
        /// </summary>
        /// <param name="graphics">surface to render the image</param>
        /// <param name="project">project containing the files to render</param>
        /// <param name="renderInfo">information for positioning, scaling and translating</param>
        /// <param name="selectionInfo">information about the users selection</param>
        public void RenderSelectionLayer(Graphics graphics, GerberProject project, RenderInformation renderInfo, SelectionInformation selectionInfo)
        {
            int bmWidth = 0;
            int bmHeight = 0;

            // Calculate how big to make the bitmap back buffer.
            if (renderInfo.ImageWidth < renderInfo.DisplayWidth)
                bmWidth = (int)(renderInfo.DisplayWidth * graphics.DpiX);

            else
                bmWidth = (int)(renderInfo.ImageWidth * graphics.DpiX);

            if (renderInfo.ImageHeight < renderInfo.DisplayHeight)
                bmHeight = (int)(renderInfo.DisplayHeight * graphics.DpiY);

            else
                bmHeight = (int)(renderInfo.ImageHeight * graphics.DpiY);

            // Create a back buffer and draw to it with no alpha level.
            using (Bitmap bitmap = new Bitmap(bmWidth, bmHeight, graphics))
            using (Graphics backBuffer = Graphics.FromImage(bitmap))
            {
                backBuffer.CompositingMode = CompositingMode.SourceOver;
                ScaleAndTranslate(backBuffer, renderInfo);
                Color foregroundColor = Color.FromArgb(177, Color.White);
                GerberDraw.DrawImageToTarget(backBuffer, selectionInfo, project.UserTransform, foregroundColor, backgroundColor);
                // Copy the back buffer to the visible surface with alpha level.
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.DrawImage(bitmap, 0, 0);
            }
        }

        private static bool OpenImage(GerberProject project, string fullPathName, bool reloading, int index)
        {
            GerberImage parsedImage = null;
            string displayFileName = String.Empty;
            bool success = false;

            int numberOfAttributes = 0;
            List<GerberHIDAttribute> attributesList = null;

            if (Gerber.IsGerberRS427X(fullPathName))
                parsedImage = Gerber.ParseGerber(fullPathName);

            else if (Drill.IsDrillFile(fullPathName))
                parsedImage = Drill.ParseDrillFile(fullPathName, attributesList, numberOfAttributes, reloading);

            else
                throw new GerberFileException("Unknown file format " + fullPathName);

            if (parsedImage != null)
            {
                if (!reloading)
                {
                    project.FileInfo.Add(new GerberFileInformation());
                    AddFileToProject(project, parsedImage, fullPathName, reloading);
                }

                else
                {
                    AddFileToProject(project, parsedImage, fullPathName, reloading);
                }
            }

            return success;
        }

        private static void AddFileToProject(GerberProject project, GerberImage parsedImage, string fullPathName, bool reloading)
        {
            int fileIndex = project.FileInfo.Count - 1;
            int colorIndex = 0;
            GerberVerifyError error = GerberVerifyError.ImageOK;

            //Debug.WriteLine("Integrity check on image....\n");
            error = parsedImage.GerberImageVerify();
            if (error != GerberVerifyError.ImageOK)
            {
                project.FileInfo.RemoveAt(fileIndex);   // Image has errors, remove it from the file list.
                if ((error & GerberVerifyError.MissingNetList) > 0)
                    throw new GerberImageException("Missing image net list.");

                if ((error & GerberVerifyError.MissingFormat) > 0)
                    throw new GerberImageException("Missing format information in file.");

                if ((error & GerberVerifyError.MissingApertures) > 0)
                    throw new GerberImageException("Missing apertures/drill sizes.");

                if ((error & GerberVerifyError.MissingImageInfo) > 0)
                    throw new GerberImageException("Missing image information.");
            }

            project.FileInfo[fileIndex].Image = parsedImage;
            if (reloading) // If reload, just exchange the image and return.
                return;

            project.FileInfo[fileIndex].FullPathName = fullPathName;
            project.FileInfo[fileIndex].FileName = Path.GetFileName(fullPathName);
            colorIndex = defaultColorIndex % NumberOfDefaultColors;
            project.FileInfo[fileIndex].Color = Color.FromArgb(defaultColors[colorIndex, 1], defaultColors[colorIndex, 2], defaultColors[colorIndex, 3]);
            project.FileInfo[fileIndex].Alpha = defaultColors[colorIndex, 0];
            project.FileInfo[fileIndex].IsVisible = true;
            defaultColorIndex++;
        }

        private void RenderLayer(Graphics graphics, GerberProject project, RenderInformation renderInfo, int fileIndex)
        {
            GerberFileInformation fileInfo = project.FileInfo[fileIndex];
            int bmWidth = 0;
            int bmHeight = 0;

            // Calculate how big to make the bitmap back buffer.
            if (renderInfo.ImageWidth < renderInfo.DisplayWidth)
                bmWidth = (int)(renderInfo.DisplayWidth * graphics.DpiX);

            else
                bmWidth = (int)(renderInfo.ImageWidth * graphics.DpiX);

            if (renderInfo.ImageHeight < renderInfo.DisplayHeight)
                bmHeight = (int)(renderInfo.DisplayHeight * graphics.DpiY);

            else
                bmHeight = (int)(renderInfo.ImageHeight * graphics.DpiY);

            // Create a back buffer and draw to it with no alpha level.
            using (Bitmap bitmap = new Bitmap(bmWidth, bmHeight, graphics))
            using (Graphics backBuffer = Graphics.FromImage(bitmap))
            {
                backBuffer.CompositingMode = CompositingMode.SourceCopy;
                ScaleAndTranslate(backBuffer, renderInfo);
                
                // For testing.
                /*BoundingBox bb = GetProjectBounds(project);
                RectangleF r = new RectangleF((float)bb.Left, (float)bb.Top, (float)(bb.Right - bb.Left), (float)(bb.Top - bb.Bottom));
                GraphicsPath path = new GraphicsPath();
                path.AddLine((float)bb.Left, (float)bb.Bottom, (float)bb.Left, (float)(bb.Top));
                path.AddLine((float)bb.Left, (float)bb.Top, (float)bb.Right, (float)bb.Top);
                path.AddLine((float)bb.Right, (float)bb.Top, (float)bb.Right, (float)bb.Bottom);
                path.AddLine((float)bb.Right, (float)bb.Bottom, (float)bb.Left, (float)bb.Bottom);
                backBuffer.DrawPath(new Pen(Color.FromArgb(117, 200, 0, 0), 0.015f), path); */

                // Add transparency to the rendering color.
                Color foregroundColor = Color.FromArgb(fileInfo.Alpha, fileInfo.Color);
                GerberDraw.DrawImageToTarget(backBuffer, fileInfo, project.UserTransform, foregroundColor, backgroundColor);

                // Copy the back buffer to the visible surface with alpha level.
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.DrawImage(bitmap, 0, 0);
            }
        }

        private static void ScaleAndTranslate(Graphics graphics, RenderInformation renderInfo)
        {
            if (renderInfo.RenderType == GerberRenderQuality.Default)
            {
                graphics.SmoothingMode = SmoothingMode.Default;
            }

            else if (renderInfo.RenderType == GerberRenderQuality.HighQuality)
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
            }

            else
            {
                graphics.SmoothingMode = SmoothingMode.HighSpeed;
            }

            graphics.PageUnit = GraphicsUnit.Inch;
            double translateX = (renderInfo.Left * renderInfo.ScaleFactorX);
            double translateY = (renderInfo.Bottom * renderInfo.ScaleFactorY);
            //  Translate the draw area before drawing. We must translate the whole drawing down
            //  an additional displayHeight to account for the negative y flip done later.
            graphics.ScaleTransform(1, -1);
            graphics.TranslateTransform((float)(translateX + renderInfo.ScrollValueX), (float)(translateY - renderInfo.ScrollValueY));
            //graphics.TranslateTransform(-(float)(translateX - renderInfo.ScrollValueX), (float)(translateY + renderInfo.ImageHeight + renderInfo.ScrollValueY));
            //graphics.ScaleTransform((float)renderInfo.ScaleFactorY, -(float)renderInfo.ScaleFactorY);
        }

        // Check for a valid value.
        private bool IsNormal(double value)
        {
            return (!double.IsInfinity(value) && !double.IsNaN(value));
        }

        /// <summary>
        /// Adds a gerber object to the selection buffer if it lies within the selection region.
        /// </summary>
        /// <param name="selectionInfo">current selection info</param>
        /// <param name="image">gerber image containing the net</param>
        /// <param name="net">net to add to the selection info</param>
        public void ObjectInSelectedRegion(SelectionInformation selectionInfo, ref int i, Graphics graphics)
        {
            bool inSelect = false;
            GerberImage image = selectionInfo.SelectionImage;
            GerberNet net = image.GerberNetList[i];
            float x1 = (float)selectionInfo.LowerLeftX;
            float y1 = (float)selectionInfo.LowerLeftY;
            float x2 = (float)selectionInfo.UpperRightX;
            float y2 = (float)selectionInfo.UpperRightY;

            if (selectionInfo.SelectionType == GerberSelection.PointClick)
            {
                if (net.BoundingBox != null)
                {
                    if (!net.BoundingBox.Contains(new PointD(x1, y1)))
                        return;

                    if (net.ApertureState == GerberApertureState.Flash)
                        inSelect = net.BoundingBox.Contains(new PointD(x1, y1));

                    else if (net.ApertureState == GerberApertureState.On)
                    {
                        switch (net.Interpolation)
                        {
                            case GerberInterpolation.PolygonAreaStart:
                                inSelect = net.BoundingBox.Contains(new PointD(x1, y1));
                                break;

                            case GerberInterpolation.LinearX10:
                            case GerberInterpolation.LinearX1:
                            case GerberInterpolation.LinearX01:
                            case GerberInterpolation.LinearX001:
                                using (GraphicsPath gp = new GraphicsPath())
                                using (Pen pen = new Pen(Color.Transparent))
                                {
                                    pen.Width = (float)image.ApertureArray[net.Aperture].Parameters[0];
                                    pen.StartCap = pen.EndCap = LineCap.Round;
                                    PointF start = new PointF((float)(net.StartX), (float)(net.StartY));
                                    PointF end = new PointF((float)(net.StopX), (float)(net.StopY));
                                    gp.AddLine(start, end);
                                    if (gp.IsOutlineVisible(new PointF(x1, y1), pen, graphics))
                                        inSelect = true;

                                    break;
                                }

                            case GerberInterpolation.ClockwiseCircular:
                            case GerberInterpolation.CounterClockwiseCircular:
                                using (GraphicsPath gp = new GraphicsPath())
                                using (Pen pen = new Pen(Color.Transparent))
                                {
                                    float centerX = (float)net.CircleSegment.CenterX;
                                    float centerY = (float)net.CircleSegment.CenterY;
                                    float width = (float)net.CircleSegment.Width;
                                    float height = (float)net.CircleSegment.Height;
                                    float startAngle = (float)net.CircleSegment.StartAngle;
                                    float sweepAngle = (float)net.CircleSegment.SweepAngle;
                                    if (image.ApertureArray[net.Aperture].ApertureType == GerberApertureType.Rectangle)
                                        pen.StartCap = pen.EndCap = LineCap.Square;

                                    else
                                        pen.StartCap = pen.EndCap = LineCap.Round;

                                    RectangleF arcRectangle = new RectangleF(centerX - (width / 2), centerY - (height / 2), width, height);
                                    pen.Width = width;

                                    gp.AddArc(arcRectangle, startAngle, sweepAngle);
                                    if (gp.IsOutlineVisible(new PointF(x1, y1), pen, graphics))
                                        inSelect = true;
                                }
                                break;
                        }
                    }
                }
            }

            else if (selectionInfo.SelectionType == GerberSelection.DragBox)
            {
                if (net.BoundingBox != null)
                {
                    double left = Math.Min(selectionInfo.LowerLeftX, selectionInfo.UpperRightX);
                    double right = Math.Max(selectionInfo.LowerLeftX, selectionInfo.UpperRightX);
                    double top = Math.Min(selectionInfo.LowerLeftY, selectionInfo.UpperRightY);
                    double bottom = Math.Max(selectionInfo.LowerLeftY, selectionInfo.UpperRightY);

                    BoundingBox box = new BoundingBox(left, bottom, right, top);
                    if (!box.Contains(net.BoundingBox))
                        return;

                    if (net.ApertureState == GerberApertureState.Flash)
                        inSelect = box.Contains(net.BoundingBox);

                    else if (net.ApertureState == GerberApertureState.On)
                        inSelect = box.Contains(net.BoundingBox);
                }
            }

            if (inSelect)
            {
                selectionInfo.SelectedNetList.Add(net);
                selectionInfo.SelectionCount++;
                if (net.Interpolation == GerberInterpolation.PolygonAreaStart)  // Add all the poly points.
                {
                    do
                    {
                        i++;
                        net = image.GerberNetList[i];
                        selectionInfo.SelectedNetList.Add(net);
                    } while (net.Interpolation != GerberInterpolation.PolygonAreaEnd);
                }
            }
        }

        /// <summary>
        /// Clear the selection buffer.
        /// </summary>
        /// <param name="selectionInfo">selection info</param>
        public void ClearSelectionBuffer(SelectionInformation selectionInfo)
        {
            if (selectionInfo.SelectedNetList.Count > 0)
                selectionInfo.ClearSelectionList();
        }

        // Test if the net is already selected.
        /*private bool ObjectInSelectionBuffer(GerberNet gerberNet, SelectionInformation selectionInfo)
        {
            foreach (GerberNet net in selectionInfo.SelectedNetList)
            {
                if (net == gerberNet)
                    return true;
            }

            return false;
        }*/

        private RectangleF BoundingBoxToRectangle(GerberNet net)
        {
            float sx = (float)net.BoundingBox.Left;
            float sy = (float)net.BoundingBox.Top;
            float width = (float)(net.BoundingBox.Right - net.BoundingBox.Left);
            float height = (float)(net.BoundingBox.Top - net.BoundingBox.Bottom);

            rect = new RectangleF(sx, sy, width, height);
            return rect;
        }
    }
}





