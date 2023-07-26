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
            var dictTextQueryArray = MongoAccess.FetchDictionaryDataFromDatabase(strConnection, strDatabase, "textquery");
            SortedDictionary<string, int> dictTextCat = dictTextQueryArray[0];
            SortedDictionary<string, int> dictTextVert = dictTextQueryArray[1];
            SortedDictionary<string, int> dictTextPub = dictTextQueryArray[2];


            // Create source file header and get container for the rest of the source code
            StringBuilder sb = CSharpGenerator.CreateSourceFileHeader();

            // Assemble a list of textquery tests for Categories.
            CSharpGenerator.CreateTestCases(sb, "Marketplace_TestData_TextQuery_Category", "Category", dictTextCat, "textquery");

            // Assemble a list of textquery tests for Verticals.
            CSharpGenerator.CreateTestCases(sb, "Marketplace_TestData_TextQuery_Vertical", "Vertical", dictTextVert, "textquery");

            // Assemble a list of textquery tests for Publishers.
            CSharpGenerator.CreateTestCases(sb, "Marketplace_TestData_TextQuery_Publisher", "Publisher", dictTextPub, "textquery");

            //
            // Generate tests for checkbox query
            //
            var dictCheckboxArray = MongoAccess.FetchDictionaryDataFromDatabase(strConnection, strDatabase, "checkbox");
            SortedDictionary<string, int> dictCheckCat = dictCheckboxArray[0];
            SortedDictionary<string, int> dictCheckVert = dictCheckboxArray[1];
            SortedDictionary<string, int> dictCheckPub = dictCheckboxArray[2];

            // Assemble a list of checkbox tests for Categories.
            CSharpGenerator.CreateTestCases(sb, "Marketplace_TestData_Checkbox_Category", "Category", dictCheckCat, "checkbox");

            // Assemble a list of checkbox tests for Verticals.
            CSharpGenerator.CreateTestCases(sb, "Marketplace_TestData_Checkbox_Vertical", "Vertical", dictCheckVert, "checkbox");

            // Assemble a list of checkbox tests for Publishers.
            CSharpGenerator.CreateTestCases(sb, "Marketplace_TestData_Checkbox_Publisher", "Publisher", dictCheckPub, "checkbox");

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




        //private static SortedDictionary<string, int>[] FetchDictionaryDataFromDatabase(string strConnectionSnapshot, string strDatabaseSnapshot, string strTestType)
        //{
        //    //
        //    // Fetch MongoDB data
        //    //
        //    MongoClient client = new MongoClient(strConnectionSnapshot);
        //    var db = client.GetDatabase(strDatabaseSnapshot);

        //    // Get lookup item details <Id, Name> from database
        //    var xx = QueryCollectionLookupItem(db);
        //    SortedDictionary<string, string> dictLookup = xx.Item1;
        //    SortedDictionary<string, string> dictReverseLookup = xx.Item2;

        //    // Get <Categories, Count> from database
        //    var dictCategory = QueryCategoriesFromMarketplaceItem(db, dictLookup, dictReverseLookup, strTestType);

        //    // Get <Verticals, Count> from database
        //    var dictVertical = QueryVerticalsFromMarketplaceItem(db, dictLookup);

        //    // Get a list of publishers <PublisherID, PublisherName> from database
        //    var dictPublisher = QueryPublishers(db);

        //    // Get a count of marketitems for each publisher <PublisherName, Count> from database
        //    SortedDictionary<string, int> dictPublisherItemCount = QueryItemCountPerPublisher(db, dictPublisher, dictLookup);

        //    SortedDictionary<string, int>[] dictArray = new SortedDictionary<string, int>[3];
        //    dictArray[0] = dictCategory;
        //    dictArray[1] = dictVertical;
        //    dictArray[2] = dictPublisherItemCount;
        //    return dictArray;
        //}


        //private static StringBuilder CreateTestCases(StringBuilder sbOutput, string strClassName, string strItemType, SortedDictionary<string, int> dict1, string strTestType)
        //{
        //    // Class header (declaration)
        //    sbOutput.AppendLine($"    public class {strClassName}");
        //    sbOutput.AppendLine("    {");
        //    sbOutput.AppendLine("        public static IEnumerable<object[]> MyData =>");
        //    sbOutput.AppendLine("            new List<object[]>");
        //    sbOutput.AppendLine("            {");

        //    // Created lines should look like this:
        //    //         new object[] { "checkbox", "Category", "Air Compressing", 2 },

        //    int iItem = 0;
        //    foreach (KeyValuePair<string, int> kvp1 in dict1)
        //    {
        //        string strItemName = kvp1.Key;
        //        int cExpected = kvp1.Value;
        //        sbOutput.AppendLine($"\t\tnew object[] {{\"{strTestType}\", \"{strItemType}\", \"{strItemName}\", {iItem}, {cExpected} }},");
        //        iItem++;
        //    }

        //    // Class footer
        //    sbOutput.AppendLine("            };");
        //    sbOutput.AppendLine("    }");

        //    return sbOutput;
        //}


        //private static IConfigurationRoot CreateConfiguration()
        //{
        //    var builder = new ConfigurationBuilder()
        //        .AddUserSecrets(Assembly.GetExecutingAssembly(), true);

        //    var config = builder.Build();
        //    return config;
        //}

        //private static SortedDictionary<string,string> QueryPublishers(IMongoDatabase db)
        //{
        //    var pubItems = db.GetCollection<Publisher>("Publisher");
        //    var docPublisher = pubItems.Find(new BsonDocument()).ToList();
        //    SortedDictionary<string, string> dict = new SortedDictionary<string, string>();
        //    foreach (Publisher p in docPublisher)
        //    {
        //        if (p.IsActive == true && p.Verified == true)
        //        {
        //            // Console.WriteLine($"{p.ID} -> {p.DisplayName}");
        //            dict.Add(p.ID, p.DisplayName);
        //        }
        //    }

        //    return dict;
        //}

        //private static SortedDictionary<string,int> QueryItemCountPerPublisher(IMongoDatabase db, SortedDictionary<string,string> dictPublishers, SortedDictionary<string, string> dictLookup)
        //{
        //    SortedDictionary<string,int> dictReturn = new SortedDictionary<string, int>();
        //    var items = db.GetCollection<MarketplaceItem>("MarketplaceItem");
        //    // var docMarketplaceItems = items.Find(new BsonDocument()).ToList();

        //    foreach (KeyValuePair<string,string> kvp in dictPublishers)
        //    {
        //        string strPublisherId = kvp.Key;
        //        string strPublisherName = kvp.Value;

        //        var myDocItems = items.Find($"{{ $and: [ {{ PublisherId : ObjectId('{strPublisherId}') }}, {{ IsActive : true }} ] }}");
        //        int cItems = 0;
        //        foreach (MarketplaceItem mi in myDocItems.ToList())
        //        {
        //            string strStatusId = mi.StatusId.ToString();
        //            if (dictLookup.ContainsKey(strStatusId))
        //            {
        //                string strStatus = dictLookup[strStatusId];
        //                if (strStatus == "Live")
        //                {
        //                    cItems++;
        //                }
        //            }
        //        }
        //        //var myitems = items.Find($"{{ $and: [ {{ PublisherId : ObjectId('{strPublisherId}') }}, {{ IsActive : true }} ] }}").CountDocuments();
        //        dictReturn.Add(strPublisherName, cItems);
        //    }

        //    return dictReturn;

        //}
        //private static SortedDictionary<string, int> QueryCategoriesFromMarketplaceItem(IMongoDatabase db, SortedDictionary<string, string> dictLookup, SortedDictionary<string, string> dictReverseLookup, string strTestType)
        //{
        //    // First Pass: Get a list of all categories and a starting count value.
        //    var items = db.GetCollection<MarketplaceItem>("MarketplaceItem");

        //    var docMarketplaceItems = items.Find(new BsonDocument()).ToList();
        //    SortedDictionary<string, int> dictCatPass1 = new SortedDictionary<string, int>();
        //    SortedDictionary<string, int> dictCatPass2 = new SortedDictionary<string, int>();

        //    string strLiveId = null;

        //    foreach (MarketplaceItem doc in docMarketplaceItems)
        //    {
        //        string strStatusId = doc.StatusId.ToString();
        //        if (dictLookup.ContainsKey(strStatusId))
        //        {
        //            string strStatus = dictLookup[strStatusId];
        //            if (strStatus == "Live" && doc.IsActive)
        //            {
        //                strLiveId = doc.StatusId.ToString();
        //                doc.astrCategories = ConvertListToStrings(doc.Categories, dictLookup);
        //                AddCountToSortedDictionary(dictCatPass1, doc.astrCategories);
        //            }
        //        }
        //    }

        //    if (strTestType == "checkbox")
        //    {
        //        return dictCatPass1;
        //    }

        //    // Second Pass: Add in category names that occur in fields "Abstract" and "Description"
        //    foreach (KeyValuePair<string, int> kvp in dictCatPass1)
        //    {
        //        // Need a query that looks something like this:
        //        // { $or: [ { $or: [{ "Categories" : ObjectId('618aa924557c7b88d5fb487b')},  { "Description" : /analytics/ } ]},  { "Abstract" : /analytics/ } ] }
        //        string strName = kvp.Key;
        //        string strObjectId = dictReverseLookup[strName]; ;

        //        // string strMyQuery = $"{{ $or: [ {{ $or: [{{ \"Categories\" : ObjectId('{strObjectId}')}},  {{ \"Description\" : /{strName}/i }} ]}},  {{ \"Abstract\" : /{strName}/i }} ] }}";
        //        string strMyQuery = $"{{ $and: [ {{\"StatusId\": ObjectId('{strLiveId}') }}, {{ $and: [ {{ \"IsActive\": true }}, {{ $or: [ {{ $or: [{{ \"Categories\" : ObjectId('{strObjectId}')}},  {{ \"Description\" : /{strName}/i }} ]}},  {{ \"Abstract\" : /{strName}/i }} ] }} ] }} ] }}";
        //        var myitems = items.Find(strMyQuery).CountDocuments();
        //        dictCatPass2.Add(strName, (int)myitems);
        //    }

        //    return dictCatPass2;
        //}

        //private static SortedDictionary<string, int> QueryVerticalsFromMarketplaceItem(IMongoDatabase db, SortedDictionary<string, string> dict)
        //{
        //    /// <summary>
        //    ///  Collection: MarketplaceItem
        //    /// </summary>
        //    /// <param name="args"></param>
        //    var items = db.GetCollection<MarketplaceItem>("MarketplaceItem");

        //    var docMarketplaceItems = items.Find(new BsonDocument()).ToList();
        //    SortedDictionary<string, int> dictVerticals = new SortedDictionary<string, int>();

        //    foreach (MarketplaceItem doc in docMarketplaceItems)
        //    {
        //        string strStatusId = doc.StatusId.ToString();
        //        if (dict.ContainsKey(strStatusId))
        //        {
        //            string strStatus = dict[strStatusId];
        //            if (strStatus == "Live" && doc.IsActive == true)
        //            {
        //                doc.astrVerticals = ConvertListToStrings(doc.IndustryVerticals, dict);
        //                AddCountToSortedDictionary(dictVerticals, doc.astrVerticals);
        //            }
        //        }
        //    }

        //    return dictVerticals;
        //}

        //private static (SortedDictionary<string, string>, SortedDictionary<string, string>) QueryCollectionLookupItem(IMongoDatabase db)
        //{
        //    /// <summary>
        //    ///  Collection: LookupItem
        //    /// </summary>
        //    /// <param name="args"></param>
        //    var lookupItems = db.GetCollection<LookupItem>("LookupItem");
        //    var docLookupItems = lookupItems.Find(new BsonDocument()).ToList();
        //    SortedDictionary<string, string> dict = new SortedDictionary<string, string>();
        //    SortedDictionary<string, string> dictReverse = new SortedDictionary<string, string>();
        //    foreach (LookupItem litem in docLookupItems)
        //    {
        //        if (litem.IsActive == true)
        //        {
        //            // Console.WriteLine($"{litem.ID} -> {litem.Name}");
        //            if (!litem.Name.StartsWith("SM "))
        //            {
        //                dict.Add(litem.ID, litem.Name);
        //                dictReverse.Add(litem.Name, litem.ID);
        //            }
        //        }
        //    }

        //    return (dict, dictReverse);
        //}

        //private static void AddCountToSortedDictionary(SortedDictionary<string, int> dict, List<string> list)
        //{
        //    foreach (string strKey in list)
        //    {
        //        if (dict.ContainsKey(strKey))
        //        {
        //            dict[strKey] += 1;
        //        }
        //        else
        //        {
        //            dict.Add(strKey, 1);
        //        }
        //    }
        //}




        //private static List<string> ConvertListToStrings(List<BsonObjectId> alist, SortedDictionary<string, string> dictLookupItems)
        //{
        //    List<string> listStrings = new List<string>();
        //    foreach (BsonObjectId boid in alist)
        //    {
        //        string strBoid = boid.ToString();
        //        if (dictLookupItems.ContainsKey(strBoid))
        //        {
        //            listStrings.Add(dictLookupItems[strBoid]);
        //        }
        //        else
        //        {
        //            // We don't need to flag these. Just indicates items that are disabled
        //            // for some reason. Won't cause us any problems.
        //            // Console.WriteLine($"Cannot find {strBoid} in lookup table");
        //        }
        //    }

        //    return listStrings;
        //}
    } // End Class Program


} // End Namespace MP_TestGenerator
