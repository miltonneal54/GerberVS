using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberVS
{
    public struct Union
    {
        public int intValue;
        public double doubleValue;
    }

    public class GerberInstruction
    {
        // Auto Properties
        public GerberOpcodes Opcode { get; set; }
        public Union data;

        public GerberInstruction()
        {
            data = new Union();
        }
    }

    
}
