using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;
using System;

namespace DumpAppLog
{
    internal class Program
    {
        private static string? strConnection = "mongodb://testuser:password@localhost:27017";

        private static string? strDatabase = "test";

        static void Main(string[] args)
        {
            strConnection = Environment.GetEnvironmentVariable("MARKETPLACE_MONGODB_CONNECTIONSTRING");
            strDatabase = Environment.GetEnvironmentVariable("MARKETPLACE_MONGODB_DATABASE");

            Console.WriteLine("Dump App Log");

            MongoClient client = new MongoClient(strConnection);

            var db = client.GetDatabase(strDatabase);

            var lookupItems = db.GetCollection<AppLogItem>("app_log");

            var docAppLogItems = lookupItems.Find(new BsonDocument()).ToList();

            for (int i = 0; i < docAppLogItems.Count; i++)
            {
                DateTime d = docAppLogItems[i].Date;
                string strLevel = docAppLogItems[i].Level;
                string strMessage = docAppLogItems[i].Message;
                string strLogger = docAppLogItems[i].Logger;
                string strDate = $"{d.Year:0000}-{d.Month:00}-{d.Day:00} {d.Hour:00}:{d.Minute:00}:{d.Second:00}";

                Console.WriteLine($"{strDate}\t{strLevel}\t{strLogger}\t{strMessage}");
            }
        }
    }
}