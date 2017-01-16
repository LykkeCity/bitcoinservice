using System.Text;
using AzureStorage.Blob;
using Common;

namespace AzureRepositories
{

    public static class GeneralSettingsReader
    {
        public static T ReadGeneralSettings<T>(string connectionString, string fileName)
        {
            var settingsStorage = new AzureBlobStorage(connectionString);
            var settingsData = settingsStorage.GetAsync("settings", fileName).Result.ToBytes();
            var str = Encoding.UTF8.GetString(settingsData);

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(str);
        }
    }

}
