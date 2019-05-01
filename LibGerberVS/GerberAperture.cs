/* GerberAperture.cs - Classes for handling gerber apertures. */

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
using System.Globalization;
using System.Diagnostics;

namespace GerberVS
{
    public class GerberApertureInfo
    {
        public int Number { get; set; }
        public int Level { get; set; }
        public int Count { get; set; }
        public GerberApertureType ApertureType { get; set; }
        public double[] Parameters { get; set; }

        public GerberApertureInfo()
        {
            Parameters = new double[5];
        }
    }

    /// <summary>
    /// Holds an aperture definition.
    /// </summary>
    public class ApertureDefinition
    {
        private Collection<SimplifiedApertureMacro> simplifiedMacroList;
        public GerberApertureType ApertureType { get; set; }
        public ApertureMacro ApertureMacro { get; set; }
        public double[] Parameters { get; set; }
        public int ParameterCount { get; set; }
        public GerberUnit Unit { get; set; }

        /// <summary>
        /// Gets the simplified macro collection.
        /// </summary>
        public Collection<SimplifiedApertureMacro> SimplifiedMacroList
        { 
            get { return simplifiedMacroList; }
        }

        public ApertureDefinition()
        {
            simplifiedMacroList = new Collection<SimplifiedApertureMacro>();
            Parameters = new double[Gerber.MaximumApertureParameters];
        }
    }

    /// <summary>
    /// Processes an aperture macro.
    /// </summary>
    public class ApertureMacro
    {
        private Collection<GerberInstruction> instructionList;

        public string Name { get; set; }
        public int NufPushes { get; set; }                  // Nuf pushes in program to estimate stack size.

        public Collection<GerberInstruction> InstructionList
        {
            get { return instructionList; }
        }

        public ApertureMacro()
        {
            instructionList = new Collection<GerberInstruction>();
        }

