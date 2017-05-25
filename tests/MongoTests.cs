using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Settings;
using MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoRepositories.Mongo;

namespace Bitcoin.Tests
{
    [TestClass]
    public class MongoTests
    {
        [TestMethod]
        public async Task TestGuid()
        {
            var settings = Config.Services.GetService<BaseSettings>();
            var mongoClient = new MongoClient(settings.Db.MongoDataConnString);

            var storage = new MongoStorage<Order>(mongoClient, "orders", "tests");

            var order = (await storage.GetDataAsync(o => o.OrderId == Guid.Parse("9abe5f31-8171-4196-acf6-37086f71a8df"))).LastOrDefault();

            await storage.ReplaceAsync(order.BsonId, order1 => order1);
            //9abe5f31-8171-4196-acf6-37086f71a8df
        }


        private class Order : MongoEntity
        {
            [BsonRepresentation(BsonType.String)]
            public Guid OrderId { get; set; }
        }
    }
}
