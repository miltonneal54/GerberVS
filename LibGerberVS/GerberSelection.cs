/* GerberSelection.cs - Type classes for maintaining a user selection. */

/*  Copyright (C) 2015-2021 Milton Neal <milton200954@gmail.com>
    *** Acknowledgments to Gerbv Authors and Contributors. ***

    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions
    are met:

    1. Redistributions of source code must retain the above copyright
       notice, this list of conditions and the following disclaimer.
    2. Redistributions in binary form must reproduce the above copyright
       notice, this list of conditions and the following disclaimer in the
       documentation and/or other materials provided with the distribution.
    3. Neither the name of the project nor the names of its contributors
       may be used to endorse or promote products derived from this software
       without specific prior written permission.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
    ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
    IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
    ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
    FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
    DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
    OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
    HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
    LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
    OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
    SUCH DAMAGE.
 */

using System.Collections.ObjectModel;

namespace GerberVS
{
    /// <summary>
    /// Maintains a list of selected objects and their associated net index.
    /// </summary>
    public class SelectionItem
    {

        /// <summary>
        /// Gets the net list in the current selection.
        /// </summary>
        public Collection<GerberNet> SelectedNetList { get; }

        /// <summary>
        /// Gets the list of net indexes in the cuttent selection
        /// </summary>
        public Collection<int> SelectedNetIndex { get; }

        /// <summary>
        /// Creates a new instance of the selection item type class.
        /// </summary>
        public SelectionItem()
        {
            SelectedNetList = new Collection<GerberNet>();
            SelectedNetIndex = new Collection<int>();
        }
    }

    /// <summary>
    /// Holds information about the current user selection.
    /// </summary>
    public class SelectionInformation
    {
        /// <summary>
        /// File information on the  current selection layer.
        /// </summary>
        public GerberFileInformation FileInfo { get; set;}

        /// <summary>
        /// Indicates whether a point or region type selection is used.
        /// </summary>
        public GerberSelection SelectionType { get; set; }

        /// <summary>
        /// Lower left X coorinate of the selection region.
        /// </summary>
        public double LowerLeftX { get; set; }

        /// <summary>
        /// Lower left Y coorinate of the selection region.
        /// </summary>
        public double LowerLeftY { get; set; }

        /// <summary>
        /// Upper right X coorinate of the selection region.
        /// </summary>
        public double UpperRightX { get; set; }

        /// <summary>
        /// Upper right Y coorinate of the selection region.
        /// </summary>
        public double UpperRightY { get; set; }

        /// <summary>
        /// Gets the list of objects and their indexes in the current selection.
        /// </summary>
        public SelectionItem SelectedNodeArray { get; }

        /// <summary>
        /// Gets the number of nets in the current selection.
        /// </summary>
        public int Count
        {
            get { return SelectedNodeArray.SelectedNetIndex.Count; }
        }

        /// <summary>
        /// Creates a new instance of the selection information type class.
        /// </summary>
        public SelectionInformation(GerberFileInformation fileInfo)
        {
            SelectedNodeArray = new SelectionItem();
            SelectionType = GerberSelection.None;
            FileInfo = fileInfo;
        }
     }
}