        public static ApertureMacro ProcessApertureMacro(GerberLineReader reader)
        {
            const int MathOperationStackSize = 2;
            ApertureMacro apertureMacro = new ApertureMacro();
            GerberInstruction instruction;

            GerberOpcodes[] mathOperations = new GerberOpcodes[MathOperationStackSize];
            char characterRead;
            int primitive = 0;
            int mathOperationIndex = 0;
            int equate = 0;
            bool continueLoop = true;
            bool comma = false;                // Just read an operator (one of '*+X/)
            bool isNegative = false;           // Negative numbers succeeding ','
            bool foundPrimitive = false;
            int length = 0;

            // Get macro name
            apertureMacro.Name = reader.GetStringValue('*');
            characterRead = reader.Read();	// skip '*'

            // The first instruction in all programs will be NOP.
            instruction = new GerberInstruction();
            instruction.Opcode = GerberOpcodes.NOP;
            apertureMacro.InstructionList.Add(instruction);

            while (continueLoop && !reader.EndOfFile)
            {
                length = 0;
                characterRead = reader.Read();
                switch (characterRead)
                {
                    case '$':
                        if (foundPrimitive)
                        {
                            instruction = new GerberInstruction();
                            instruction.Opcode = GerberOpcodes.PushParameter;
                            apertureMacro.InstructionList.Add(instruction);
                            apertureMacro.NufPushes++;
                            instruction.Data.IntValue = reader.GetIntegerValue(ref length);
                            comma = false;
                        }

                        else
                            equate = reader.GetIntegerValue(ref length);

                        break;

                    case '*':
                        while (mathOperationIndex != 0)
                        {
                            instruction = new GerberInstruction();
                            instruction.Opcode = mathOperations[--mathOperationIndex];
                            apertureMacro.InstructionList.Add(instruction);
                        }

                        // Check is due to some gerber files has spurious empty lines (eg EagleCad)
                        if (foundPrimitive)
                        {
                            instruction = new GerberInstruction();
                            if (equate > 0)
                            {
                                instruction.Opcode = GerberOpcodes.PopParameter;
                                instruction.Data.IntValue = equate;
                            }

                            else
                            {
                                instruction.Opcode = GerberOpcodes.Primative;
                                instruction.Data.IntValue = primitive;
                            }

                            apertureMacro.InstructionList.Add(instruction);

                            equate = 0;
                            primitive = 0;
                            foundPrimitive = false;
                        }
                        break;

                    case '=':
                        if (equate > 0)
                            foundPrimitive = true;

                        break;

                    case ',':
                        if (!foundPrimitive)
                        {
                            foundPrimitive = true;
                            break;
                        }

                        while (mathOperationIndex != 0)
                        {
                            instruction = new GerberInstruction();
                            instruction.Opcode = mathOperations[--mathOperationIndex];
                            apertureMacro.InstructionList.Add(instruction);
                        }

                        comma = true;
                        break;

                    case '+':
                        while ((mathOperationIndex != 0) &&
                                OperatorPrecedence((mathOperationIndex > 0) ? mathOperations[mathOperationIndex - 1] : GerberOpcodes.NOP) >=
                                (OperatorPrecedence(GerberOpcodes.Add)))
                        {
                            instruction = new GerberInstruction();
                            instruction.Opcode = mathOperations[--mathOperationIndex];
                            apertureMacro.InstructionList.Add(instruction);
                        }

                        mathOperations[mathOperationIndex++] = GerberOpcodes.Add;
                        comma = true;
                        break;

                    case '-':
                        if (comma)
                        {
                            isNegative = true;
                            comma = false;
                            break;
                        }

                        while ((mathOperationIndex != 0) &&
                                OperatorPrecedence((mathOperationIndex > 0) ? mathOperations[mathOperationIndex - 1] : GerberOpcodes.NOP) >=
                                (OperatorPrecedence(GerberOpcodes.Subtract)))
                        {
                            instruction = new GerberInstruction();
                            instruction.Opcode = mathOperations[--mathOperationIndex];
                            apertureMacro.InstructionList.Add(instruction);
                        }

                        mathOperations[mathOperationIndex++] = GerberOpcodes.Subtract;
                        break;

                    case '/':
                        while ((mathOperationIndex != 0) &&
                                OperatorPrecedence((mathOperationIndex > 0) ? mathOperations[mathOperationIndex - 1] : GerberOpcodes.NOP) >=
                                (OperatorPrecedence(GerberOpcodes.Divide)))
                        {
                            instruction = new GerberInstruction();
                            instruction.Opcode = mathOperations[--mathOperationIndex];
                            apertureMacro.InstructionList.Add(instruction);
                        }

                        mathOperations[mathOperationIndex++] = GerberOpcodes.Divide;
                        comma = true;
                        break;

                    case 'X':
                    case 'x':
                        while ((mathOperationIndex != 0) &&
                            OperatorPrecedence((mathOperationIndex > 0) ? mathOperations[mathOperationIndex - 1] : GerberOpcodes.NOP) >=
                            (OperatorPrecedence(GerberOpcodes.Multiple)))
                        {
                            instruction = new GerberInstruction();
                            instruction.Opcode = mathOperations[--mathOperationIndex];
                            apertureMacro.InstructionList.Add(instruction);
                        }

                        mathOperations[mathOperationIndex++] = GerberOpcodes.Multiple;
                        comma = true;
                        break;

                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '.':
                        // Comments in aperture macros are a definition starting with zero and ends with a '*'
                        if ((characterRead == '0') && (!foundPrimitive) && (primitive == 0))
                        {
                            // Comment continues until next '*', just throw it away.
                            reader.GetStringValue('*');
                            characterRead = reader.Read(); // Read the '*'.
                            break;
                        }

                        // First number in an aperture macro describes the primitive as a numerical value
                        if (!foundPrimitive)
                        {
                            primitive = (primitive * 10) + (characterRead - '0');
                            break;
                        }

                        reader.Position--;
                        instruction = new GerberInstruction();
                        instruction.Opcode = GerberOpcodes.Push;
                        apertureMacro.InstructionList.Add(instruction);
                        apertureMacro.NufPushes++;
                        instruction.Data.DoubleValue = reader.GetDoubleValue();
                        if (isNegative)
                            instruction.Data.DoubleValue = -(instruction.Data.DoubleValue);

                        isNegative = false;
                        comma = false;
                        break;

                    case '%':
                        reader.Position--;      // Must return with % first in string since the main parser needs it.
                        continueLoop = false;   // Reached the end of the macro.
                        break;

                    default:
                        // Whitespace.
                        break;
                }
            }

            if (reader.EndOfFile)
                return null;

            return apertureMacro;
        }

