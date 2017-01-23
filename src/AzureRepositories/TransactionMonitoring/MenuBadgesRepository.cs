using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories.Monitoring;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.TransactionMonitoring
{
    public class MenuBadgeEntity : TableEntity, IMenuBadge
    {
        public static string GeneratePartitionKey()
        {
            return "Badge";
        }

        public static string GenerateRowKey(string badgeId)
        {
            return badgeId;
        }


        public string Id => RowKey;
        public string Value { get; set; }

        public static MenuBadgeEntity Create(string id, string value)
        {
            return new MenuBadgeEntity
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(id),
                Value = value
            };
        }
    }


    public class MenuBadgesRepository : IMenuBadgesRepository
    {
        private readonly INoSQLTableStorage<MenuBadgeEntity> _tableStorage;

        public MenuBadgesRepository(INoSQLTableStorage<MenuBadgeEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task SaveBadgeAsync(string id, string value)
        {
            var entity = MenuBadgeEntity.Create(id, value);
            return _tableStorage.InsertOrReplaceAsync(entity);
        }

        public Task RemoveBadgeAsync(string id)
        {
            var partitionKey = MenuBadgeEntity.GeneratePartitionKey();
            var rowKey = MenuBadgeEntity.GenerateRowKey(id);
            return _tableStorage.DeleteAsync(partitionKey, rowKey);
        }

        public async Task<IEnumerable<IMenuBadge>> GetBadesAsync()
        {
            var partitionKey = MenuBadgeEntity.GeneratePartitionKey();
            return await _tableStorage.GetDataAsync(partitionKey);
        }
    }
}
