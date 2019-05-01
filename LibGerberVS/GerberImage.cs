using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberVS
{
    public class GerberImageInfo
    {
        // Automatic Properties
        public string ImageName { get; set; }
        public GerberPolarity Polarity { get; set; }
        public double MinX { get; set; }                                // Always in inches.
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }
        public double OffsetA { get; set; }
        public double OffsetB { get; set; }
        public GerberEncoding Encoding { get; set; }
        public double ImageRotation { get; set; }
        public GerberImageJustifyType ImageJustifyTypeA { get; set; }
        public GerberImageJustifyType ImageJustifyTypeB { get; set; }
        public double ImageJustifyOffsetA { get; set; }
        public double ImageJustifyOffsetB { get; set; }
        public double ImageJustifyOffsetActualA { get; set; }
        public double ImageJustifyOffsetActualB { get; set; }
        public string PlotterFilm { get; set; }
        public string FileTypeName { get; set; }                    // Descriptive string for the type of file (rs274-x, drill, etc)
        public List<GerberHIDAttribute> AttributeList { get; set; } // Attribute list that is used to hold all sorts of information about how the layer will be parsed.
        public int NumberOfAttribute { get; set; }

        public GerberImageInfo()
        { }
    }

    /// <summary>
    /// Creates a new gerber image and initializes its properties
    /// </summary>
    public class GerberImage
    {
        private Collection<ApertureMacro> apertureMacroList;        // A list of all aperture macros used (only used in RS274X types)
        private Collection<GerberNet> gerberNetList;                // A list of all geometric entities in the layer.

        // Public properties.
        public Collection<ApertureMacro> ApertureMacroList
        {
            get { return apertureMacroList; }
        }

        public Collection<GerberNet> GerberNetList
        {
            get { return gerberNetList; }
        }

        // Automatic properties.
        public GerberImageInfo ImageInfo { get; set; }               // Miscellaneous info regarding the layer such as overall size, etc.
        public GerberFileType FileType { get; set; }                 // The type of file (RS274X, drill, or pick-and-place).
        public ApertureDefinition[] ApertureArray { get; set; }      // List of all apertures used.
        public List<GerberLevel> LevelList { get; set; }             // List of all RS274X levels used (only used in RS274X types).
        public List<GerberNetState> NetStateList { get; set; }       // A list all RS274X states used (only used in RS274X types)
        public GerberFormat Format { get; set; }                     // Formatting info
        public GerberFileStats GerberStats { get; set; }             // RS274X statistics for the layer.
        public DrillFileStats DrillStats { get; set; }               // Excellon drill statistics for the layer.

        // Constructor
        public GerberImage(string fileTypeName)
        {
            ImageInfo = new GerberImageInfo();
            ImageInfo.MinX = double.MaxValue;
            ImageInfo.MinY = double.MaxValue;
            ImageInfo.MaxX = double.MinValue;
            ImageInfo.MaxY = double.MinValue;
            if (string.IsNullOrEmpty(fileTypeName))
                ImageInfo.FileTypeName = "Unknown";

            else
                ImageInfo.FileTypeName = fileTypeName;

            // The individual file parsers will have to set this.
            ImageInfo.AttributeList = null;
            ImageInfo.NumberOfAttribute = 0;
            apertureMacroList = new Collection<ApertureMacro>();
            ApertureArray = new ApertureDefinition[Gerber.MaximumApertures];
            LevelList = new List<GerberLevel>();
            NetStateList = new List<GerberNetState>();
            Format = new GerberFormat();
            gerberNetList = new Collection<GerberNet>();
            GerberStats = new GerberFileStats();
            DrillStats = new DrillFileStats();
            GerberNet gerberNet = new GerberNet();          // Create first gerberNet and fill in some initial values.
            gerberNet.Level = new GerberLevel(this);        // Create our first level and fill with some default values.
            gerberNet.NetState = new GerberNetState(this);  // Create our first netState.
            gerberNet.Label = String.Empty;
            GerberNetList.Add(gerberNet);
        }

       

        /// <summary>
        /// Perform some basic integrity tests on the gerber image.
        /// </summary>
        /// <returns>error status</returns>
        public GerberVerifyError GerberImageVerify()
        {
            GerberVerifyError errorStatus = GerberVerifyError.ImageOK;
            int numberOfNets = 0;

            if (this.GerberNetList == null)
                errorStatus |= GerberVerifyError.MissingNetList;

            if (this.Format == null) 
                errorStatus |= GerberVerifyError.MissingFormat;

            if (this.ImageInfo == null) 
                errorStatus |= GerberVerifyError.MissingImageInfo;

            if (this.GerberNetList != null)
                numberOfNets = this.GerberNetList.Count;

            // If we have nets but no apertures are defined, then complain.
            if (numberOfNets > 0)
            {
                if (this.ApertureArray.Length == 0)
                    errorStatus |= GerberVerifyError.MissingApertures;
            }

            return errorStatus;
        }
    }
}
