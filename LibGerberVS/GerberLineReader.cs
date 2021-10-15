
/* GerberLineReader.cs - Gerber file parser helper */

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
using System.IO;

namespace GerberVS
{
    /// <summary>
    /// Manage reading of the Gerber file
    /// </summary>
    internal class GerberLineReader
    {
        private StreamReader streamReader;
        private bool result = false;
        private bool isFirst;

        public string CurrentLine { get; private set; }
        public int LineLength { get; private set; }
        public int LineNumber { get; private set; }
        public int Position { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public bool EndOfFile
        {
            get { return CurrentLine == null; }
        }

        /// <summary>
        /// Initialize a new instance of the gerber line reader.
        /// </summary>
        /// <param name="streamReader">stream to read from</param>
        public GerberLineReader(StreamReader streamReader)
        {
            this.streamReader = streamReader;
            LineNumber = 0;
            Position = 0;
            LineLength = 0;
            CurrentLine = String.Empty;
        }

        /// <summary>
        /// Read the next character in the line or start on the next line if at the end of the current line.
        /// </summary>
        /// <returns>the character read</returns>
        public char Read()
        {
            if (Position < 0)
                Position = 1;

            if (Position >= LineLength)      // At the end of the line, read the next one.
            {
                CurrentLine = streamReader.ReadLine();
                LineNumber++;
                if (String.IsNullOrEmpty(CurrentLine))  // Empty line, return new line character.
                    return '\n';

                Position = 0;
                LineLength = CurrentLine.Length;
            }

            return CurrentLine[Position++];
        }

        /// <summary>
        /// Reads from the current line position to the end of line, consumes carriage return and linefeed characters and points to the start on the next line.
        /// </summary>
        public string ReadLineToEnd()
        {
            string line = String.Empty;
            int i = Position;

            for (; i < LineLength; i++)
                line += Read();

            return line;
        }

        /// <summary>
        /// Reads in digits from the current file position and converts them to an integer.
        /// </summary>
        /// <param name="length">number of digit in the integer</param>
        /// <returns>the value as an integer</returns>
        public int GetIntegerValue(ref int length)
        {
            string numberString = String.Empty;
            int rtnValue = int.MaxValue;

            SkipWhiteSpaces();
            isFirst = true;
            char nextCharacter = Read();
            while (Char.IsDigit(nextCharacter) || (nextCharacter == '-' && isFirst) || (nextCharacter == '+' && isFirst))
            {
                if(Char.IsDigit(nextCharacter))
                    length++;   // Exclude any prefixed sign.

                numberString += nextCharacter;
                isFirst = false;
                nextCharacter = Read();
            }

            Position--;
            if (!String.IsNullOrEmpty(numberString))
                result = int.TryParse(numberString, out rtnValue);

            return rtnValue;
        }

        /// <summary>
        /// Reads the line data and converts a series of digits to a double precision number if found.
        /// </summary>
        /// <returns></returns>
        public double GetDoubleValue()
        {
            string doubleString = string.Empty;
            double rtnValue = double.MaxValue;

            SkipWhiteSpaces();
            isFirst = true;
            char nextCharacter = Read();
            while ((Char.IsDigit(nextCharacter) || nextCharacter == '.') || (nextCharacter == '-' && isFirst) || (nextCharacter == '+' && isFirst))
            {
                doubleString += nextCharacter;
                nextCharacter = Read();
                isFirst = false;
            }

            Position--;
            if (!string.IsNullOrEmpty(doubleString))
                result = double.TryParse(doubleString, out rtnValue);

            return rtnValue;
        }

        /// <summary>
        /// Reads the line data and converts a series of digits to a double precision number if found.
        /// </summary>
        /// <param name="length">number of digits including the decimal point but excluding any prefixed sign</param>
        /// <returns>double precision number</returns>
        public double GetDoubleValue(ref int length)
        {
            string doubleString = String.Empty;
            double rtnValue = double.MaxValue;

            SkipWhiteSpaces();
            isFirst = true;
            char nextCharacter = Read();
            while ((Char.IsDigit(nextCharacter) || nextCharacter == '.') || (nextCharacter == '-' && isFirst) || (nextCharacter == '+' && isFirst))
            {
                if (Char.IsDigit(nextCharacter) || nextCharacter == '.')
                    length++;

                doubleString += nextCharacter;
                isFirst = false;
                nextCharacter = Read();
            }

            if(nextCharacter != '\n')
                Position--;

            if (!string.IsNullOrEmpty(doubleString))
                result = double.TryParse(doubleString, out rtnValue);

            return rtnValue;
        }

        /// <summary>
        /// Reads a specified number of characters into the return string.
        /// </summary>
        /// <param name="count">number of characters to read</param>
        /// <returns>the resultant string</returns>
        public string GetStringValue(int count)
        {
            string dataString = String.Empty;

            if (count < (LineLength - Position))
            {
                for (int i = 0; i < count; i++)
                    dataString += Read();
            }

            return dataString;
        }

        /// <summary>
        /// Reads the stream up to but not including the first occurance of a specified character.
        /// </summary>
        /// <param name="value">character to read to</param>
        /// <returns>the resulting string</returns>
        public string GetStringValue(char value)
        {
            string dataString = String.Empty;

            char charValue = Read();
            while (charValue != value)
            {
                dataString += charValue;
                charValue = Read();
            }

            Position--;
            return dataString;
        }

        /// <summary>
        /// Skips over white spaces.
        /// </summary>
        public void SkipWhiteSpaces()
        {
            char nextCharacter = Read();
            while (Char.IsWhiteSpace(nextCharacter))
                nextCharacter = Read();

            Position--;
        }
    }
}
