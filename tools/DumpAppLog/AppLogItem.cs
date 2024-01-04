using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpAppLog
{
    [BsonIgnoreExtraElements]

    public class AppLogItem : AbstractEntity
    {
        public DateTime Date { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Logger { get; set; }
        public int ThreadID{ get; set; }
        public string ThreadName { get; set; }
        public int ProcessID { get; set; }
        public string ProcessName { get; set; }
        public object Properties { get; set; }
    }

    public abstract class AbstractEntity
    {
        //TBD - revisit making ID a string. 
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string ID { get; set; }

    }
}
