namespace MP_TestGenerator
{
    using MongoDB.Bson;
    using MongoDB.Driver;
    using Microsoft.Extensions.Configuration;
    using MP_TestGenerator.Entities;
    using MongoDB.Bson.Serialization.Attributes;
    using System.Text;

    internal class Program
    {
        #pragma warning disable CS0120 // An object reference is required for the non-static field, method, or property 'Program.dictLookupItems'

        static void Main(string[] args)
        {
            IConfigurationRoot config = CreateConfiguration();

            string strConnection = config["MongoDBSnapshot2023-07-19:ConnectionString"];
            string strDatabase = config["MongoDBSnapshot2023-07-19:DatabaseName"];

            // Two sets of test parameters are generated. The output of this program 
            // consists of lines of text for use a inputs to two separate test functions.
            StringBuilder sbTextQuery = CreateAndOutputTestSet(strConnection, strDatabase, "textquery");
            StringBuilder sbCheckBox = CreateAndOutputTestSet(strConnection, strDatabase, "checkbox");
            StringBuilder sbLookup = GenerateCSharpLookupTables(strConnection, strDatabase);

            // Dump all of the tests
            Console.WriteLine("// ============================================================");
            Console.WriteLine("// ============================================================");
            Console.WriteLine("// Tests of type 'textquery'");
            Console.WriteLine(sbTextQuery.ToString());

            Console.WriteLine("// ============================================================");
            Console.WriteLine("// ============================================================");
            Console.WriteLine("// Tests of type 'checkbox'");
            Console.WriteLine(sbCheckBox.ToString());

            Console.WriteLine("// ============================================================");
            Console.WriteLine("// ============================================================");
            Console.WriteLine("// Lookup tables of all available publishers, verticals, and processes (categories)");
            Console.WriteLine(sbLookup.ToString());
        }


        private static StringBuilder GenerateCSharpLookupTables(string strConnection, string strDatabase)
        {
            //
            // Fetch MongoDB data
            //
            MongoClient client = new MongoClient(strConnection);
            var db = client.GetDatabase(strDatabase);

            // Get lookup item details <Id, Name> for production database
            var xx = QueryCollectionLookupItem(db);
            SortedDictionary<string, string> dictLookup = xx.Item1;
            SortedDictionary<string, string> dictReverseLookup = xx.Item2;

            // Get <Categories, Count> for production database
            var dictCategoryProduction = QueryCategoriesFromMarketplaceItem(db, dictLookup, dictReverseLookup, "checkbox");

            // Get <Verticals, Count> for production database
            var dictVerticalProduction = QueryVerticalsFromMarketplaceItem(db, dictLookup);

            // Get a list of publishers <PublisherID, PublisherName> for production database
            var dictPublisherProduction = QueryPublishers(db);

            // Get a count of marketitems for each publisher <PublisherName, Count> for production database
            SortedDictionary<string, int> dictPublisherItemCountProduction = QueryItemCountPerPublisher(db, dictPublisherProduction);

            StringBuilder sb = new StringBuilder();

            int cProductionCat = BuildTestLookupTable<string, int>(sb, "astrCategories", dictCategoryProduction);
            int cProductionVert = BuildTestLookupTable<string, int>(sb, "astrVerticals", dictVerticalProduction);
            int cProductionPub = BuildTestLookupTable<string, int>(sb, "astrPublishers", dictPublisherItemCountProduction);

            return sb;
        }
        private static StringBuilder CreateAndOutputTestSet(string strConnectionSnapshot, string strDatabaseSnapshot, string strTestType)
        {
            //
            // Fetch MongoDB data
            //
            MongoClient client = new MongoClient(strConnectionSnapshot);
            var db = client.GetDatabase(strDatabaseSnapshot);

            // Get lookup item details <Id, Name> for production database
            var xx = QueryCollectionLookupItem(db);
            SortedDictionary<string, string> dictLookupProduction = xx.Item1;
            SortedDictionary<string, string> dictReverseLookup = xx.Item2;

            // Get <Categories, Count> for production database
            var dictCategoryProduction = QueryCategoriesFromMarketplaceItem(db, dictLookupProduction, dictReverseLookup, strTestType);

            // Get <Verticals, Count> for production database
            var dictVerticalProduction = QueryVerticalsFromMarketplaceItem(db, dictLookupProduction);

            // Get a list of publishers <PublisherID, PublisherName> for production database
            var dictPublisherProduction = QueryPublishers(db);

            // Get a count of marketitems for each publisher <PublisherName, Count> for production database
            SortedDictionary<string, int> dictPublisherItemCountProduction = QueryItemCountPerPublisher(db, dictPublisherProduction);

            // Assemble a list of tests for Categories.
            StringBuilder sb = new StringBuilder();
            StringBuilder sbCategory = CreateTestCases(sb, "Category", dictCategoryProduction, strTestType);

            // Assemble a list of tests for Verticals.
            StringBuilder sbVertical = CreateTestCases(sb, "Vertical", dictVerticalProduction, strTestType);

            // Assemble a list of tests for Publishers.
            StringBuilder sbPublisher = CreateTestCases(sb, "Publisher", dictPublisherItemCountProduction, strTestType);

            return sb;
        }

