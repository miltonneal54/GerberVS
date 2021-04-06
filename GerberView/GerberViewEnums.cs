using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberView
{
    public enum GerberRenderMode
    {
        /// <summary>
        /// Render with translation to centre of display area.
        /// </summary>
        TranslateToCentre = 0,

        /// <summary>
        /// Render with translation to fit display area.
        /// </summary>
        TranslateToFit,

        /// <summary>
        /// Scale maximised to fit display area.
        /// </summary>
        ScaleToFit

    }
}
