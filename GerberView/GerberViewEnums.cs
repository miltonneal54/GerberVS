using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberView
{
    public enum ImageTranslateMode
    {
        /// <summary>
        /// Translate the displayed image to the center of the display area.
        /// </summary>
        TranslateToCenter,

        /// <summary>
        /// Translate the displayed image to the origin of the display area.
        /// </summary>
        TranslateToFit,
    }
}
