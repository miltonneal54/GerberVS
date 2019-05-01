using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberVS
{
    /// <summary>
    /// Holds information about the current selection.
    /// </summary>
    public class SelectionInformation
    {
        private Collection<GerberNet> selectedNetList;

        public string Filename { get; set; }
        public GerberImage SelectionImage { get; set; }
        public GerberSelection SelectionType { get; set; }
        public int SelectionCount { get; set; }
        public double LowerLeftX { get; set; }
        public double LowerLeftY { get; set; }
        public double UpperRightX { get; set; }
        public double UpperRightY { get; set; }

        public Collection<GerberNet> SelectedNetList
        {
            get { return selectedNetList; }
        }

        public SelectionInformation()
        {
            selectedNetList = new Collection<GerberNet>();
            SelectionType = GerberSelection.None;
            SelectionCount = 0;
        }

        public void ClearSelectionList()
        {
            selectedNetList.Clear();
            LowerLeftX = 0.0;
            LowerLeftY = 0.0;
            UpperRightX = 0.0;
            UpperRightY = 0.0;
            SelectionType = GerberSelection.None;
            SelectionImage = null;
            SelectionCount = 0;
        }
    }
}
