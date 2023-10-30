using MongoDB.Bson;
using MongoDB.Driver;
using MP_TestGenerator.Entities;
using System.Collections;
using System.Text;

namespace MP_TestGenerator
{
    internal class MongoAccess
    {
        public static SortedDictionary<string, int>[] FetchDictionaryDataFromDatabase(string strConnectionSnapshot, string strDatabaseSnapshot, string strTestType, out int cMaxItems, bool bVerbose = false)
        {
            //
            // Fetch MongoDB data
            //
            MongoClient client = new MongoClient(strConnectionSnapshot);
            var db = client.GetDatabase(strDatabaseSnapshot);

            // Get Processes (Category) lookup item details <Id, Name> from database
            var xx = QueryCollectionLookupItem(db, "Processes");
            SortedDictionary<string, string> dictCategoriesLookup = xx.Item1;
            SortedDictionary<string, string> dictCategoriesReverseLookup = xx.Item2;

            var vert = QueryCollectionLookupItem(db, "Industry Vertical");
            SortedDictionary<string, string> dictVerticalLookup = vert.Item1;
            SortedDictionary<string, string> dictVerticalReverseLookup = vert.Item2;

            var stat = QueryCollectionLookupItem(db, "Marketplace Status");
            SortedDictionary<string, string> dictStatus = stat.Item1;
            string strStatusLive = dictStatus.ContainsValue("Live") ? dictStatus.FirstOrDefault(x => x.Value == "Live").Key : string.Empty; 

            cMaxItems = QueryMaxMarketplaceItems(db, strStatusLive);

            // Get a list of publishers <PublisherID, PublisherName> from database
            var dictPublisher = QueryPublishers(db);

            // Get <Categories, Count> from database
            var dictCategory = QueryCategoriesFromMarketplaceItem(db, strStatusLive, dictCategoriesLookup, dictCategoriesReverseLookup, dictVerticalLookup, dictVerticalReverseLookup, strTestType, dictPublisher, bVerbose);

            // Get <Verticals, Count> from database
            var dictVertical = QueryVerticalsFromMarketplaceItem(db, strStatusLive, dictCategoriesLookup, dictCategoriesReverseLookup, dictVerticalLookup, dictVerticalReverseLookup, strTestType, dictPublisher, bVerbose);

            // Get a count of marketitems for each publisher <PublisherName, Count> from database
            SortedDictionary<string, int> dictPublisherItemCount = QueryItemCountPerPublisher(db, strStatusLive, dictPublisher, bVerbose);

            SortedDictionary<string, int>[] dictArray = new SortedDictionary<string, int>[3];
            dictArray[0] = dictCategory;
            dictArray[1] = dictVertical;
            dictArray[2] = dictPublisherItemCount;
            return dictArray;
        }

        public static int QueryMaxMarketplaceItems(IMongoDatabase db, string strStatusLive)
        {
            int cMaxItems = 0;
            var items = db.GetCollection<MarketplaceItem>("MarketplaceItem");
            string strMyQuery = $"{{ $and: [ {{ \"StatusId\": ObjectId('{strStatusLive}') }}, {{ \"IsActive\": true }}] }}";
            var mySelectedItems = items.Find(strMyQuery);
            var mySelectedList = mySelectedItems.ToList();
            cMaxItems = mySelectedList.Count;

            return cMaxItems;
        }

        public static (SortedDictionary<string, string>, SortedDictionary<string, string>) QueryCollectionLookupItem(IMongoDatabase db, string strLookupTypeName)
        {
            /// <summary>
            ///  Collection: LookupItem
            /// </summary>
            /// <param name="args"></param>
            var lookupItems = db.GetCollection<LookupItem>("LookupItem");
            var docLookupItems = lookupItems.Find(new BsonDocument()).ToList();
            SortedDictionary<string, string> dict = new SortedDictionary<string, string>();
            SortedDictionary<string, string> dictReverse = new SortedDictionary<string, string>();
            foreach (LookupItem litem in docLookupItems)
            {
                if (litem.IsActive == true)
                {
                    // Console.WriteLine($"{litem.ID} -> {litem.Name}");
                    if (litem.LookupType.Name == strLookupTypeName)
                    {
                        dict.Add(litem.ID, litem.Name);
                        dictReverse.Add(litem.Name, litem.ID);
                    }
                }
            }

            return (dict, dictReverse);
        }