        /// <summary>
        /// Gets the precedence of operator codes.
        /// </summary>
        /// <param name="opcode">operater code</param>
        /// <returns>value indicating precedence</returns>
        private static int OperatorPrecedence(GerberOpcodes opcode)
        {
            switch (opcode)
            {
                case GerberOpcodes.Add:
                case GerberOpcodes.Subtract:
                    return 1;

                case GerberOpcodes.Multiple:
                case GerberOpcodes.Divide:
                    return 2;
            }

            return 0;
        }
    }

    /// <summary>
    /// Simplifies an aperture macro.
    /// </summary>
    public class SimplifiedApertureMacro
    {
        public GerberApertureType ApertureType { get; set; }
        public double[] Parameters { get; set; }

        public SimplifiedApertureMacro()
        {
            Parameters = new double[Gerber.MaximumApertureParameters];
        }

        // Stack declarations and operations to be used by the simplify engine that executes the parsed aperture macros.
        private static class MacroStack
        {
            public static double[] Values { get; set; }
            public static int Count { get; set; }

            public static void InitializeStack(int stackSize)
            {
                Values = new double[stackSize];
                Count = 0;
            }

            // Pushes a value onto the stack.
            public static void Push(double value)
            {
                Values[Count++] = value;
                return;
            }

            // Pops a value off the stack.
            public static bool Pop(ref double value)
            {
                // Check if we try to pop an empty stack.
                if (Count == 0)
                    throw new MacroStackOverflowException("Attempt to pop an empty stack.");

                value = Values[--Count];
                return true;
            }

            // Reset the stack.
            public static void Reset()
            {
                for (int i = 0; i < Values.Length; i++)
                    Values[i] = 0.0;

                Count = 0;
            }
        }

