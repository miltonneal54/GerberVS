using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace GerberVS
{
    /// <summary>
    /// Creates a Png image of a gerber project.
    /// </summary>
    public static class ExportProjectAsPng
    {
        /// <summary>
        /// Exports a gerber project to a Png image.
        /// </summary>
        /// <param name="filePath">Full path name to write file to</param>
        /// <param name="project">project info</param>
        /// <param name="renderInfo">render information</param>
        public static void ProjectToPng(string filePath, GerberProject project, GerberRenderInformation renderInfo, Graphics g)
        {
            try
            {
                int fileIndex = project.FileInfo.Count - 1;
                int width = (int)(renderInfo.ImageWidth * g.DpiX);
                int height = (int)(renderInfo.ImageHeight * g.DpiY);

                using (Bitmap bitmap = new Bitmap(width, height))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    for (int i = fileIndex; i >= 0; i--)
                    {
                        ;
                        //if (project.FileInfo[i] != null && project.FileInfo[i].IsVisible)
                        //LibGerberVS.RenderLayer(graphics, project.FileInfo[i], renderInfo, project);
                    }

                    bitmap.Save(filePath, ImageFormat.Png);
                }
            }

            catch (Exception ex)
            {
                throw new GerberExportException(Path.GetFileName(filePath), ex);
            }
        }
    }
}
