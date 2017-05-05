using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoRepositories.Mongo
{	
	public abstract class MongoEntity
	{
		[BsonId]
		public string BsonId { get; set; }

        [BsonElement("_version")]
		public int BsonVersion { get; set; }

        [BsonElement("_created")]
        public DateTime BsonCreateDt { get; set; }

	    [BsonElement("_updated")]
        public DateTime? BsonUpdateDt { get; set; }

	    protected MongoEntity()
	    {
	        BsonCreateDt = DateTime.UtcNow;
	    }
	}
}
