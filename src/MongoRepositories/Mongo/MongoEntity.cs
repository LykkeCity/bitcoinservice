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

		public int BsonVersion { get; set; }
	}
}
