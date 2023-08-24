namespace CESMII.Marketplace.MongoDB
{
    using CESMII.Marketplace.Data.Entities;
    using global::MongoDB.Bson;
    using global::MongoDB.Driver;
    using MongoDB;

    public class TestSuite_MongoDB
    {
        // Need to have a connection string and a database name.
        string strConnection = "mongodb://testuser:password@localhost:27017";
        string strDatabase = "test";

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
        public void ValidLookupCollection_on_Startup()
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
    }
}