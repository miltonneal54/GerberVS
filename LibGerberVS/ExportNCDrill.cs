using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GerberVS
{
    /// <summary>
    /// Create a Excellon drill file from a gerber image.
    /// </summary>
    public static class ExportExcellonDrill
    {
        /// <summary>
        /// Export a gerber image to NC drill file format.
        /// </summary>
        /// <param name="fullPathName">Full path name to write file to</param>
        /// <param name="inputImage">gerber image to export</param>
        /// <returns></returns>
        public static bool RS274xFromImage(string fullPathName, GerberImage inputImage)
        {
            UserTransform transform = new UserTransform(0, 0, 1, 1, 0, false, false, false);
            return DrillFileFromImage(fullPathName, inputImage, transform);
        }

        /// <summary>
        /// Export a gerber image to NC file format with user tranformations.
        /// </summary>
        /// <param name="fullPathName">Full path name to write file to</param>
        /// <param name="inputImage">gerber image to export</param>
        /// <param name="transform">apply the user transformations</param>
        /// <returns></returns>
        public static bool DrillFileFromImage(string fullPathName, GerberImage inputImage, UserTransform transform)
        {
            List<int> apertureTable = new List<int>();
            GerberNet currentNet;
            try
            {
                using (StreamWriter streamWriter = new StreamWriter(fullPathName, false, Encoding.ASCII))
                {

                    // Copy the image, cleaning it in the process.
                    GerberImage newImage = GerberImage.Copy(inputImage);
                    // Write header info.
                    streamWriter.WriteLine("M48");
                    streamWriter.WriteLine("INCH,TZ");

                    // Define all apertures.
                    Aperture currentAperture;
                    for (int i = 0; i < Gerber.MaximumApertures; i++)
                    {
                        currentAperture = newImage.ApertureArray[i];
                        if (currentAperture == null)
                            continue;

                        switch (currentAperture.ApertureType)
                        {
                            case GerberApertureType.Circle:
                                streamWriter.WriteLine("T{0:00}C{1:0.000}", i, currentAperture.Parameters[0]);
                                // Add the "approved" aperture to our valid list.
                                apertureTable.Add(i);
                                break;
                            default:
                                break;
                        }
                    }

                    streamWriter.WriteLine("M95");    // End of header.
                    // Write rest of image
                    for (int i = 0; i < apertureTable.Count; i++)
                    {
                        int apertureIndex = apertureTable[i];

                        // Write tool change.
                        streamWriter.WriteLine("T{0:00}", apertureIndex);

                        // Run through all nets and look for drills using this aperture.
                        for (int netIndex = 0; netIndex < newImage.GerberNetList.Count; netIndex++)
                        {
                            currentNet = newImage.GerberNetList[netIndex];
                            if (currentNet.Aperture != apertureIndex)
                                continue;

                            switch (currentNet.ApertureState)
                            {
                                case GerberApertureState.Flash:
                                    streamWriter.WriteLine("X{0:000000}Y{1:000000}", Math.Round(currentNet.StopX * 10000.0), Math.Round(currentNet.StopY * 10000.0));
                                    break;

                                case GerberApertureState.On:	// Cut slot.
                                    streamWriter.WriteLine("X{0:000000}Y{1:000000}G85X{2:000000}Y{3:000000}",
                                                           Math.Round(currentNet.StartX * 10000.0),
                                                           Math.Round(currentNet.StartY * 10000.0),
                                                           Math.Round(currentNet.StopX * 10000.0),
                                                           Math.Round(currentNet.StopY * 10000.0));
                                    break;

                                default:
                                    break;
                            }
                        }
                    }

                    // Write footer.
                    streamWriter.WriteLine("M30");
                    return true;
                }
            }

            catch (Exception ex)
            {
                throw new GerberExportException(Path.GetFileName(fullPathName), ex);
            }
        }
    }
}
