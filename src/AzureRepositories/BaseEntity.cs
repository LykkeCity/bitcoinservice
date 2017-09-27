using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories
{
    public class BaseEntity : TableEntity
    {
        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);

            foreach (var p in GetType().GetProperties().Where(x =>
                (x.PropertyType == typeof(decimal) || x.PropertyType == typeof(decimal?)) && properties.ContainsKey(x.Name)))
            {
                var value = properties[p.Name].StringValue;
                p.SetValue(this, value != null ? Convert.ToDecimal(value, CultureInfo.InvariantCulture) : (decimal?)null);
            }
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var properties = base.WriteEntity(operationContext);

            foreach (var p in GetType().GetProperties().Where(x => x.PropertyType == typeof(decimal) || x.PropertyType == typeof(decimal?)))
                properties.Add(p.Name, new EntityProperty(p.GetValue(this)?.ToString()));

            return properties;
        }
    }
}