        private static int BuildTestLookupTable<TKey, TValue>(StringBuilder sb, string strArrayName, SortedDictionary<TKey,TValue> dictInput)
        {
            int cItems = 0;
            sb.AppendLine($"\tprivate static string [] {strArrayName} = ");
            sb.AppendLine($"\t{{");
            foreach (KeyValuePair<TKey, TValue> kvp1 in dictInput)
            {
                if (kvp1.Key != null)
                {
                    #pragma warning disable CS8602 // Dereference of a possibly null reference.
                    string strKey = kvp1.Key.ToString().Trim();
                    sb.AppendLine($"\t\"{strKey}\",");
                    cItems++;
                }
            }
            sb.AppendLine($"\t}};");
            sb.AppendLine($"");

            return cItems;
        }

        private static StringBuilder CreateTestCases(StringBuilder sbOutput, string strItemType, SortedDictionary<string, int> dict1, string strTestType)
        {

            // Created lines should look like this:
            // For categories:
            // [TestCase(<test type>, <Category Name>,  "Category", <Expected Count>)]
            // For verticals:
            // [TestCase(<test type>, <Vertical Name>, "Vertical", <Expected Count>)]
            foreach (KeyValuePair<string, int> kvp1 in dict1)
            {
                string strItemName = kvp1.Key;
                int cExpected = kvp1.Value;
                sbOutput.AppendLine($"[TestCase(\"{strTestType}\", \"{strItemType}\", \"{strItemName}\", {cExpected})]");
            }

            return sbOutput;
        }


        private static IConfigurationRoot CreateConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("C:\\Users\\paul\\AppData\\Roaming\\Microsoft\\UserSecrets\\f0812c0d-9826-4b23-a1fb-ba0eb9cc9e2c\\secrets.json", false);

            var config = builder.Build();
            return config;
        }

        private static SortedDictionary<string,string> QueryPublishers(IMongoDatabase dbProduction)
        {
            var pubItems = dbProduction.GetCollection<Publisher>("Publisher");
            var docPublisher = pubItems.Find(new BsonDocument()).ToList();
            SortedDictionary<string, string> dict = new SortedDictionary<string, string>();
            foreach (Publisher p in docPublisher)
            {
                if (p.IsActive == true && p.Verified == true)
                {
                    // Console.WriteLine($"{p.ID} -> {p.DisplayName}");
                    dict.Add(p.ID, p.DisplayName);
                }
            }

            return dict;
        }

        private static SortedDictionary<string,int> QueryItemCountPerPublisher(IMongoDatabase db, SortedDictionary<string,string> dictPublishers)
        {
            SortedDictionary<string,int> dictReturn = new SortedDictionary<string, int>();
            var items = db.GetCollection<MarketplaceItem>("MarketplaceItem");
            // var docMarketplaceItems = items.Find(new BsonDocument()).ToList();

            foreach (KeyValuePair<string,string> kvp in dictPublishers)
            {
                string strPublisherId = kvp.Key;
                string strPublisherName = kvp.Value;

                var myitems = items.Find($"{{ $and: [ {{ PublisherId : ObjectId('{strPublisherId}') }}, {{ IsActive : true }} ] }}").CountDocuments();
                dictReturn.Add(strPublisherName, (int)myitems);
            }

            return dictReturn;

        }
        private static SortedDictionary<string, int> QueryCategoriesFromMarketplaceItem(IMongoDatabase db, SortedDictionary<string, string> dictLookup, SortedDictionary<string, string> dictReverseLookupProduction, string strTestType)
        {
            // First Pass: Get a list of all categories and a starting count value.
            var items = db.GetCollection<MarketplaceItem>("MarketplaceItem");

            var docMarketplaceItems = items.Find(new BsonDocument()).ToList();
            SortedDictionary<string, int> dictCatPass1 = new SortedDictionary<string, int>();
            SortedDictionary<string, int> dictCatPass2 = new SortedDictionary<string, int>();

            string strLiveId = null;

            foreach (MarketplaceItem doc in docMarketplaceItems)
            {
                string strStatusId = doc.StatusId.ToString();
                if (dictLookup.ContainsKey(strStatusId))
                {
                    string strStatus = dictLookup[strStatusId];
                    if (strStatus == "Live" && doc.IsActive)
                    {
                        strLiveId = doc.StatusId.ToString();
                        doc.astrCategories = ConvertListToStrings(doc.Categories, dictLookup);
                        AddCountToSortedDictionary(dictCatPass1, doc.astrCategories);
                    }
                }
            }

            if (strTestType == "checkbox")
            {
                return dictCatPass1;
            }

            // Second Pass: Add in category names that occur in fields "Abstract" and "Description"
            foreach (KeyValuePair<string, int> kvp in dictCatPass1)
            {
                // Need a query that looks something like this:
                // { $or: [ { $or: [{ "Categories" : ObjectId('618aa924557c7b88d5fb487b')},  { "Description" : /analytics/ } ]},  { "Abstract" : /analytics/ } ] }
                string strName = kvp.Key;
                string strObjectId = dictReverseLookupProduction[strName]; ;

                // string strMyQuery = $"{{ $or: [ {{ $or: [{{ \"Categories\" : ObjectId('{strObjectId}')}},  {{ \"Description\" : /{strName}/i }} ]}},  {{ \"Abstract\" : /{strName}/i }} ] }}";
                string strMyQuery = $"{{ $and: [ {{\"StatusId\": ObjectId('{strLiveId}') }}, {{ $and: [ {{ \"IsActive\": true }}, {{ $or: [ {{ $or: [{{ \"Categories\" : ObjectId('{strObjectId}')}},  {{ \"Description\" : /{strName}/i }} ]}},  {{ \"Abstract\" : /{strName}/i }} ] }} ] }} ] }}";
                var myitems = items.Find(strMyQuery).CountDocuments();
                dictCatPass2.Add(strName, (int)myitems);
            }

            return dictCatPass2;
        }

