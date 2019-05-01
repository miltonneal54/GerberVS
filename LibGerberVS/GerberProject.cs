using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace GerberVS
{
    public class GerberProject
    {
        private Collection<GerberFileInformation> fileInfo;         // The array for holding the child fileinfos.
        public Color BackgroundColor { get; set; }                  // The background color used for rendering.
        public int CurrentIndex { get; set; }                       // The index of the currently active fileinfo.
        public GerberUserTransform UserTransform { get; set; }      // User-specified transformation for the file (mirroring, translating, etc)
        public GerberRenderQuality RenderQuality { get; set; }      // The type of renderer to use.
        public string Path { get; set; }                            // The default path to load new files from.
        public string ProjectName { get;set; }                      // The default name for the private project file.

        public GerberProject()
        {
            fileInfo = new Collection<GerberFileInformation>();
            RenderQuality = GerberRenderQuality.Default;
            UserTransform = new GerberUserTransform();
        }

        public Collection<GerberFileInformation> FileInfo
        {
            get { return fileInfo; }
        }
    }

    /// <summary>
    /// Holds the rendering infomation for the graphics surface
    /// </summary>
    public class RenderInformation
    {
        // Auto properties.
        /// <summary>
        /// The width of the image.
        /// </summary>
        public double ImageWidth { get; set; }
        public double ImageHeight { get; set; }     // The height of the image.
        public double DisplayWidth { get; set; }    // The width of the render area.
        public double DisplayHeight { get; set; }   // The height of the render area.
        public double ScaleFactorX { get; set; }    // X direction scale factor.
        public double ScaleFactorY { get; set; }    // Y direction scale factor.
        public double TranslateX { get; set; }      // The X translate value.
        public double TranslateY { get; set; }      // The Y translate value.
        public double ScrollValueX { get; set; }    // Current X scroll value.
        public double ScrollValueY { get; set; }    // Current Y scroll value.
        public double Left { get; set; }            // The X coordinate of the lower left corner (in real world coordinates, in inches).
        public double Bottom { get; set; }          // The Y coordinate of the lower left corner (in real world coordinates, in inches).
        public GerberRenderQuality RenderType { get; set; } //!< the type of rendering to use.

        public RenderInformation()
        {
            ScaleFactorX = 1.0;
            ScaleFactorY = 1.0;
            RenderType = GerberRenderQuality.Default;
        }
    }

    /// <summary>
    /// Holds information related to an individual layer that is part of a project.
    /// </summary>
    public class GerberFileInformation
    {
        public GerberImage Image { get; set; }      // The image holding all the geometry of the layer.
        public Color Color { get; set; }            // The color to render this layer with.
        public int Alpha { get; set; }              // Alpha level;
        public bool IsVisible { get; set; }         // True if this layer file should be rendered with the project.
        public string FullPathName { get; set; }    // Full pathname to the file.
        public string FileName { get; set; }        // The name used when referring to this layer file(e.g. in a layer selection menu)
        public bool LayerDirty { get; set; }        // True if layer has been modified since last save.
        public bool Inverted { get; set; }          // True if the file image should be rendered "inverted" (light is dark and vice versa).
        //public GerberUserTransform UserTransform { get; set; }      // User-specified transformation for the file (mirroring, translating, etc)

        public GerberFileInformation()
        {
            Image = null;
            IsVisible = true;
            //UserTransform = new GerberUserTransform();
        }
    }

    public struct HIDAttributeValue
    {
        private int intValue;
        private string strValue;
        private double realValue;

        public int IntValue
        {
            get { return intValue; }
            set { intValue = value; }
        }

        public string StrValue
        {
            get { return strValue; }
            set { strValue = value; }
        }

        public double RealValue
        {
            get { return realValue; }
            set { realValue = value; }
        }

        public HIDAttributeValue(int intValue, string strValue, double realValue)
        {
            this.intValue = intValue;
            this.strValue = strValue;
            this.realValue = realValue;
        }
    }

    public class GerberHIDAttribute
    {
        public string Name { get; set; }
        public string HelpText { get; set; }
        public GerberHIDType HIDType { get; set; }
        public int MinValue { get; set; }
        public int MaxValue { get; set; }	/* for integer and real */
        public HIDAttributeValue DefaultValue;	/* Also actual value for global attributes.  */
        public string[] Enumerations { get; set; }
        /* If set, this is used for global attributes (i.e. those set
           statically with REGISTER_ATTRIBUTES below) instead of changing
           the default_val.  Note that a HID_Mixed attribute must specify a
           pointer to gerbv_HID_Attr_Val here, and HID_Boolean assumes this is
           "char *" so the value should be initialized to zero, and may be
           set to non-zero (not always one).  */
        public IntPtr Value { get; set; }
        public int Hash { get; set; } /* for detecting changes. */

        public GerberHIDAttribute()
        { }

        public GerberHIDAttribute(GerberHIDAttribute attibute)
        {
            Name = attibute.Name;
            HelpText = attibute.HelpText;
            HIDType = attibute.HIDType;
            MinValue = attibute.MinValue;
            MaxValue = attibute.MaxValue;
            Enumerations = attibute.Enumerations;
            Value = attibute.Value;
            Hash = attibute.Hash;
        }
    }

    /// <summary>
    /// Defines the tranformations set by the user.
    /// </summary>
    public class GerberUserTransform
    {
        public double TranslateX { get; set; }      // The X translation (in inches)
        public double TranslateY { get; set; }      // The Y translation (in inches)
        public double ScaleX { get; set; }          // The X scale factor (1.0 is default).
        public double ScaleY { get; set; }          // The Y scale factor (1.0 is default).
        public double Rotation { get; set; }        // The rotation of the level around the origin (in degrees).
        public bool MirrorAroundX { get; set; }     // True if the level is mirrored around the X axis (horizonal flip).
        public bool MirrorAroundY { get; set; }     // True if the level is mirrored around the Y axis (vertical flip).
       
        public GerberUserTransform()
        {
            ScaleX = 1.0;
            ScaleY = 1.0;
            // All others are default initial values;
        }
    }
}
