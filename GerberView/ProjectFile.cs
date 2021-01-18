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

    /// <summary>
    /// Project file write exception class.
    /// </summary>
    [Serializable]
    public class ProjectWriteException : System.Exception
    {
        private static string baseMessage = "Project file write error.";
        /// <summary>
        /// Initialses a new instance of ProjectWriteException class.
        /// </summary>
        public ProjectWriteException()
            : base(baseMessage)
        { }

        /// <summary>
        /// Initialses a new instance of ProjectWriteException class with a specified error message.
        /// </summary>
        /// <param name="message">exception message</param>
        public ProjectWriteException(string message)
            : base(baseMessage + Environment.NewLine + message)
        { }

        /// <summary>
        /// Initialses a new instance of ProjectWriteException class with a specified error message and a reference to the inner exception.
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="innerException">inner exception</param>
        public ProjectWriteException(string message, Exception innerException)
            : base(baseMessage + Environment.NewLine + message, innerException)
        { }
    }

    /// <summary>
    /// Project file read exception class.
    /// </summary>
    [Serializable]
    public class ProjectReadException : System.Exception
    {
        private static string baseMessage = "Project file read error.";
        /// <summary>
        /// Initialses a new instance of ProjectReadException class.
        /// </summary>
        public ProjectReadException()
            : base(baseMessage)
        { }

        /// <summary>
        /// Initialses a new instance of ProjectReadException class with a specified error message.
        /// </summary>
        /// <param name="message">exception message</param>
        public ProjectReadException(string message)
            : base(baseMessage + Environment.NewLine + message)
        { }

        /// <summary>
        /// Initialses a new instance of ProjectReadException class with a specified error message and a reference to the inner exception.
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="innerException">inner exception</param>
        public ProjectReadException(string message, Exception innerException)
            : base(baseMessage + Environment.NewLine + message, innerException)
        { }

    }
    class ProjectFile
    {

        /// <summary>
        /// Writes a gerber project file in Xml format.
        /// </summary>
        /// <param name="project">filename of project</param>
        public static void WriteProject(GerberProject project)
        {
            int count = project.FileInfo.Count;
            string filePath = project.Path;

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineOnAttributes = true;

            try
            {
                using (XmlWriter writer = XmlWriter.Create(filePath, settings))
                {

                    // Project.
                    writer.WriteStartDocument();
                    writer.WriteStartElement("ProjectInformation");
                    writer.WriteElementString("ProjectName", project.ProjectName);
                    writer.WriteElementString("Path", project.Path);
                    writer.WriteElementString("FileCount", project.FileCount.ToString());
                    writer.WriteElementString("CurrentIndex", project.CurrentIndex.ToString());
                    writer.WriteElementString("BackgroundColor", (Convert.ToInt32(project.BackgroundColor.ToArgb())).ToString());
                    writer.WriteElementString("CheckBeforeDelete", project.CheckBeforeDelete.ToString());
                    writer.WriteElementString("ShowHiddenSelection", project.ShowHiddenSelection.ToString());

                    for (int i = 0; i < count; i++)
                    {
                        writer.WriteStartElement("LayerInformation");
                        writer.WriteElementString("FileName", project.FileInfo[i].FileName);
                        writer.WriteElementString("FilePath", project.FileInfo[i].FullPathName);
                        writer.WriteElementString("LayerColor", (Convert.ToInt32(project.FileInfo[i].Color.ToArgb())).ToString());
                        writer.WriteElementString("LayerAlpha", project.FileInfo[i].Alpha.ToString());
                        writer.WriteElementString("LayerVisible", project.FileInfo[i].IsVisible.ToString());

                        // User transform.
                        writer.WriteElementString("TranslateX", project.FileInfo[i].UserTransform.TranslateX.ToString());
                        writer.WriteElementString("TranslateY", project.FileInfo[i].UserTransform.TranslateY.ToString());
                        writer.WriteElementString("ScaleX", project.FileInfo[i].UserTransform.ScaleX.ToString());
                        writer.WriteElementString("ScaleY", project.FileInfo[i].UserTransform.ScaleY.ToString());
                        writer.WriteElementString("Rotation", project.FileInfo[i].UserTransform.Rotation.ToString());
                        writer.WriteElementString("MirrorAroundX", project.FileInfo[i].UserTransform.MirrorAroundX.ToString());
                        writer.WriteElementString("MirrorAroundY", project.FileInfo[i].UserTransform.MirrorAroundY.ToString());
                        writer.WriteElementString("Inverted", project.FileInfo[i].UserTransform.Inverted.ToString());
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Flush();
                }
            }

            catch (Exception ex)
            {
                throw new ProjectWriteException("Error writing project file.", ex);
            }

        }

        /// <summary>
        /// Creates a gerber project from an xlm project file.
        /// </summary>
        /// <param name="project">gerber project</param>
        /// <param name="projectFile">name of the project file</param>
        public static void ReadProject(GerberProject project, string projectFile)
        {
            GerberFileInformation fileInfo = new GerberFileInformation();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineOnAttributes = true;

            try
            {
                using (XmlReader reader = XmlReader.Create(projectFile))
                {

                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            while (reader.NodeType != XmlNodeType.EndElement)
                            {
                                reader.Read();
                                if (reader.Name == "ProjectName")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            project.ProjectName = reader.Value;
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "Path")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            project.Path = reader.Value;
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "FileCount")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            project.FileCount = int.Parse(reader.Value);
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "CurrentIndex")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            project.CurrentIndex = int.Parse(reader.Value);
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "BackgroundColor")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            project.BackgroundColor = Color.FromArgb(int.Parse(reader.Value));
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "CheckBeforeDelete")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            project.CheckBeforeDelete = bool.Parse(reader.Value);
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "ShowHiddenSelection")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            project.ShowHiddenSelection = bool.Parse(reader.Value);
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "FileName")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            fileInfo.FileName = reader.Value;
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "FilePath")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            fileInfo.FullPathName = reader.Value;
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "LayerColor")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            fileInfo.Color = Color.FromArgb(int.Parse(reader.Value));
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "LayerAlpha")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            fileInfo.Alpha = int.Parse(reader.Value);
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "LayerVisible")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            fileInfo.IsVisible = bool.Parse(reader.Value);
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "Inverted")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            fileInfo.UserTransform.Inverted = bool.Parse(reader.Value);
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "TranslateX")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            fileInfo.UserTransform.TranslateX = double.Parse(reader.Value);
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "TranslateY")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            fileInfo.UserTransform.TranslateY = double.Parse(reader.Value);
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "ScaleX")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            fileInfo.UserTransform.ScaleX = double.Parse(reader.Value);
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "ScaleY")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            fileInfo.UserTransform.ScaleY = double.Parse(reader.Value);
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "Rotation")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            fileInfo.UserTransform.Rotation = double.Parse(reader.Value);
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "MirrorAroundX")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            fileInfo.UserTransform.MirrorAroundX = bool.Parse(reader.Value);
                                    }

                                    reader.Read();
                                }

                                if (reader.Name == "MirrorAroundY")
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                        if (reader.NodeType == XmlNodeType.Text)
                                            fileInfo.UserTransform.MirrorAroundY = bool.Parse(reader.Value);
                                    }

                                    reader.Read();
                                    // End of file information, add it to the project.
                                    project.FileInfo.Add(fileInfo);
                                    fileInfo = new GerberFileInformation();
                                }
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                throw new ProjectReadException("Error reading project file.", ex);
            }
        }
    }
}