        private static SortedDictionary<string, string> QueryPublishers(IMongoDatabase db)
        {
            var pubItems = db.GetCollection<Publisher>("Publisher");
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

        private static SortedDictionary<string, int> QueryItemCountPerPublisher(IMongoDatabase db, string strStatusLive, SortedDictionary<string, string> dictPublishers, bool bVerbose = false)
        {
            SortedDictionary<string, int> dictReturn = new SortedDictionary<string, int>();
            var items = db.GetCollection<MarketplaceItem>("MarketplaceItem");
            // var docMarketplaceItems = items.Find(new BsonDocument()).ToList();

            foreach (KeyValuePair<string, string> kvp in dictPublishers)
            {
                string strPublisherId = kvp.Key;
                string strPublisherName = kvp.Value;

                var myDocItems = items.Find($"{{ $and: [ {{ PublisherId : ObjectId('{strPublisherId}') }}, {{ IsActive : true }} ] }}");
                int cItems = 0;
                foreach (MarketplaceItem mi in myDocItems.ToList())
                {
                    string strStatusId = mi.StatusId.ToString();
                    if (strStatusId == strStatusLive)
                    {
                        cItems++;
                        if (bVerbose)
                        {

                        }
                    }
                }
                //var myitems = items.Find($"{{ $and: [ {{ PublisherId : ObjectId('{strPublisherId}') }}, {{ IsActive : true }} ] }}").CountDocuments();
                dictReturn.Add(strPublisherName, cItems);
            }

            return dictReturn;

        }
        private static SortedDictionary<string, int> QueryCategoriesFromMarketplaceItem(IMongoDatabase db, string strStatusLive, SortedDictionary<string, string> dictCategories, SortedDictionary<string, string> dictReverseCategories, SortedDictionary<string, string> dictVertical, SortedDictionary<string, string> dictReverseVertical, string strTestType, SortedDictionary<string, string> dictPublisher, bool bVerbose = false)
        {
            // First Pass: Get a list of all categories and a starting count value.
            var items = db.GetCollection<MarketplaceItem>("MarketplaceItem");

            var docMarketplaceItems = items.Find(new BsonDocument()).ToList();
            SortedDictionary<string, int> dictCatPass1 = new SortedDictionary<string, int>();
            SortedDictionary<string, int> dictCatPass2 = new SortedDictionary<string, int>();

            int iItem = 1;

            foreach (MarketplaceItem doc in docMarketplaceItems)
            {
                string strStatusId = doc.StatusId.ToString();
                if (strStatusId == strStatusLive && doc.IsActive)
                { 
                    doc.astrCategories = ConvertListToStrings(doc.Categories, dictCategories);
                    AddCountToSortedDictionary(dictCatPass1, doc.astrCategories);
                    if (bVerbose)
                    {
                        string strPublisher = dictPublisher[doc.PublisherId.ToString()];
                        Console.WriteLine($"{iItem}) {strPublisher} {doc.DisplayName}");
                        string [] astrCategories = doc.astrCategories.ToArray();
                        for (int i = 0; i < astrCategories.Length; i++)
                        {
                            Console.WriteLine($"\t{iItem}.{i})\t{doc.astrCategories[i]}");
                        }   
                    }

                    iItem++;
                }
            }

            // And that is enough when handling user checkbox selections.
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
                if (strName == "Industrial Software")
                {
                    Console.WriteLine("Debug");
                }
                string strObjectId = dictReverseCategories[strName]; ;
                string strCatSearch = $" ObjectId('{strObjectId}') ";
                StringBuilder sb = new StringBuilder();
                int cContains = 0;
                string strSep = "{ $in: [";
                foreach (KeyValuePair<string, string> kvpReverse in dictReverseCategories)
                {
                    // Only count categories that contain the name of the target category.
                    if (strObjectId != kvpReverse.Value)
                    {
                        string strCategory = kvpReverse.Key;
                        if (strCategory.Contains(strName))
                        {
                            sb.Append($"{strSep} ObjectId('{kvpReverse.Value}')");
                            strSep = ",";
                            cContains++;
                        }
                    }
                }

                if (cContains > 0)
                {
                    // When searching for more than 1 category, we include the $in operator and an array of values.
                    sb.Append($", ObjectId('{strObjectId}') ] }} ");
                    strCatSearch = sb.ToString();
                }

                // string strMyQuery = $"{{ $or: [ {{ $or: [{{ \"Categories\" : ObjectId('{strObjectId}')}},  {{ \"Description\" : /{strName}/i }} ]}},  {{ \"Abstract\" : /{strName}/i }} ] }}";
                string strMyQuery = $"{{ $and: [ {{\"StatusId\": ObjectId('{strStatusLive}') }}, {{ $and: [ {{ \"IsActive\": true }}, {{ $or: [ {{ $or: [{{ \"Categories\" : {strCatSearch} }},  {{ \"Description\" : /{strName}/i }} ]}},  {{ \"Abstract\" : /{strName}/i }} ] }} ] }} ] }}";
                var mySelectedItems = items.Find(strMyQuery);
                var mySelectedList = mySelectedItems.ToList();
                var myCount = mySelectedItems.CountDocuments();

                // Somehow we do this...
                // AddCountToSortedDictionary(dictCatPass1, doc.astrCategories);
                dictCatPass2.Add(strName, (int)myCount);
                if (bVerbose)
                {

                }
            }

            return dictCatPass2;
        }

