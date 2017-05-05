using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoRepositories.Utils
{
	public static class BsonDocumentExtensions
	{
		public static BsonDocument MergeExt(this BsonDocument doc, BsonDocument doc2)
		{
			foreach (var bsonElement in doc2.Elements)
			{
				if (bsonElement.Value != BsonNull.Value)
					doc[bsonElement.Name] = bsonElement.Value;
			}
			return doc;
		}
	}
}
