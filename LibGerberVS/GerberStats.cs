using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GerberVS
{
    /// <summary>
    /// Maintains statistics on the various codes used in a RS274X file.
    /// </summary>
    public class GerberFileStats 
    {
        private Collection<GerberError> errorList;
        private Collection<GerberApertureInfo> apertureList;
        private Collection<GerberApertureInfo> dCodeList;

        // Auto Properties
        public int LevelCount { get; set; }
        public int G0 { get; set; }
        public int G1 { get; set; }
        public int G2 { get; set; }
        public int G3 { get; set; }
        public int G4 { get; set; }
        public int G10 { get; set; }
        public int G11 { get; set; }
        public int G12 { get; set; }
        public int G36 { get; set; }
        public int G37 { get; set; }
        public int G54 { get; set; }
        public int G55 { get; set; }
        public int G70 { get; set; }
        public int G71 { get; set; }
        public int G74 { get; set; }
        public int G75 { get; set; }
        public int G90 { get; set; }
        public int G91 { get; set; }
        public int UnknowGCodes { get; set; }

        public int D1 { get; set; }
        public int D2 { get; set; }
        public int D3 { get; set; }
        public int UnknownDCodes { get; set; }
        public int DCodeErrors { get; set; }

        public int M0 { get; set; }
        public int M1 { get; set; }
        public int M2 { get; set; }
        public int UnknownMCodes { get; set; }

        public int XCount { get; set; }
        public int YCount { get; set; }
        public int ICount { get; set; }
        public int JCount { get; set; }
        // Must include % RS-274 codes.
        public int StarCount { get; set; }
        public int UnknownCount { get; set; }

        // Properties
        public Collection<GerberError> ErrorList
        {
            get { return errorList; }
        }

        public Collection<GerberApertureInfo> ApertureList
        {
            get { return apertureList; }
        }

        public Collection<GerberApertureInfo> DCodeList
        {
            get { return dCodeList; }
        }

        // Constructor
        public GerberFileStats()
        {
            errorList = new Collection<GerberError>();
            apertureList = new Collection<GerberApertureInfo>();
            dCodeList = new Collection<GerberApertureInfo>();
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
        public void AddNewError(int level, string errorMessage, GerberErrorType errorType, string fileName = "", int lineNumber = 0)
        {
            errorList.Add(new GerberError(level, errorMessage, errorType, fileName, lineNumber));

            /*bool exists = false;

            // Check that the new error is unique.
            foreach(GerberError error in errorList)
            {
                if (error.ErrorMessage == errorMessage && error.Level == level && fileName == error.FileName)
                {
                    exists = true;
                    break;
                }

            }

            if (!exists)
                errorList.Add(new GerberError(level, errorMessage, errorType, fileName, lineNumber));*/
        }

        public void AddNewAperture(int level, int number, GerberApertureType type, double[] parameter)
        {
            GerberApertureInfo newAperture;

            // Next check to see if this aperture is already in the list.
            foreach (GerberApertureInfo aperture in apertureList)
            {
                if ((aperture.Number == number) && (aperture.Level == level))
                {
                    //Debug.WriteLine("Aperture {0} int level {1} is already in the list.", number, level);
                    return;
                }
            }

            // This aperture number is unique.  Therefore, add it to the list.
            //Debug.WriteLine("    Adding type {0} to aperture list ", type);

            newAperture = new GerberApertureInfo();
            // Set member elements.
            newAperture.Level = level;
            newAperture.Number = number;
            newAperture.ApertureType = type;
            for (int i = 0; i < 5; i++)
                newAperture.Parameters[i] = parameter[i];

            apertureList.Add(newAperture);
            return;
        }

        public void AddNewDList(int number)
        {
            GerberApertureInfo newDCode;

            // Look to see if this is already in list.
            foreach (GerberApertureInfo dCode in dCodeList)
            {
                if (dCode.Number == number)
                {
                    //Debug.WriteLine("    Code {0} already exists in D List.", number);
                    return;
                }
            }

            // This aperture number is unique.  Therefore, add it to the list.
            //Debug.WriteLine("    Adding code {0} to D List", number);
            newDCode = new GerberApertureInfo();
            // Set member elements.
            newDCode.Number = number;
            newDCode.Count = 0;
            dCodeList.Add(newDCode);
            return;
        }

        public bool IncrementDListCount(int number, int count)
        {
            // Find D code in list and increment it.
            foreach (GerberApertureInfo dCode in dCodeList)
            {
                if (dCode.Number == number)
                {
                    //Debug.WriteLine("    Incrementing D list count for code {0}, total = {1}", number, dList.Count + 1);
                    dCode.Count += count;   // Add to this aperture count, then return.
                    return true;            // Return true for success.
                }
            }

            // This D number is not defined.  Therefore, flag error.
            //Debug.WriteLine("    Didn't find this D code {0} in defined list.");
            AddNewError(-1, "Undefined aperture number called out in D code", GerberErrorType.GerberError);
            return false;  // Return false for failure.
        }
    }
}
