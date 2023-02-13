using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberVS
{
    /// <summary>
    /// Class for defining a gerber level.
    /// </summary>
    /// <remarks>
    /// Constructor name "Level" replaces "Layer" in the later Gerber specifications.
    /// </remarks>
    public class GerberLevel
    {
        // Auto properties.
        public GerberKnockout Knockout { get; set; }             // The current knockout group (refer to RS274X specs)

        /// <summary>
        /// The current step and repeat group for this level.
        /// </summary>
        public GerberStepAndRepeat StepAndRepeat { get; set; }

        /// <summary>
        /// Dark or clear polarity for this level.
        /// </summary>
        public GerberPolarity Polarity { get; set; }

        /// <summary>
        /// The level name.
        /// </summary>
        public string LevelName { get; set; }

        /// <summary>
        /// Creates a new Gerber Level.
        /// </summary>
        public GerberLevel(GerberImage gerberImage)
        {
            Knockout = new GerberKnockout();
            StepAndRepeat = new GerberStepAndRepeat();
            Polarity = GerberPolarity.Dark;
            LevelName = String.Empty;

            // If not the first then copy the previous level values into the new one.
            if (gerberImage.LevelList.Count > 0)
            {
                int previous = gerberImage.LevelList.Count - 1;

                LevelName = gerberImage.LevelList[previous].LevelName;
                StepAndRepeat.X = gerberImage.LevelList[previous].StepAndRepeat.X;
                StepAndRepeat.Y = gerberImage.LevelList[previous].StepAndRepeat.Y;
                StepAndRepeat.DistanceX = gerberImage.LevelList[previous].StepAndRepeat.DistanceX;
                StepAndRepeat.DistanceY = gerberImage.LevelList[previous].StepAndRepeat.DistanceY;
                Polarity = gerberImage.LevelList[previous].Polarity;
                Knockout = gerberImage.LevelList[previous].Knockout;
                // Clear this boolean so we only draw the knockout once.
                Knockout.FirstInstance = false;
            }

            gerberImage.LevelList.Add(this);
        }
    }

    
    public class GerberKnockout
    {
        public bool FirstInstance { get; set; }
        public GerberKnockoutType KnockoutType { get; set; }
        public GerberPolarity Polarity { get; set; }    // The polarity of the knockout.
        public double LowerLeftX { get; set; }
        public double LowerLeftY { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Border { get; set; }

        /// <summary>
        /// Creates a new instance of GerberKnockout.
        /// </summary>
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

