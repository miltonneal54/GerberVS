using System.Collections.Generic;
using System.Diagnostics;

namespace IsoCnc
{
    internal class CodeTemplate
    {
        Dictionary<string, int> valueTable;
        public CodeTemplate() { }

        public CodeTemplate(Dictionary<string, int> valueTable) 
        {
            this.valueTable = valueTable;
        }

        public string CreateFormatString (string template)
        {
            foreach (var key in valueTable.Keys) 
            {
                template = template.Replace("{" + key, "{" + valueTable[key].ToString());
            }
            return template;
        }
    }
}