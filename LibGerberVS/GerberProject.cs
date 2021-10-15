using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace GerberVS
{
    /// <summary>
    /// Maintains information about the current file group.
    /// </summary>
    public class GerberProject
    {
        private Collection<GerberFileInformation> fileInfo;         // Collection of the child file information.

        /// <summary>
        /// The background color used for rendering the project.
        /// </summary>
        public Color BackgroundColor { get; set; }

        /// <summary>
        /// Number of files in the project.
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// The index of the selected file.
        /// </summary>
        public int CurrentIndex { get; set; }

        /// <summary>
        /// The quality of rendering to use.
        /// </summary>
        public GerberRenderQuality RenderQuality { get; set; }

        /// <summary>
        /// Confirm before deleting nets from an image.
        /// </summary>
        public bool CheckBeforeDelete { get; set; }

        /// <summary>
        /// Determine if a user selection is shown on a hidden layer.
        /// </summary>
        public bool ShowHiddenSelection { get; set; }

        /// <summary>
        /// The default path to load project files from.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The default name for the private project file.
        /// </summary>
        public string ProjectName { get; set; } 
        
        /// <summary>
        /// Test for an empty project.
        /// </summary>
        public bool IsEmpty
        {
            get { return fileInfo.Count == 0; }
        }

        /// <summary>
        /// Gets the file information list.
        /// </summary>
        public Collection<GerberFileInformation> FileInfo
        {
            get { return fileInfo; }
        }

        /// <summary>
        /// Creates a new instance of the gerber project type class.
        /// </summary>
        public GerberProject()
        {
            fileInfo = new Collection<GerberFileInformation>();
            CheckBeforeDelete = true;
            ShowHiddenSelection = false;
            RenderQuality = GerberRenderQuality.Default;
        }
    }

    /// <summary>
    /// Holds the rendering infomation for the gerber image.
    /// </summary>
    public class GerberRenderInformation
    {
        // Auto properties.
        /// <summary>
        /// The width of the scaled image.
        /// </summary>
        public double ImageWidth { get; set; }

        /// <summary>
        /// The height of the scaled image.
        /// </summary>
        public double ImageHeight { get; set; }

        /// <summary>
        /// The width of the display or print area.
        /// </summary>
        public double DisplayWidth { get; set; }

        /// <summary>
        /// The height of the display or print area.
        /// </summary>
        public double DisplayHeight { get; set; }   // The height of the render area.

        /// <summary>
        /// Gets or sets the X direction scale factor.
        /// </summary>
        public double ScaleFactorX { get; set; }

        /// <summary>
        /// Gets or sets the Y direction scale factor.
        /// </summary>
        public double ScaleFactorY { get; set; }

        /// <summary>
        /// The X coordinate of the lower left corner (in real world coordinates, in inches).
        /// </summary>
        public double Left { get; set; } 

        /// <summary>
        /// The Y coordinate of the lower left corner (in real world coordinates, in inches).
        /// </summary>
        public double Bottom { get; set; }

        /// <summary>
        /// The quality of rendering to use when drawing layers.
        /// </summary>
        public GerberRenderQuality RenderQuality { get; set; }

        /// <summary>
        /// Creates a new RenderInformation type class.
        /// </summary>
        public GerberRenderInformation()
        {
            ScaleFactorX = 1.0f;
            ScaleFactorY = 1.0f;
            RenderQuality = GerberRenderQuality.Default;
        }
    }

    /// <summary>
    /// Holds information related to an individual layer that is part of a project.
    /// </summary>
    public class GerberFileInformation
    {
        /// <summary>
        /// The image holding all the geometry of the layer.
        /// </summary>
        public GerberImage Image { get; internal set; }

        /// <summary>
        /// The color to render this layer with.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Set true if this layer file should be rendered with the project.
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// Full path and file name.
        /// </summary>
        public string FullPathName { get; set; }

        /// <summary>
        /// Short filename.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Set true if layer has been modified since last save.
        /// </summary>
        public bool LayerDirty { get; set; }

        /// <summary>
        /// User specified transformation for the layer.
        /// </summary>
        public UserTransform UserTransform { get; set; }

        /// <summary>
        /// Creates a new instance of gerber file information type.
        /// </summary>
        public GerberFileInformation()
        {
            Image = null;
            IsVisible = true;
            UserTransform = new UserTransform();
        }
    }

    /// <summary>
    /// Defines the tranformations set by the user.
    /// </summary>
    public class UserTransform
    {
        /// <summary>
        /// Gets or sets the X translation (in inches).
        /// </summary>
        public double TranslateX { get; set; }

        /// <summary>
        /// Gets or sets the Y translation (in inches).
        /// </summary>
        public double TranslateY { get; set; }

        /// <summary>
        /// Gets or sets the X direction scale.
        /// </summary>
        public double ScaleX { get; set; }

        /// <summary>
        /// Gets or sets the Y direction scale.
        /// </summary>
        public double ScaleY { get; set; }

        /// <summary>
        /// The rotation of the layer around the origin (in degrees).
        /// </summary>
        public double Rotation { get; set; }

        /// <summary>
        /// True if the layer is mirrored around the X axis (horizonal flip).
        /// </summary>
        public bool MirrorAroundX { get; set; }

        /// <summary>
        /// True if the layer is mirrored around the Y axis (vertical flip).
        /// </summary>
        public bool MirrorAroundY { get; set; }

        /// <summary>
        /// Set true if the layer should be rendered inverted.
        /// </summary>
        public bool Inverted { get; set; }
       
        /// <summary>
        /// Creates a new instance of the user transformation type class.
        /// </summary>
        public UserTransform()
        {
            ScaleX = 1.0;
            ScaleY = 1.0;
        }

        /// <summary>
        /// Creates a new instance of the user transformation type class with supplied parameters.
        /// </summary>
        /// <param name="translateX"></param>
        /// <param name="translateY"></param>
        /// <param name="scaleX"></param>
        /// <param name="scaleY"></param>
        /// <param name="rotation"></param>
        /// <param name="mirrorArroundX"></param>
        /// <param name="mirrorAroundY"></param>
        /// <param name="Inverted"></param>
        public UserTransform(double translateX, Double translateY, double scaleX, double scaleY, double rotation,
                                   bool mirrorArroundX, bool mirrorAroundY, bool Inverted)
        {
            TranslateX = translateX;
            TranslateY = translateY;
            ScaleX = scaleX;
            ScaleY = scaleY;
            Rotation = rotation;
            MirrorAroundX = mirrorArroundX;
            MirrorAroundY = mirrorAroundY;
            Inverted = false;
        }
    }
}
