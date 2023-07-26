using MongoDB.Bson;
using MongoDB.Driver;
using MP_TestGenerator.Entities;

namespace MP_TestGenerator
{
    internal class MongoAccess
    {
        public static SortedDictionary<string, int>[] FetchDictionaryDataFromDatabase(string strConnectionSnapshot, string strDatabaseSnapshot, string strTestType)
        {
            //
            // Fetch MongoDB data
            //
            MongoClient client = new MongoClient(strConnectionSnapshot);
            var db = client.GetDatabase(strDatabaseSnapshot);

            // Get lookup item details <Id, Name> from database
            var xx = QueryCollectionLookupItem(db);
            SortedDictionary<string, string> dictLookup = xx.Item1;
            SortedDictionary<string, string> dictReverseLookup = xx.Item2;

            // Get <Categories, Count> from database
            var dictCategory = QueryCategoriesFromMarketplaceItem(db, dictLookup, dictReverseLookup, strTestType);

            // Get <Verticals, Count> from database
            var dictVertical = QueryVerticalsFromMarketplaceItem(db, dictLookup);

            // Get a list of publishers <PublisherID, PublisherName> from database
            var dictPublisher = QueryPublishers(db);

            // Get a count of marketitems for each publisher <PublisherName, Count> from database
            SortedDictionary<string, int> dictPublisherItemCount = QueryItemCountPerPublisher(db, dictPublisher, dictLookup);

            SortedDictionary<string, int>[] dictArray = new SortedDictionary<string, int>[3];
            dictArray[0] = dictCategory;
            dictArray[1] = dictVertical;
            dictArray[2] = dictPublisherItemCount;
            return dictArray;
        }

        public static (SortedDictionary<string, string>, SortedDictionary<string, string>) QueryCollectionLookupItem(IMongoDatabase db)
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
                    if (!litem.Name.StartsWith("SM "))
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

        private static SortedDictionary<string, int> QueryItemCountPerPublisher(IMongoDatabase db, SortedDictionary<string, string> dictPublishers, SortedDictionary<string, string> dictLookup)
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
                    if (dictLookup.ContainsKey(strStatusId))
                    {
                        string strStatus = dictLookup[strStatusId];
                        if (strStatus == "Live")
                        {
                            cItems++;
                        }
                    }
                }
                //var myitems = items.Find($"{{ $and: [ {{ PublisherId : ObjectId('{strPublisherId}') }}, {{ IsActive : true }} ] }}").CountDocuments();
                dictReturn.Add(strPublisherName, cItems);
            }

            return dictReturn;

        }
        private static SortedDictionary<string, int> QueryCategoriesFromMarketplaceItem(IMongoDatabase db, SortedDictionary<string, string> dictLookup, SortedDictionary<string, string> dictReverseLookup, string strTestType)
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
                string strObjectId = dictReverseLookup[strName]; ;

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
