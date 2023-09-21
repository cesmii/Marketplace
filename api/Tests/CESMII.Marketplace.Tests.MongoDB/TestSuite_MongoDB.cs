namespace CESMII.Marketplace.MongoDB
{
    using System;
    using CESMII.Marketplace.Data.Entities;
    using global::MongoDB.Bson;
    using global::MongoDB.Driver;
    using MongoDB;

    public class TestSuite_MongoDB
    {
        // Need to have a connection string and a database name.

        //string strConnection = "mongodb://testuser:password@localhost:27017";
        public string strConnection;

        //string strDatabase = "test";
        public string strDatabase;

        public TestSuite_MongoDB()
        {
            strConnection = utils.GetEnvString("MARKETPLACE_MONGODB_CONNECTIONSTRING");
            strDatabase = utils.GetEnvString("MARKETPLACE_MONGODB_DATABASE");
        }

        [Fact]
        public void ValidConnectionString_on_Startup()
        {
            Assert.NotNull(strConnection);
            Assert.NotNull(strDatabase);

            MongoClient client = new MongoClient(strConnection);
            Assert.NotNull(client);
        }

        [Fact]
        public void ValidDatabase_on_Startup()
        {
            Assert.NotNull(strConnection);
            Assert.NotNull(strDatabase);

            MongoClient client = new MongoClient(strConnection);

            var db = client.GetDatabase(strDatabase);
            Assert.NotNull(db);
        }

        [Fact]
        public void Lookup_Collection_Found_on_Startup()
        {
            Assert.NotNull(strConnection);
            Assert.NotNull(strDatabase);

            MongoClient client = new MongoClient(strConnection);

            var db = client.GetDatabase(strDatabase);

            var lookupItems = db.GetCollection<LookupItem>("LookupItem");
            Assert.NotNull(lookupItems);

            var docLookupItems = lookupItems.Find(new BsonDocument()).ToList();

            Assert.NotNull(docLookupItems);
            Assert.True(docLookupItems.Any());
        }

        [Fact]
        public void JobLog_Collection_Found_on_Startup()
        {
            Assert.NotNull(strConnection);
            Assert.NotNull(strDatabase);

            MongoClient client = new MongoClient(strConnection);

            var db = client.GetDatabase(strDatabase);

            var lookupItems = db.GetCollection<LookupItem>("JobLog");
            Assert.NotNull(lookupItems);

            var docLookupItems = lookupItems.Find(new BsonDocument()).ToList();

            Assert.NotNull(docLookupItems);
            Assert.True(docLookupItems.Any());
        }
        [Fact]
        public void LookupItem_Collection_Found_on_Startup()
        {
            Assert.NotNull(strConnection);
            Assert.NotNull(strDatabase);

            MongoClient client = new MongoClient(strConnection);

            var db = client.GetDatabase(strDatabase);

            var lookupItems = db.GetCollection<LookupItem>("LookupItem");
            Assert.NotNull(lookupItems);

            var docLookupItems = lookupItems.Find(new BsonDocument()).ToList();

            Assert.NotNull(docLookupItems);
            Assert.True(docLookupItems.Any());
        }
        [Fact]
        public void MarketplaceItem_Collection_Found_on_Startup()
        {
            Assert.NotNull(strConnection);
            Assert.NotNull(strDatabase);

            MongoClient client = new MongoClient(strConnection);

            var db = client.GetDatabase(strDatabase);

            var lookupItems = db.GetCollection<LookupItem>("MarketplaceItem");
            Assert.NotNull(lookupItems);

            var docLookupItems = lookupItems.Find(new BsonDocument()).ToList();

            Assert.NotNull(docLookupItems);
            Assert.True(docLookupItems.Any());
        }
        [Fact]
        public void MarketplaceItemAnalytics_Collection_Found_on_Startup()
        {
            Assert.NotNull(strConnection);
            Assert.NotNull(strDatabase);

            MongoClient client = new MongoClient(strConnection);

            var db = client.GetDatabase(strDatabase);

            var lookupItems = db.GetCollection<LookupItem>("MarketplaceItemAnalytics");
            Assert.NotNull(lookupItems);

            var docLookupItems = lookupItems.Find(new BsonDocument()).ToList();

            Assert.NotNull(docLookupItems);
            Assert.True(docLookupItems.Any());
        }
        [Fact]
        public void Organization_Collection_Found_on_Startup()
        {
            Assert.NotNull(strConnection);
            Assert.NotNull(strDatabase);

            MongoClient client = new MongoClient(strConnection);

            var db = client.GetDatabase(strDatabase);

            var lookupItems = db.GetCollection<LookupItem>("Organization");
            Assert.NotNull(lookupItems);

            var docLookupItems = lookupItems.Find(new BsonDocument()).ToList();

            Assert.NotNull(docLookupItems);
            Assert.True(docLookupItems.Any());
        }
        [Fact]
        public void Permission_Collection_Found_on_Startup()
        {
            Assert.NotNull(strConnection);
            Assert.NotNull(strDatabase);

            MongoClient client = new MongoClient(strConnection);

            var db = client.GetDatabase(strDatabase);

            var lookupItems = db.GetCollection<LookupItem>("Permission");
            Assert.NotNull(lookupItems);

            var docLookupItems = lookupItems.Find(new BsonDocument()).ToList();

            Assert.NotNull(docLookupItems);
            Assert.True(docLookupItems.Any());
        }
        [Fact]
        public void ProfileItem_Collection_Found_on_Startup()
        {
            Assert.NotNull(strConnection);
            Assert.NotNull(strDatabase);

            MongoClient client = new MongoClient(strConnection);

            var db = client.GetDatabase(strDatabase);

            var lookupItems = db.GetCollection<LookupItem>("ProfileItem");
            Assert.NotNull(lookupItems);

            var docLookupItems = lookupItems.Find(new BsonDocument()).ToList();

            Assert.NotNull(docLookupItems);
            Assert.True(docLookupItems.Any());
        }
        [Fact]
        public void Publisher_Collection_Found_on_Startup()
        {
            Assert.NotNull(strConnection);
            Assert.NotNull(strDatabase);

            MongoClient client = new MongoClient(strConnection);

            var db = client.GetDatabase(strDatabase);

            var lookupItems = db.GetCollection<LookupItem>("Publisher");
            Assert.NotNull(lookupItems);

            var docLookupItems = lookupItems.Find(new BsonDocument()).ToList();

            Assert.NotNull(docLookupItems);
            Assert.True(docLookupItems.Any());
        }
        [Fact]
        public void RequestInfo_Collection_Found_on_Startup()
        {
            Assert.NotNull(strConnection);
            Assert.NotNull(strDatabase);

            MongoClient client = new MongoClient(strConnection);

            var db = client.GetDatabase(strDatabase);

            var lookupItems = db.GetCollection<LookupItem>("RequestInfo");
            Assert.NotNull(lookupItems);

            var docLookupItems = lookupItems.Find(new BsonDocument()).ToList();

            Assert.NotNull(docLookupItems);
            Assert.True(docLookupItems.Any());
        }

        [Fact]
        public void SearchKeyword_Collection_Found_on_Startup()
        {
            Assert.NotNull(strConnection);
            Assert.NotNull(strDatabase);

            MongoClient client = new MongoClient(strConnection);

            var db = client.GetDatabase(strDatabase);

            var lookupItems = db.GetCollection<LookupItem>("SearchKeyword");
            Assert.NotNull(lookupItems);

            var docLookupItems = lookupItems.Find(new BsonDocument()).ToList();

            Assert.NotNull(docLookupItems);
            Assert.True(docLookupItems.Any());
        }
    }
}