        // Simplify the aperture macro.
        public static bool SimplifyApertureMacro(ApertureDefinition aperture, double scale)
        {
            const int extraStackSize = 10;
            bool success = true;
            int numberOfParameters = 0;
            bool clearOperatorUsed = false;
            double[] localParameters = new double[Gerber.MaximumApertureParameters]; // Local copy of parameters.
            double[] tmp = { 0.0, 0.0 };
            int index = 0;
            GerberApertureType type = GerberApertureType.None;
            SimplifiedApertureMacro macro;

            if (aperture == null || aperture.ApertureMacro == null)
                throw new GerberApertureException("In SimplifyApertureMacro, aperture = null");

            // Allocate stack for VM.
            MacroStack.InitializeStack(aperture.ApertureMacro.NufPushes + extraStackSize);

            // Make a copy of the parameter list that we can rewrite if necessary.
            localParameters = new double[Gerber.MaximumApertureParameters];
            foreach (double p in aperture.Parameters)
                localParameters[index++] = p;

            foreach (GerberInstruction instruction in aperture.ApertureMacro.InstructionList)
            {
                switch (instruction.Opcode)
                {
                    case GerberOpcodes.NOP:
                        break;

                    case GerberOpcodes.Push:
                        MacroStack.Push(instruction.Data.DoubleValue);
                        break;

                    case GerberOpcodes.PushParameter:
                        MacroStack.Push(localParameters[instruction.Data.IntValue - 1]);
                        break;

                    case GerberOpcodes.PopParameter:
                        MacroStack.Pop(ref tmp[0]);
                        localParameters[instruction.Data.IntValue - 1] = tmp[0];
                        break;

                    case GerberOpcodes.Add:
                        MacroStack.Pop(ref tmp[0]);
                        MacroStack.Pop(ref tmp[1]);
                        MacroStack.Push(tmp[1] + tmp[0]);
                        break;

                    case GerberOpcodes.Subtract:
                        MacroStack.Pop(ref tmp[0]);
                        MacroStack.Pop(ref tmp[1]);
                        MacroStack.Push(tmp[1] - tmp[0]);
                        break;

                    case GerberOpcodes.Multiple:
                        MacroStack.Pop(ref tmp[0]);
                        MacroStack.Pop(ref tmp[1]);
                        MacroStack.Push(tmp[1] * tmp[0]);
                        break;

                    case GerberOpcodes.Divide:
                        MacroStack.Pop(ref tmp[0]);
                        MacroStack.Pop(ref tmp[1]);
                        MacroStack.Push(tmp[1] / tmp[0]);
                        break;

                    case GerberOpcodes.Primative:
                        // This handles the exposure thing in the aperture macro.
                        // The exposure is always the first element on stack independent
                        // of aperture macro.
                        switch (instruction.Data.IntValue)
                        {
                            case 1:
                                //Debug.Write("    Aperture macro circle [1] (");
                                type = GerberApertureType.MacroCircle;
                                numberOfParameters = 4;
                                break;

                            case 3:
                                break;  // EOF.

                            case 4:
                                //Debug.Write("    Aperture macro outline [4] (");
                                type = GerberApertureType.MacroOutline;
                                // Number of parameters are:
                                // Number of points defined in entry 1 of the stack + start point. Times two since it is both X and Y.
                                // Then three more; exposure, nuf points and rotation.
                                numberOfParameters = ((int)MacroStack.Values[1] + 1) * 2 + 3;
                                break;

                            case 5:
                                //Debug.Write("    Aperture macro polygon [5] (");
                                type = GerberApertureType.MacroPolygon;
                                numberOfParameters = 6;
                                break;

                            case 6:
                                //Debug.WriteLine("    Aperture macro moiré [6] (");
                                type = GerberApertureType.MacroMoire;
                                numberOfParameters = 9;
                                break;

                            case 7:
                                //Debug.Write("    Aperture macro thermal [7] (");
                                type = GerberApertureType.MacroThermal;
                                numberOfParameters = 6;
                                break;

                            case 2:
                            case 20:
                                //Debug.Write("    Aperture macro line 20/2 (");
                                type = GerberApertureType.MacroLine20;
                                numberOfParameters = 7;
                                break;

                            case 21:
                                //Debug.Write("    Aperture macro line 21 (");
                                type = GerberApertureType.MarcoLine21;
                                numberOfParameters = 6;
                                break;

                            case 22:
                                //Debug.Write("    Aperture macro line 22 (");
                                type = GerberApertureType.MacroLine22;
                                numberOfParameters = 6;
                                break;

                            default:
                                success = false;
                                break;
                        }

                        if (type != GerberApertureType.None)
                        {
                            if (numberOfParameters > Gerber.MaximumApertureParameters)
                            {
                                throw new GerberApertureException("Too many parameters for aperture macro;");
                                // GERB_COMPILE_ERROR("Number of parameters to aperture macro are more than gerbv is able to store\n");
                            }

                            // Create a new simplified aperture macro and start filling in the blanks.
                            macro = new SimplifiedApertureMacro();
                            macro.ApertureType = type;
                            index = 0;
                            for (int i = 0; i < numberOfParameters; i++)
                                macro.Parameters[i] = MacroStack.Values[i];

                            // Convert any mm values to inches.
                            switch (type)
                            {
                                case GerberApertureType.MacroCircle:
                                    if (Math.Abs(macro.Parameters[0]) < 0.001)
                                        clearOperatorUsed = true;

                                    macro.Parameters[1] /= scale;
                                    macro.Parameters[2] /= scale;
                                    macro.Parameters[3] /= scale;
                                    break;

                                case GerberApertureType.MacroOutline:
                                    if (Math.Abs(macro.Parameters[0]) < 0.001)
                                        clearOperatorUsed = true;

                                    for (int i = 2; i < numberOfParameters - 1; i++)
                                        macro.Parameters[i] /= scale;

                                    break;

                                case GerberApertureType.MacroPolygon:
                                    if (Math.Abs(macro.Parameters[0]) < 0.001)
                                        clearOperatorUsed = true;

                                    macro.Parameters[2] /= scale;
                                    macro.Parameters[3] /= scale;
                                    macro.Parameters[4] /= scale;
                                    break;

                                case GerberApertureType.MacroMoire:
                                    macro.Parameters[0] /= scale;
                                    macro.Parameters[1] /= scale;
                                    macro.Parameters[2] /= scale;
                                    macro.Parameters[3] /= scale;
                                    macro.Parameters[4] /= scale;
                                    macro.Parameters[6] /= scale;
                                    macro.Parameters[7] /= scale;
                                    break;

                                case GerberApertureType.MacroThermal:
                                    macro.Parameters[0] /= scale;
                                    macro.Parameters[1] /= scale;
                                    macro.Parameters[2] /= scale;
                                    macro.Parameters[3] /= scale;
                                    macro.Parameters[4] /= scale;
                                    break;

                                case GerberApertureType.MacroLine20:
                                    if (Math.Abs(macro.Parameters[0]) < 0.001)
                                        clearOperatorUsed = true;

                                    macro.Parameters[1] /= scale;
                                    macro.Parameters[2] /= scale;
                                    macro.Parameters[3] /= scale;
                                    macro.Parameters[4] /= scale;
                                    macro.Parameters[5] /= scale;
                                    break;

                                case GerberApertureType.MarcoLine21:
                                case GerberApertureType.MacroLine22:
                                    if (Math.Abs(macro.Parameters[0]) < 0.001)
                                        clearOperatorUsed = true;

                                    macro.Parameters[1] /= scale;
                                    macro.Parameters[2] /= scale;
                                    macro.Parameters[3] /= scale;
                                    macro.Parameters[4] /= scale;
                                    break;

                                default:
                                    break;
                            }

                            aperture.SimplifiedMacroList.Add(macro);
                            //for (int i = 0; i < numberOfParameters; i++)
                            //    Debug.Write(String.Format("{0}, ", MacroStack.Values[i]));

                            //Debug.WriteLine(")");
                        }

                        // Here we reset the stack pointer. It's not generally
                        // correct to do this, but since I know how the compiler works
                        // I can do this. The correct way to do this should be to 
                        // subtract number of used elements in each primitive operation.
                        MacroStack.Reset();
                        break;

                    default:
                        break;
                }
            }

            // Store a flag to let the renderer know if it should expect any "clear" primatives.
            aperture.Parameters[0] = clearOperatorUsed ? 1.0f : 0.0f;
            return success;
        }
    }

    public struct Union
    {
        private int intValue;
        private double doubleValue;

        public int IntValue
        {
            get { return intValue; }
            set { intValue = value; }
        }

        public double DoubleValue
        {
            get { return doubleValue; }
            set { doubleValue = value; }
        }

        public Union(int intValue, double doubleValue)
        {
            this.intValue = intValue;
            this.doubleValue = doubleValue;
        }
    }

    public class GerberInstruction
    {
        // Auto Properties
        public GerberOpcodes Opcode { get; set; }
        public Union Data;

        public GerberInstruction()
        {
            Data = new Union();
        }
    }
}

