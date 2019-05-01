using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Xml;

using GerberVS;

namespace GerberView
{
    class Project
    {
        // Saves the device library to an XML file.
        public static void WriteProject(GerberProject project)
        {
            string projectName = project.ProjectName;
            int count = project.FileInfo.Count;

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineOnAttributes = true;

            using (XmlWriter writer = XmlWriter.Create(projectName + ".gpf", settings))
            {
                writer.WriteStartDocument();

                writer.WriteStartElement("ProjectInformation");
                writer.WriteElementString("ProjectName", project.ProjectName);
                writer.WriteElementString("CurrentIndex", project.CurrentIndex.ToString());
                writer.WriteElementString("BackgroundColor", (Convert.ToInt32(project.BackgroundColor.ToArgb())).ToString());

                writer.WriteStartElement("ProjectTransformation");
                
                writer.WriteElementString("ScaleX", project.UserTransform.ScaleX.ToString());
                writer.WriteElementString("ScaleY", project.UserTransform.ScaleY.ToString());
                //writer.WriteElementString("Inverted", project.UserTransform.Inverted.ToString());
                writer.WriteEndElement();

                writer.WriteStartElement("FileInformation");
                for (int i = 0; i < count; i++)
                {
                    writer.WriteElementString("FileName", project.FileInfo[i].FileName);
                    writer.WriteElementString("FilePath", project.FileInfo[i].FullPathName);
                    writer.WriteElementString("LayerColor", (Convert.ToInt32(project.FileInfo[i].Color.ToArgb())).ToString());
                    writer.WriteElementString("LayerAlpha", project.FileInfo[i].Alpha.ToString());
                    writer.WriteElementString("LayerVisible", project.FileInfo[i].IsVisible.ToString());
                    
                    //writer.WriteEndElement();
                }

                writer.WriteEndElement();

                writer.WriteEndDocument();
                writer.Flush();
                writer.Close();
            }
        }
    }
}