        private static SortedDictionary<string, int> QueryVerticalsFromMarketplaceItem(IMongoDatabase db, SortedDictionary<string, string> dict)
        {
            /// <summary>
            ///  Collection: MarketplaceItem
            /// </summary>
            /// <param name="args"></param>
            var items = db.GetCollection<MarketplaceItem>("MarketplaceItem");

            var docMarketplaceItems = items.Find(new BsonDocument()).ToList();
            SortedDictionary<string, int> dictVerticals = new SortedDictionary<string, int>();

            foreach (MarketplaceItem doc in docMarketplaceItems)
            {
                string strStatusId = doc.StatusId.ToString();
                if (dict.ContainsKey(strStatusId))
                {
                    string strStatus = dict[strStatusId];
                    if (strStatus == "Live" && doc.IsActive == true)
                    {
                        doc.astrVerticals = ConvertListToStrings(doc.IndustryVerticals, dict);
                        AddCountToSortedDictionary(dictVerticals, doc.astrVerticals);
                    }
                }
            }

            return dictVerticals;
        }

        private static (SortedDictionary<string, string>, SortedDictionary<string, string>) QueryCollectionLookupItem(IMongoDatabase dbProduction)
        {
            /// <summary>
            ///  Collection: LookupItem
            /// </summary>
            /// <param name="args"></param>
            var lookupItems = dbProduction.GetCollection<LookupItem>("LookupItem");
            var docLookupItems = lookupItems.Find(new BsonDocument()).ToList();
            SortedDictionary<string, string> dict = new SortedDictionary<string, string>();
            SortedDictionary<string, string> dictReverse = new SortedDictionary<string, string>();
            foreach (LookupItem litem in docLookupItems)
            {
                //if (litem.IsActive == true && 
                //    (litem.LookupType.EnumValue == CESMII.Marketplace.Common.Enums.LookupTypeEnum.Process ||
                //     litem.LookupType.EnumValue == CESMII.Marketplace.Common.Enums.LookupTypeEnum.IndustryVertical))
                //{
                //    dict.Add(litem.ID, litem.Name);
                //    dictReverse.Add(litem.Name, litem.ID);
                //}
                if (litem.IsActive == true)
                {
                    // Console.WriteLine($"{litem.ID} -> {litem.Name}");
                    if (!litem.Name.StartsWith("SM "))
                    {
                        dict.Add(litem.ID, litem.Name);
                        dictReverse.Add(litem.Name, litem.ID);
                    }
                }
            }

            return (dict, dictReverse);
        }

        private static void AddCountToSortedDictionary(SortedDictionary<string, int> dict, List<string> list)
        {
            foreach (string strKey in list)
            {
                if (dict.ContainsKey(strKey))
                {
                    dict[strKey] += 1;
                }
                else
                {
                    dict.Add(strKey, 1);
                }
            }
        }




        private static List<string> ConvertListToStrings(List<BsonObjectId> alist, SortedDictionary<string, string> dictLookupItems)
        {
            List<string> listStrings = new List<string>();
            foreach (BsonObjectId boid in alist)
            {
                string strBoid = boid.ToString();
                if (dictLookupItems.ContainsKey(strBoid))
                {
                    listStrings.Add(dictLookupItems[strBoid]);
                }
                else
                {
                    Console.WriteLine($"Cannot find {strBoid} in lookup table");
                }
            }

            return listStrings;
        }
    } // End Class Program


} // End Namespace MP_TestGenerator
