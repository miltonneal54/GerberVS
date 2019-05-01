using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberVS
{
    /// <summary>
    /// Maintains a list of errors during parsing of the gerber file.
    /// </summary>
    public class GerberError
    {
        // Auto Properties
        public int Level { get; set; }
        public string ErrorMessage { get; set; }
        public GerberErrorType ErrorType { get; set; }
        public string FileName { get; set; }
        public int LineNumber { get; set; }

        /// <summary>
        /// Creates an new instance an error log entry.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="errorMessage"></param>
        /// <param name="errorType"></param>
        public GerberError(int level, string errorMessage, GerberErrorType errorType, string fileName = "", int lineNumber = 0)
        {
            this.Level = level;
            this.ErrorMessage = errorMessage;
            this.ErrorType = errorType;
            this.FileName = fileName;
            this.LineNumber = lineNumber;
        }
    }
}
