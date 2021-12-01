/* LibGerberVS.cs - Main Library file. */

/*  Copyright (C) 2015-2021 Milton Neal <milton200954@gmail.com>
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
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;

[assembly: CLSCompliant(true)]
namespace GerberVS
{
    /// <summary>
    /// Gerber process library.
    /// </summary>
    public class LibGerberVS
    {
        private const int NumberOfDefaultColors = 18;
        private static int defaultColorIndex = 0;
        private static Color backgroundColor;
        private static readonly int[,] defaultColors = {
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

        /// <summary>
        /// Creates a new Gerber Project.
        /// </summary>
        /// <returns>the newly created project</returns>
        public GerberProject CreateNewProject()
        {
            GerberProject project = new GerberProject();
            defaultColorIndex = 0;
            return project;
        }

        /// <summary>
        /// Opens a layer and appends it to the project.
        /// </summary>
        /// <param name="project">project to append file</param>
        /// <param name="fullPathName">file to open</param>
        /// <returns></returns>
        public void OpenLayerFromFileName(GerberProject project, string fullPathName)
        {
            bool reload = false;

            try
            {
                OpenImage(project, fullPathName, reload, -1);
            }

            catch (Exception ex)
            {
                throw new GerberDllException("", ex);
            }

        }

        /// <summary>
        /// Opens a layer and appends it to the project using the specified color and alpha level.
        /// </summary>
        /// <param name="project">project</param>
        /// <param name="fullPathName">file path</param>
        /// <param name="color">layer color</param>
        public void OpenLayerFromFileNameAndColor(GerberProject project, string fullPathName, Color color)
        {
            bool reload = false;
            int fileIndex;
            try
            {
                OpenImage(project, fullPathName, reload, -1);
                fileIndex = project.FileCount - 1;
                project.FileInfo[fileIndex].LayerDirty = false;
                project.FileInfo[fileIndex].Color = color;
            }

            catch (Exception ex)
            {
                throw new GerberDllException("", ex);
            }
        }

        /// <summary>
        /// Saves the current image layer to the specified file.
        /// </summary>
        /// <param name="project">current project</param>
        /// <param name="index">index of the current layer</param>
        /// <param name="fullPathName">path and filename to save the file</param>
        public void SaveLayerFromIndex(GerberProject project, int index, string fullPathName)
        {
            GerberFileType fileType = project.FileInfo[index].Image.FileType;
            SaveLayerFromIndex(project, index, fileType, fullPathName);
        }

        /// <summary>
        /// Saves the current image layer to the specified file.
        /// </summary>
        /// <param name="project">current project</param>
        /// <param name="index">index of the current layer</param>
        /// <param name="fileType"> file type to save the image as</param>
        /// <param name="fullPathName">path and filename to save the file</param>
        public void SaveLayerFromIndex(GerberProject project, int index, GerberFileType fileType, string fullPathName)
        {
            if (fileType == GerberFileType.RS274X)
            {
                WriteGerberRS274X.RS274XFromImage(fullPathName, project.FileInfo[index].Image, project.FileInfo[index].UserTransform);
            }

            else if(fileType == GerberFileType.Drill)
            {
                WriteExcellonDrill.DrillFileFromImage(fullPathName, project.FileInfo[index].Image, project.FileInfo[index].UserTransform);
            }

            project.FileInfo[index].LayerDirty = false;
        }

        /// <summary>
        /// Reloads an existing layer within the current project.
        /// </summary>
        /// <param name="project">project</param>
        /// <param name="index">project file index to reload</param>
        public void ReloadLayer(GerberProject project, int index)
        {
            bool reload = true;

            try
            {
                OpenImage(project, project.FileInfo[index].FullPathName, reload, index);
                project.FileInfo[index].LayerDirty = false;
            }

            catch (Exception ex)
            {
                throw new GerberDllException("", ex);
            }
        }

        /// <summary>
        /// Reloads all existing layers within the current project.
        /// </summary>
        /// <param name="project">gerber project</param>
        public void ReloadAllLayers(GerberProject project)
        {
            for (int i = 0; i < project.FileCount; i++)
            {
                if (project.FileInfo[i] != null && !String.IsNullOrEmpty(project.FileInfo[i].FullPathName))
                    ReloadLayer(project, i);
            }
        }


        /// <summary>
        /// Removes a file from the project at the specified index.
        /// </summary>
        /// <param name="project">project containing the file info to remove</param>
        /// <param name="index">index to remove at</param>
        public void UnloadLayer(GerberProject project, int index)
        {
            int count = project.FileCount;

            // Move all later layers down to fill the empty slot.
            for (int i = index; i < count - 1; i++)
                project.FileInfo[i] = project.FileInfo[i + 1];

            // Make sure the final spot is clear.
            project.FileInfo.Remove(project.FileInfo[count - 1]);
            project.FileCount--;
        }

        /// <summary>
        /// Removes all layers from the project.
        /// </summary>
        /// <param name="project">project containing the file list.</param>
        public void UnloadAllLayers(GerberProject project)
        {
            int fileIndex = project.FileCount - 1;
            // Must count down since UnloadLayer collapses layers down. Otherwise, layers slide past the index,
            for (int index = fileIndex; index >= 0; index--)
            {
                if (project.FileInfo[index] != null && !String.IsNullOrEmpty(project.FileInfo[index].FileName))
                    UnloadLayer(project, index);
            }
        }

        /// <summary>
        /// Changes the order that layers are rendered.
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

        private static void AddFileToProject(GerberProject project, GerberImage parsedImage, string fullPathName, bool reloading, int index)
        {
            int colorIndex = 0;
            GerberVerifyErrors error = GerberVerifyErrors.None;
            GerberFileInformation fileInfo = project.FileInfo[index];

            //Debug.WriteLine("Integrity check on image....\n");
            error = parsedImage.ImageVerify();
            if (error != GerberVerifyErrors.None)
            {
                project.FileInfo.RemoveAt(index);   // Image has errors, remove it from the file list and throw exception.
                project.FileCount--;
                if ((error & GerberVerifyErrors.MissingNetList) > 0)
                    throw new GerberImageException("Missing image net list.");

                if ((error & GerberVerifyErrors.MissingFormat) > 0)
                    throw new GerberImageException("Missing format information in file.");

                if ((error & GerberVerifyErrors.MissingApertures) > 0)
                    throw new GerberImageException("Missing aperture/drill sizes.");

                if ((error & GerberVerifyErrors.MissingImageInfo) > 0)
                    throw new GerberImageException("Missing image information.");
            }

            fileInfo.Image = parsedImage;
            if (reloading) // If reloading, just exchange the image and return.
                return;

            fileInfo.FullPathName = fullPathName;
            fileInfo.FileName = Path.GetFileName(fullPathName);
            colorIndex = defaultColorIndex % NumberOfDefaultColors;
            fileInfo.Color = Color.FromArgb(defaultColors[colorIndex, 0], defaultColors[colorIndex, 1],
                                                           defaultColors[colorIndex, 2], defaultColors[colorIndex, 3]);
            fileInfo.IsVisible = true;
            defaultColorIndex++;
        }

        private static void OpenImage(GerberProject project, string fullPathName, bool reloading, int index)
        {
            GerberImage parsedImage = null;

            if (Gerber.IsGerberRS427X(fullPathName))
                parsedImage = Gerber.ParseGerber(fullPathName);

            else if (Drill.IsDrillFile(fullPathName))
                parsedImage = Drill.ParseDrillFile(fullPathName, reloading);

            else
                throw new GerberFileException("Unknown file type: " + Path.GetFileName(fullPathName));

            if (parsedImage != null)
            {
                if (!reloading)
                {
                    project.FileInfo.Add(new GerberFileInformation());
                    project.FileCount++;
                    index = project.FileCount - 1;
                }

                AddFileToProject(project, parsedImage, fullPathName, reloading, index);
            }
        }

        /// <summary>
        /// Creates the Gerber image holding all the geometry for the layer.
        /// </summary>
        /// <param name="filePath">filename containing the layer geometry</param>
        /// <returns>gerber image</returns>
        public GerberImage CreateRS274XImageFromFile(string filePath)
        {
            GerberImage returnImage;

            returnImage = Gerber.ParseGerber(filePath);
            return returnImage;
        }

        /// <summary>
        /// Creates the Gerber image holding all the geometry for the drill layer.
        /// </summary>
        /// <param name="filePath">filename containing the excellon drill geometry</param>
        /// <returns>gerber image</returns>
        public GerberImage CreateExcellonImageFromFile(string filePath)
        {
            GerberImage returnImage;

            returnImage = Drill.ParseDrillFile(filePath);
            return returnImage;
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

            for (int i = 0; i < project.FileCount; i++)
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
                        double scaleX = project.FileInfo[i].UserTransform.ScaleX;
                        double scaleY = project.FileInfo[i].UserTransform.ScaleY;
                        if (project.FileInfo[i].UserTransform.MirrorAroundX)
                            scaleY *= -1;

                        if (project.FileInfo[i].UserTransform.MirrorAroundY)
                            scaleX *= -1;

                        fullMatrix.Scale((float)scaleX, (float)scaleY);
                        fullMatrix.Rotate((float)project.FileInfo[i].UserTransform.Rotation);
                        fullMatrix.Translate((float)project.FileInfo[i].UserTransform.TranslateX, (float)project.FileInfo[i].UserTransform.TranslateY);
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
            RectangleF projectRectangle = new RectangleF((float)x1, (float)y2, (float)(x2 - x1), (float)(y2 - y1));
            return boundingbox;
        }

        /// <summary>
        /// Adds a gerber object to the selection buffer if it lies within the selection region.
        /// </summary>
        /// <param name="graphics">target where the image is rendered</param>
        /// <param name="selectionInfo">user selection information</param>
        /// <param name="index">current index of the gerber net to test</param>
        public void ObjectInSelectedRegion(Graphics graphics, SelectionInformation selectionInfo, ref int index)
        {
            if (selectionInfo.SelectionType == GerberSelection.PointClick)
                GerberSelectRegion.PointInObject(graphics, selectionInfo, ref index);

            else
                GerberSelectRegion.ObjectsInRegion(graphics, selectionInfo, ref index);
        }

        /// <summary>
        /// Applies offsets to move the image within the display area.
        /// </summary>
        /// <param name="project">project data</param>
        /// <param name="renderInfo">rendering information</param>
        public void TranslateToFitDisplay(GerberProject project, GerberRenderInformation renderInfo)
        {
            BoundingBox bb = GetProjectBounds(project);
            if (!bb.IsValid())
                return;

            double left = (bb.Left * renderInfo.ScaleFactorX) - 0.05f;
            double bottom = (bb.Bottom * renderInfo.ScaleFactorY) - 0.05f;
            double right = (bb.Right * renderInfo.ScaleFactorX) + 0.05f;
            double top = (bb.Top * renderInfo.ScaleFactorY) + 0.05f;

            renderInfo.LowerLeftX = -left;
            renderInfo.LowerLeftY = -top;

            renderInfo.ImageWidth = right - left;
            renderInfo.ImageHeight = top - bottom;
        }

        /// <summary>
        /// Scales and translates the image to fit centrally within the display area.
        /// </summary>
        /// <param name="project">project data</param>
        /// <param name="renderInfo">rendering information</param>
        public void ZoomToFitDisplay(GerberProject project, GerberRenderInformation renderInfo)
        {
            double width, height;
            double scaleX, scaleY;
            double left, bottom;

            BoundingBox bb = GetProjectBounds(project);
            if (!bb.IsValid())
                return;

            width = bb.Right - bb.Left;
            height = bb.Top - bb.Bottom;
            scaleX = renderInfo.DisplayWidth / (width * 1.05);
            scaleY = renderInfo.DisplayHeight / (height * 1.05);
            renderInfo.ScaleFactorX = renderInfo.ScaleFactorY = Math.Min(scaleX, scaleY);

            width *= renderInfo.ScaleFactorX;
            height *= renderInfo.ScaleFactorY;
            left = bb.Left * renderInfo.ScaleFactorX;
            renderInfo.LowerLeftX = ((renderInfo.DisplayWidth - width) / 2) - left;
            bottom = bb.Bottom * renderInfo.ScaleFactorY;
            renderInfo.LowerLeftY = -((renderInfo.DisplayHeight + height) / 2) - bottom;

            renderInfo.ImageWidth = width;
            renderInfo.ImageHeight = height;
        }

        /// <summary>
        /// Applies offsets to centre the image within the display area.
        /// </summary>
        /// <param name="project">project data</param>
        /// <param name="renderInfo">rendering information</param>
        public void TranslateToCenter(GerberProject project, GerberRenderInformation renderInfo)
        {
            double width, height;
            double left, bottom, right, top;

            BoundingBox bb = GetProjectBounds(project);
            if (!bb.IsValid())
                return;

            left = (bb.Left * renderInfo.ScaleFactorX) - 0.05f;
            bottom = (bb.Bottom * renderInfo.ScaleFactorY) - 0.05f;
            right = (bb.Right * renderInfo.ScaleFactorX) + 0.05f;
            top = (bb.Top * renderInfo.ScaleFactorY) + 0.05f;

            width = right - left;
            height = top - bottom;
            renderInfo.LowerLeftX = ((renderInfo.DisplayWidth - width) / 2) - left;
            renderInfo.LowerLeftY = -((renderInfo.DisplayHeight + height) / 2) - bottom;
            if (width > renderInfo.DisplayWidth)
                renderInfo.LowerLeftX = -left;

            if (height > renderInfo.DisplayHeight)
                renderInfo.LowerLeftY = -(height + bottom);

            renderInfo.ImageWidth = width;
            renderInfo.ImageHeight = height;
        }

        /// <summary>
        /// Creates a selection information object for the user selection layer data.
        /// </summary>
        /// <param name="fileInfo">file information</param>
        /// <returns>a new instance of the selection info </returns>
        public SelectionInformation CreateSelectionLayer(GerberFileInformation fileInfo)
        {
            SelectionInformation selectionInfo = new SelectionInformation(fileInfo);
            selectionInfo.FileInfo.Image = GerberImage.Copy(fileInfo.Image);
            return selectionInfo;
        }

        /// <summary>
        /// Renders all the visible layers contained within the project.
        /// </summary>
        /// <param name="graphics">surface to render the image</param>
        /// <param name="project">project containing the files to render</param>
        /// <param name="renderInfo">information for positioning, scaling and translating</param>
        public void RenderAllLayers(Graphics graphics, GerberProject project, GerberRenderInformation renderInfo)
        {
            int fileCount = project.FileInfo.Count;
            backgroundColor = project.BackgroundColor;
            graphics.Clear(backgroundColor);

            for (int i = fileCount - 1; i >= 0; i--)
            {
                if (project.FileInfo[i] != null && project.FileInfo[i].IsVisible)
                    RenderLayer(graphics, project.FileInfo[i], null, renderInfo);
            }

            // For testing :- draws a bounding rectangle.
            /*GraphicsState gs = graphics.Save();
            ScaleAndTranslate(graphics, renderInfo);
            BoundingBox bb = GetProjectBounds(project);
            RectangleF r = new RectangleF((float)bb.Left, (float)bb.Top, (float)(bb.Right - bb.Left), (float)(bb.Top - bb.Bottom));
            GraphicsPath path = new GraphicsPath();
            path.AddLine((float)bb.Left, (float)bb.Bottom, (float)bb.Left, (float)(bb.Top));
            path.AddLine((float)bb.Left, (float)bb.Top, (float)bb.Right, (float)bb.Top);
            path.AddLine((float)bb.Right, (float)bb.Top, (float)bb.Right, (float)bb.Bottom);
            path.AddLine((float)bb.Right, (float)bb.Bottom, (float)bb.Left, (float)bb.Bottom);
            graphics.DrawPath(new Pen(Color.White, 0.015f / (float)renderInfo.ScaleFactorX), path);
            graphics.Restore(gs);*/
        }

        /// <summary>
        /// Renders the layer containing the user selected objects.
        /// </summary>
        /// <param name="graphics">surface to render the image</param>
        /// <param name="selectionInfo">selection info of nets to render</param>
        /// <param name="renderInfo">information for positioning, scaling and translating</param>
        public void RenderSelectionLayer(Graphics graphics, SelectionInformation selectionInfo, GerberRenderInformation renderInfo)
        {
            if (selectionInfo == null)
                return;

            RenderLayer(graphics, selectionInfo.FileInfo, selectionInfo, renderInfo);
        }

        /// <summary>
        /// Draws all the visible layers contained within the project.
        /// </summary>
        /// <param name="graphics">surface to render the image</param>
        /// <param name="project">project containing the files to render</param>
        /// <param name="renderInfo">information for positioning, scaling and translating</param>
        public void RenderAllLayersForVectorOutput(Graphics graphics, GerberProject project, GerberRenderInformation renderInfo)
        {
            int fileIndex = project.FileInfo.Count - 1;

            ScaleAndTranslate(graphics, renderInfo);
            for (int i = fileIndex; i >= 0; i--)
            {
                if (project.FileInfo[i] != null && project.FileInfo[i].IsVisible)
                {
                    GraphicsState state = graphics.Save();
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    RenderLayerToTarget(graphics, project.FileInfo[i], null);
                    graphics.Restore(state);
                }
            }
        }

        private void RenderLayer(Graphics graphics, GerberFileInformation fileInfo, SelectionInformation selectionInfo, GerberRenderInformation renderInfo)
        {
            Size bmSize = GetBitmapSize(graphics, renderInfo);
            // Create a back buffer and draw to it with no alpha level.
            using (Bitmap bitmap = new Bitmap(bmSize.Width, bmSize.Height, graphics))
            using (Graphics backBuffer = Graphics.FromImage(bitmap))
            {
                backBuffer.CompositingMode = CompositingMode.SourceCopy;
                ScaleAndTranslate(backBuffer, renderInfo);
                RenderLayerToTarget(backBuffer, fileInfo, selectionInfo);
                // Copy the back buffer to the visible surface with alpha transparency level.
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.DrawImage(bitmap, 0, 0);
            }
        }

        private static void RenderLayerToTarget(Graphics graphics, GerberFileInformation fileInfo, SelectionInformation selectionInfo)
        {
            // Add transparency to the rendering color.
            Color foregroundColor = fileInfo.Color;
            if (selectionInfo != null)
                foregroundColor = Color.FromArgb(177, Color.White);

            GerberDraw.RenderImageToTarget(graphics, fileInfo.Image, selectionInfo, fileInfo.UserTransform, foregroundColor, backgroundColor);
        }

        private static void ScaleAndTranslate(Graphics graphics, GerberRenderInformation renderInfo)
        {
            if (renderInfo.RenderQuality == GerberRenderQuality.Default)
                graphics.SmoothingMode = SmoothingMode.Default;

            else if (renderInfo.RenderQuality == GerberRenderQuality.HighQuality)
                graphics.SmoothingMode = SmoothingMode.HighQuality;

            else
                graphics.SmoothingMode = SmoothingMode.HighSpeed;

            graphics.PageUnit = GraphicsUnit.Inch;
            //  Translate the draw area before drawing. We must translate the whole drawing down.
            graphics.ScaleTransform(1, -1);
            graphics.TranslateTransform((float)renderInfo.LowerLeftX, (float)renderInfo.LowerLeftY);
            graphics.ScaleTransform((float)renderInfo.ScaleFactorX, (float)renderInfo.ScaleFactorY);
        }

        // Calculate how big to make the bitmap back buffer.
        private static Size GetBitmapSize(Graphics graphics, GerberRenderInformation renderInfo)
        {
            Size bmSize = new Size(0, 0);

            if (renderInfo.ImageWidth < renderInfo.DisplayWidth)
                bmSize.Width = (int)(renderInfo.DisplayWidth * graphics.DpiX);

            else
                bmSize.Width = (int)(renderInfo.ImageWidth * graphics.DpiX);

            if (renderInfo.ImageHeight < renderInfo.DisplayHeight)
                bmSize.Height = (int)(renderInfo.DisplayHeight * graphics.DpiY);

            else
                bmSize.Height = (int)(renderInfo.ImageHeight * graphics.DpiY);

            //bmSize.Width = (int)((renderInfo.ImageWidth * graphics.DpiX) * renderInfo.ScaleFactorX);
            //bmSize.Height = (int)((renderInfo.ImageHeight * graphics.DpiY) * renderInfo.ScaleFactorY);
            return bmSize;
        }

        // Image justify and image offset are depreciated, but still supporting it for now.
        internal static PointD GetImageOffsets(GerberImage image)
        {
            double offSetX = 0.0;
            double offSetY = 0.0;

            offSetX = image.ImageInfo.ImageJustifyOffsetActualA;
            offSetY = (float)image.ImageInfo.ImageJustifyOffsetActualB;
            offSetX += image.ImageInfo.OffsetA;
            offSetY += image.ImageInfo.OffsetB;

            return new PointD(offSetX, offSetY);
        }

        // Check for a valid value.
        private static bool IsNormal(double value)
        {
            return (!double.IsInfinity(value) && !double.IsNaN(value));
        }
    }
}





