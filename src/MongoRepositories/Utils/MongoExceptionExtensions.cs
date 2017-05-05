using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoRepositories.Utils
{
	public static class MongoExceptionExtensions
	{
		public static bool IsDuplicateError(this MongoWriteException ex)
		{
			return ex.Message.Contains("duplicate key error");
		}
	}
}
