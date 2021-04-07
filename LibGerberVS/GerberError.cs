/* GerberError.cs - Type class for handling processing errors. */

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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberVS
{
    /// <summary>
    /// Maintains a list of errors encounted during parsing of the gerber file.
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
        /// Creates an new instance of an error log entry.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="errorMessage"></param>
        /// <param name="errorType"></param>
        /// <param name="fileName"></param>
        /// <param name="lineNumber"></param>
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