        private static SortedDictionary<string, int> QueryVerticalsFromMarketplaceItem(IMongoDatabase db, string strStatusLive, SortedDictionary<string, string> dictCategories, SortedDictionary<string, string> dictReverseCategories, SortedDictionary<string, string> dictVertical, SortedDictionary<string, string> dictReverseVertical, string strTestType, SortedDictionary<string, string> dictPublishers, bool bVerbose = false)
        {
            /// <summary>
            ///  Collection: MarketplaceItem
            /// </summary>
            /// <param name="args"></param>
            var items = db.GetCollection<MarketplaceItem>("MarketplaceItem");

            bool bShowDetails = false;

            var docMarketplaceItems = items.Find(new BsonDocument()).ToList();
            SortedDictionary<string, int> dictVerticals = new SortedDictionary<string, int>();
            SortedDictionary<string, int> dictVertPass2 = new SortedDictionary<string, int>();

            foreach (MarketplaceItem doc in docMarketplaceItems)
            {
                string strStatusId = doc.StatusId.ToString();
                if (strStatusId == strStatusLive && doc.IsActive == true)
                {
                    doc.astrVerticals = ConvertListToStrings(doc.IndustryVerticals, dictVertical);
                    if (bVerbose)
                    {

                    }
                    AddCountToSortedDictionary(dictVerticals, doc.astrVerticals);
                }
            }

            if (strTestType == "checkbox")
                return dictVerticals;


            // Second Pass: Add in category names that occur in fields "Abstract" and "Description"
            foreach (KeyValuePair<string, int> kvp in dictVerticals)
            {
                ArrayList alOR = new ArrayList();

                // For each match, include a single search term (to be included in a large OR statement)
                // {{ "IndustryVerticals" : ObjectId('618aa924557c7b88d5fb487b') }}
                string strVerticalName = kvp.Key;


                if (strVerticalName == "Chemical")
                {
                    bShowDetails = true;
                    Console.WriteLine($"---------------------------- {strVerticalName} ----------------------------");
                }
                else
                {
                    bShowDetails = false;
                }

                string strObjectId = dictReverseVertical[strVerticalName]; ;
                alOR.Add($"{{ \"IndustryVerticals\" : ObjectId('{strObjectId}') }}");

                // Console.WriteLine($"QueryVerticalsFromMarketplaceItem: {strVerticalName}");

                // Include this vertical in the search
                string strVerticalSearch = $" ObjectId('{strObjectId}') ";

                // Search for verticals that are contained in the name of other verticals. Include this.
                foreach (KeyValuePair<string, string> kvpReverseVertical in dictReverseVertical)
                {
                    string strVertical = kvpReverseVertical.Key;
                    if (strVertical.Contains(strVerticalName))
                    {
                        alOR.Add($"{{ \"IndustryVerticals\" : ObjectId('{kvpReverseVertical.Value}') }}");
                        if (bShowDetails)
                            Console.WriteLine($"QueryVerticalsFromMarketplaceItem: Vertical: {strVerticalName}");
                    }
                }


                // Search for categories (processes) that contain the name or our target vertical.
                foreach (KeyValuePair<string, string> kvpReverseCategories in dictReverseCategories)
                {
                    string strCategory = kvpReverseCategories.Key;
                    if (strCategory.Contains(strVerticalName))
                    {
                        alOR.Add($"{{ \"Categories\" : ObjectId('{kvpReverseCategories.Value}') }}");
                        if (bShowDetails)
                            Console.WriteLine($"QueryVerticalsFromMarketplaceItem: Category: {kvpReverseCategories.Value}");
                    }
                }

                alOR.Add($" {{ \"Description\" : /{strVerticalName}/i }} ");
                alOR.Add($" {{ \"Abstract\" : /{strVerticalName}/i }} ");
                StringBuilder sb = new StringBuilder();
                for (int iItem = 0; iItem < alOR.Count; iItem++)
                {
                    sb.Append(alOR[iItem]);
                    if (iItem < alOR.Count - 1)
                        sb.Append(", ");
                }
                string strOrExpression = sb.ToString();
                if (bShowDetails)
                    Console.WriteLine($"QueryVerticalsFromMarketplaceItem: strOrExpression: {strOrExpression}");


                string strMyQuery = $"{{ $and: [ {{\"StatusId\": ObjectId('{strStatusLive}') }}, {{ $and: [ {{ \"IsActive\": true }}, {{ $or: [ {strOrExpression} ] }} ] }} ] }}";

                if (bShowDetails)
                    Console.WriteLine($"QueryVerticalsFromMarketplaceItem: Query: {strMyQuery}");

                var mySelectedItems = items.Find(strMyQuery);
                var mySelectedList = mySelectedItems.ToList();
                var myCount = mySelectedItems.CountDocuments();

                if (bShowDetails)
                {
                    Console.WriteLine($"---------------------------- {strVerticalName} Count: {myCount}----------------------------");
                }

                // Somehow we do this...
                // AddCountToSortedDictionary(dictCatPass1, doc.astrCategories);
                dictVertPass2.Add(strVerticalName, (int)myCount);
                if (bVerbose)
                {

                }
            }

            return dictVertPass2;
        }

        //private static void asdf()
        //{
        //    // Search for categories that contain the name of the target vertical. Include this also. Hmm.
        //    string strCategorySearch = $"";
        //    StringBuilder sbCategories = new StringBuilder();
        //    int cCategoryContains = 0;
        //    string strSep = "{ $in: [";
        //    foreach (KeyValuePair<string, string> kvpReverse in dictReverseCategories)
        //    {
        //        string strCategory = kvpReverse.Key;
        //        if (strCategory.Contains(strVerticalName))
        //        {
        //            sbCategories.Append($"{strSep} ObjectId('{kvpReverse.Value}')");
        //            strSep = ",";
        //        cCategoryContains++;
        //    }
        //    }
        //    if (cCategoryContains > 0)
        //    {
        //        // When searching for more than 1 category, we include the $in operator and an array of values.
        //        sbCategories.Append($", ObjectId('{strObjectId}') ] }} ");
        //        strCategorySearch = sbCategories.ToString();
        //    }

        //}

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
                    // We don't need to flag these. Just indicates items that are disabled
                    // for some reason. Won't cause us any problems.
                    // Console.WriteLine($"Cannot find {strBoid} in lookup table");
                }
            }

            return listStrings;
        }

    }
}
