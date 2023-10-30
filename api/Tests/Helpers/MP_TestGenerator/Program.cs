namespace MP_TestGenerator
{
    using System.Text;

    internal class Program
    {
        #pragma warning disable CS0120 // An object reference is required for the non-static field, method, or property 'Program.dictLookupItems'

        static void Main(string[] args)
        {
            // Initialize database input and file output values
            var config = Configuration.GetConfig(args);
            string strConnection = config.Item1;
            string strDatabase = config.Item2;
            string strOutputFilePath = config.Item3;

            //
            // Generate tests for textquery
            //
            int cMaxItems;
            var dictTextQueryArray = MongoAccess.FetchDictionaryDataFromDatabase(strConnection, strDatabase, "textquery", out cMaxItems, true);
            SortedDictionary<string, int> dictTextCat = dictTextQueryArray[0];
            SortedDictionary<string, int> dictTextVert = dictTextQueryArray[1];
            SortedDictionary<string, int> dictTextPub = dictTextQueryArray[2];


            // Create source file header and get container for the rest of the source code
            StringBuilder sb = CSharpGenerator.CreateSourceFileHeader();

            // Assemble a list of textquery tests for Categories.
            CSharpGenerator.CreateTestCases(sb, "Marketplace_TestData_TextQuery_Category", "Category", dictTextCat, "textquery", cMaxItems);

            // Assemble a list of textquery tests for Verticals.
            CSharpGenerator.CreateTestCases(sb, "Marketplace_TestData_TextQuery_Vertical", "Vertical", dictTextVert, "textquery", cMaxItems);

            // Assemble a list of textquery tests for Publishers.
            CSharpGenerator.CreateTestCases(sb, "Marketplace_TestData_TextQuery_Publisher", "Publisher", dictTextPub, "textquery", cMaxItems);

            //
            // Generate tests for checkbox query
            //
            var dictCheckboxArray = MongoAccess.FetchDictionaryDataFromDatabase(strConnection, strDatabase, "checkbox", out cMaxItems);
            SortedDictionary<string, int> dictCheckCat = dictCheckboxArray[0];
            SortedDictionary<string, int> dictCheckVert = dictCheckboxArray[1];
            SortedDictionary<string, int> dictCheckPub = dictCheckboxArray[2];

            // Assemble a list of checkbox tests for Categories.
            CSharpGenerator.CreateTestCases(sb, "Marketplace_TestData_Checkbox_Category", "Category", dictCheckCat, "checkbox", cMaxItems);

            // Assemble a list of checkbox tests for Verticals.
            CSharpGenerator.CreateTestCases(sb, "Marketplace_TestData_Checkbox_Vertical", "Vertical", dictCheckVert, "checkbox", cMaxItems);

            // Assemble a list of checkbox tests for Publishers.
            CSharpGenerator.CreateTestCases(sb, "Marketplace_TestData_Checkbox_Publisher", "Publisher", dictCheckPub, "checkbox", cMaxItems);

            CSharpGenerator.CreateSourceFileFooter(sb);

            if (string.IsNullOrEmpty(strOutputFilePath))
            {
                Console.WriteLine(sb.ToString());
            }
            else
            {
                System.IO.File.WriteAllText(strOutputFilePath, sb.ToString());
            }
        }

    } // End Class Program

} // End Namespace MP_TestGenerator
