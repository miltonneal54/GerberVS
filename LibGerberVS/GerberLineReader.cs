
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
        /// Reads the next character in the line without consuming it.
        /// </summary>
        /// <returns></returns>
        public char Peek()
        {
            if (Position < LineLength - 1)
                return CurrentLine[Position];

            else
                return '\n';
        }

        /// <summary>
        /// Reads the next character or the first character of the next line if at the end of the current line.
        /// </summary>
        /// <returns>the character read</returns>
        public char Read()
        {
            if (Position >= LineLength)      // At the end of the line, read the next one.
            {
                CurrentLine = streamReader.ReadLine();
                if (CurrentLine == null)    // EOF
                    return '\0';

                CurrentLine += '\n';
                LineNumber++;
                Position = 0;
                LineLength = CurrentLine.Length;
            }

            return CurrentLine[Position++];
        }

        /// <summary>
        /// Reads from the current line position to end of line and points to the start of the next line.
        /// </summary>
        public string ReadLineToEnd()
        {
            StringBuilder line = new StringBuilder();
            int i = Position;

            for (; i < LineLength; i++)
                line.Append(Read());

            return line.ToString();
        }

        /// <summary>
        /// Reads a specified number of characters into the return string.
        /// </summary>
        /// <param name="count">number of characters to read</param>
        /// <returns>the resultant string</returns>
        public string ReadLine(int count)
        {
            StringBuilder line = new StringBuilder();

            if (count < (LineLength - Position))
            {
                for (int i = 0; i < count; i++)
                    line.Append(Read());
            }

            return line.ToString();
        }

        /// <summary>
        /// Reads the current line up to but not including the first occurance of a specified character.
        /// </summary>
        /// <param name="value">character to read to</param>
        /// <returns>the resulting string</returns>
        public string ReadLine(char value)
        {
            StringBuilder line = new StringBuilder();

            char charValue = Read();
            while (charValue != value)
            {
                line.Append(charValue);
                charValue = Read();
            }

            Position--;
            return line.ToString();
        }

        /// <summary>
        /// Reads the line data and converts a series of digits to an integer.
        /// </summary>
        /// <param name="length">number of digit in the integer</param>
        /// <returns>the value as an integer</returns>
        public int GetIntegerValue(ref int length)
        {
            StringBuilder numberString = new StringBuilder();
            int rtnValue = 0;

            SkipWhiteSpaces();
            isFirst = true;
            char nextCharacter = Read();
            while (Char.IsDigit(nextCharacter) || (nextCharacter == '-' && isFirst) || (nextCharacter == '+' && isFirst))
            {
                if(Char.IsDigit(nextCharacter))
                    length++;   // Exclude any prefixed sign.

                numberString.Append(nextCharacter);
                isFirst = false;
                nextCharacter = Read();
            }

            Position--;
            result = int.TryParse(numberString.ToString(), out rtnValue);
            if (!result)
                rtnValue = int.MaxValue;

            return rtnValue;
        }

        /// <summary>
        /// Reads the line data and converts a series of digits to a double precision number.
        /// </summary>
        /// <returns>double precision value</returns>
        public double GetDoubleValue()
        {
            StringBuilder doubleString = new StringBuilder();
            double rtnValue = double.MaxValue;

            SkipWhiteSpaces();
            isFirst = true;
            char nextCharacter = Read();
            while ((Char.IsDigit(nextCharacter) || nextCharacter == '.') || (nextCharacter == '-' && isFirst) || (nextCharacter == '+' && isFirst))
            {
                doubleString.Append(nextCharacter);
                nextCharacter = Read();
                isFirst = false;
            }

            Position--;
            result = double.TryParse(doubleString.ToString(), out rtnValue);
            if (!result)
                rtnValue = double.MaxValue;

            return rtnValue;
        }

        /// <summary>
        /// Reads the line data and converts a series of digits to a double precision number.
        /// </summary>
        /// <param name="length">number of digits including the decimal point but excluding any prefixed sign</param>
        /// <returns>double precision value</returns>
        public double GetDoubleValue(ref int length)
        {
            StringBuilder doubleString = new StringBuilder();
            double rtnValue = double.MaxValue;

            SkipWhiteSpaces();
            isFirst = true;
            char nextCharacter = Read();
            while ((Char.IsDigit(nextCharacter) || nextCharacter == '.') || (nextCharacter == '-' && isFirst) || (nextCharacter == '+' && isFirst))
            {
                if (nextCharacter != '-' && nextCharacter != '+')    // Don't count + or - prefix.
                    length++;

                doubleString.Append(nextCharacter);
                isFirst = false;
                nextCharacter = Read();
            }

            Position--;
            result = double.TryParse(doubleString.ToString(), out rtnValue);
            if (!result)
                rtnValue = double.MaxValue;

            return rtnValue;
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
