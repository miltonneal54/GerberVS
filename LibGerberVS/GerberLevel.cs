using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberVS
{
    /// <summary>
    /// Creates a new instance of the Gerber Level.
    /// </summary>
    /// <remarks>
    /// Construct name "Level" replaces "Layer" in the later Gerber specifications.
    /// </remarks>
    public class GerberLevel
    {
        // Auto properties.
        public GerberStepAndRepeat StepAndRepeat { get; set; }   // The current step and repeat group (refer to RS274X specs).
        public GerberKnockout Knockout { get; set; }             // The current knockout group (refer to RS274X specs)
        public double Rotation { get; set; }                     // The current rotation around the origin.
        public GerberPolarity Polarity { get; set; }             // The polarity of this level.
        public string LevelName { get; set; }                    // The level name (NULL for none).

        /// <summary>
        /// Create a new Gerber Level.
        /// </summary>
        public GerberLevel(GerberImage gerberImage)
        {
            Knockout = new GerberKnockout();
            StepAndRepeat = new GerberStepAndRepeat();
            Rotation = 0.0;
            Polarity = GerberPolarity.Dark;
            LevelName = String.Empty;

            // If not the first then copy the previous level values into the new one.
            if (gerberImage.LevelList.Count > 0)
            {
                int previous = gerberImage.LevelList.Count - 1;
                this.LevelName = gerberImage.LevelList[previous].LevelName;
                this.StepAndRepeat = gerberImage.LevelList[previous].StepAndRepeat;
                this.Polarity = gerberImage.LevelList[previous].Polarity;
                this.Knockout = gerberImage.LevelList[previous].Knockout;
                // Clear this boolean so we only draw the knockout once.
                this.Knockout.FirstInstance = false;
            }

            gerberImage.LevelList.Add(this);
        }
    }

    public class GerberKnockout
    {
        public bool FirstInstance { get; set; }
        public GerberKnockoutType Type { get; set; }
        public GerberPolarity Polarity { get; set; }    // The polarity of the knockout.
        public double LowerLeftX { get; set; }
        public double LowerLeftY { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Border { get; set; }

        public GerberKnockout()
        {
            FirstInstance = false;
        }
    }

    /// <summary>
    /// Step and Repeat class.
    /// </summary>
    public class GerberStepAndRepeat
    {
        public int X { get; set; }
        public int Y { get; set; }
        public double DistanceX { get; set; }
        public double DistanceY { get; set; }

        public GerberStepAndRepeat()
        {
            X = 1;
            Y = 1;
            DistanceX = 0.0;
            DistanceY = 0.0;
        }
    }
}

