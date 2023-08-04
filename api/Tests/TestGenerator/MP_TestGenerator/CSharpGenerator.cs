using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP_TestGenerator
{
    internal class CSharpGenerator
    {
        public static StringBuilder CreateSourceFileHeader()
        {

            StringBuilder sb = new StringBuilder();

            // Source file header (namespace declaration)
            sb.AppendLine("namespace Marketplace_InBrowser_Tests");
            sb.AppendLine("{");

            return sb;
        }

        public static void CreateSourceFileFooter(StringBuilder sb)
        {
            // Close namespace
            sb.AppendLine("}");
        }

        public static StringBuilder CreateTestCases(StringBuilder sbOutput, string strClassName, string strItemType, SortedDictionary<string, int> dict1, string strTestType, int cMaxItems)
        {
            // Class header (declaration)
            sbOutput.AppendLine($"    public class {strClassName}");
            sbOutput.AppendLine("    {");
            sbOutput.AppendLine("        public static IEnumerable<object[]> MyData =>");
            sbOutput.AppendLine("            new List<object[]>");
            sbOutput.AppendLine("            {");

            // Created lines should look like this:
            //         new object[] { "checkbox", "Category", "Air Compressing", 2 },

            int iItem = 0;
            foreach (KeyValuePair<string, int> kvp1 in dict1)
            {
                string strItemName = kvp1.Key;
                int cExpected = kvp1.Value;
                sbOutput.AppendLine($"\t\tnew object[] {{\"{strTestType}\", \"{strItemType}\", \"{strItemName}\", {iItem}, {cExpected}, {cMaxItems} }},");
                iItem++;
            }

            // Class footer
            sbOutput.AppendLine("            };");
            sbOutput.AppendLine("    }");

            return sbOutput;
        }
    }
}
