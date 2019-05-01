/* DrillStats.cs - Classes for handling drill file statistics and error information. */

/*  Copyright (C) 2015-2018 Milton Neal <milton200954@gmail.com>
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberVS
{
    // Linked list of drills found in active levels. Used in reporting statistics.
    public class DrillInfo
    {
        public int drillCount { get; set; }
        public int drillNumber { get; set; }
        public double drillSize { get; set; }
        public string drillUnit { get; set; }

        public DrillInfo()
        {
            drillCount = 0;
            drillNumber = -1;
            drillSize = 0.0;
            drillUnit = String.Empty;
        }
    }

    /// <summary>
    /// Maintains statistics on the various codes used in a Drill file.
    /// </summary>
    public class DrillFileStats
    {
        private Collection<GerberError> errorList;
        private Collection<DrillInfo> drillInfoList;

        public int LevelCount { get; set; }
        public int Comment { get; set; }
        public int F { get; set; }
        public int G00 { get; set; }
        public int G01 { get; set; }
        public int G02 { get; set; }
        public int G03 { get; set; }
        public int G04 { get; set; }
        public int G05 { get; set; }
        public int G85 { get; set; }
        public int G90 { get; set; }
        public int G91 { get; set; }
        public int G93 { get; set; }
        public int GUnknown { get; set; }
        public int M00 { get; set; }
        public int M01 { get; set; }
        public int M18 { get; set; }
        public int M25 { get; set; }
        public int M30 { get; set; }
        public int M31 { get; set; }
        public int M45 { get; set; }
        public int M47 { get; set; }
        public int M48 { get; set; }
        public int M71 { get; set; }
        public int M72 { get; set; }
        public int M95 { get; set; }
        public int M97 { get; set; }
        public int M98 { get; set; }
        public int MUnknown { get; set; }
        public int R { get; set; }
        public int Unknown { get; set; }
        public int TotalCount { get; set; }  // Used to total up the drill count across all levels/sizes.
        public string Detect { get; set; }

        public DrillFileStats()
        {
            errorList = new Collection<GerberError>();
            drillInfoList = new Collection<DrillInfo>();
        }

        public Collection<GerberError> ErrorList
        {
            get { return errorList; }
        }

        public Collection<DrillInfo> DrillInfoList
        {
            get { return drillInfoList; }
        }


        /// <summary>
        /// Adds a new error to the error list;
        /// </summary>
        /// <param name="level"></param>
        /// <param name="errorMessage"></param>
        /// <param name="errorType"></param>
        /// <remarks>
        /// Only unique errors are added to the list.
        /// </remarks>
        public void AddNewError(int level, string errorMessage, GerberErrorType errorType, int lineNumber, string fileName)
        {
            bool exists = false;

            // Check that the new error is unique.
            foreach (GerberError error in errorList)
            {
                if (error.ErrorMessage == errorMessage && error.Level == level && error.FileName == fileName)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
                errorList.Add(new GerberError(level, errorMessage, errorType, fileName, lineNumber));
        }

        /// <summary>
        /// Adds a new error to the list.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="errorMessage"></param>
        /// <param name="errorType"></param>
        public void AddNewError(int level, string errorMessage, GerberErrorType errorType)
        {
            AddNewError(level, errorMessage, errorType, 0, String.Empty);
        }

        public void IncrementDrillCounter(int drillNumber)
        {

            // First check to see if this drill is already in the list.
            foreach (DrillInfo d in drillInfoList)
            {
                if (d.drillNumber == drillNumber)
                {
                    d.drillCount++;
                    break;
                }
            }
        }

        public void ModifyDrillList(int drillNumber, double drillSize, string drillUnit)
        {

            // Look for this drill number in drill list.
            foreach (DrillInfo d in drillInfoList)
            {
                // And update it.
                if (d.drillNumber == drillNumber)
                {
                    d.drillSize = drillSize;
                    d.drillUnit = drillUnit;
                }
            }

            return;
        }

        public void AddToDrillList(int drillNumber, double drillSize, string drillUnit)
        {
            bool exists = false;
            // First check to see if this drill is already in the list.
            if (drillInfoList.Count > 0)
            {
                foreach (DrillInfo d in drillInfoList)
                {
                    if (drillNumber == d.drillNumber)
                    {
                        exists = true;
                        break;
                    }
                }
            }

            // Create a new drill and add it to the drill list.
            if (!exists)
            {
                DrillInfo drillInfo = new DrillInfo();
                drillInfo.drillNumber = drillNumber;
                drillInfo.drillSize = drillSize;
                drillInfo.drillCount = 0;
                drillInfo.drillUnit = drillUnit;

                drillInfoList.Add(drillInfo);
            }

            return;
        }
    }
}


