using System;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;

using CognitiveServicesDemo;
using CognitiveServicesDemo.Models;
using CognitiveServicesDemo.Utilities;

namespace CognitiveServicesDemo.Services
{
    public class CosmosDbService
    {
        public string inserted_id;    // [TEST] for Corresponding IDs

        public async Task<UserMediaCosmosJSON> SelectOneItemAsync(string databaseName, string containerName, string id)
        {
            CosmosClient cosmosClient = new(dev_Settings.cosmos_endpointUri, dev_Settings.cosmos_accountKey);
            Database database = cosmosClient.GetDatabase(databaseName);
            Container container = database.GetContainer(containerName);

            try
            {
                ItemResponse<UserMediaCosmosJSON> result = await container.ReadItemAsync<UserMediaCosmosJSON>(id, new PartitionKey(id));
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public async Task<bool> InsertOneItemAsync(string databaseName, string containerName, string json)
        {
            CosmosClient cosmosClient = new(dev_Settings.cosmos_endpointUri, dev_Settings.cosmos_accountKey);
            Database database = cosmosClient.GetDatabase(databaseName);
            Container container = database.GetContainer(containerName);

            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
            container = await database.CreateContainerIfNotExistsAsync(containerName, "/id");

            try
            {
                UserMediaCosmosJSON userMedia = JsonConvert.DeserializeObject<UserMediaCosmosJSON>(json);
                Base64stringUtility encode = new("UTF-8");
                inserted_id = userMedia.Id = (encode.Encode(userMedia.UserId + DateTime.Now.ToString("_yyyyMMddHHmmss_") + userMedia.MediaFileName)).Replace("+", "=="); 
                await container.CreateItemAsync(userMedia);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
        public async Task<bool> UpdateOneItemAsync(string databaseName, string containerName, string id, UserMediaCosmosJSON json)
        {
            CosmosClient cosmosClient = new(dev_Settings.cosmos_endpointUri, dev_Settings.cosmos_accountKey);
            Database database = cosmosClient.GetDatabase(databaseName);
            Container container = database.GetContainer(containerName);

            try
            {
                await container.ReplaceItemAsync(json,id);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public async Task<bool> DeleteItemAsync(string databaseName, string containerName, string id)
        {
            CosmosClient cosmosClient = new(dev_Settings.cosmos_endpointUri, dev_Settings.cosmos_accountKey);
            Database database = cosmosClient.GetDatabase(databaseName);
            Container container = database.GetContainer(containerName);

            try
            {
                await container.DeleteItemAsync<UserMediaCosmosJSON>(id, new PartitionKey(id));
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }


        public async Task<List<UserMediaCosmosJSON>> QueryUserMedia(string databaseName, string containerName, string queryString)
        {
            CosmosClient cosmosClient = new(dev_Settings.cosmos_endpointUri, dev_Settings.cosmos_accountKey);
            Database database = cosmosClient.GetDatabase(databaseName);
            Container container = database.GetContainer(containerName);

            var queryDef = new QueryDefinition(queryString);
            var iterator = container.GetItemQueryIterator<UserMediaCosmosJSON>(queryDef);

            var ret = new List<UserMediaCosmosJSON>();

            while (iterator.HasMoreResults)
            {
                FeedResponse<UserMediaCosmosJSON> currentResultSet = await iterator.ReadNextAsync();
                foreach (var userMedia in currentResultSet)
                {
                    ret.Add(userMedia);
                }
            }

            return ret;
        }

        public List<string> QueryUserMediaJson(string databaseName, string containerName, string queryString)
        {
            CosmosClient cosmosClient = new(dev_Settings.cosmos_endpointUri, dev_Settings.cosmos_accountKey);
            Database database = cosmosClient.GetDatabase(databaseName);
            Container container = database.GetContainer(containerName);

            // hack: このメソッドはデバッグ用に以下を参考に生JSONを取得
            // https://www.umayadia.com/cssample/sample0300/Sample303AzureCosmosDBSQLAPIQuery.htm

            var itor = container.GetItemQueryStreamIterator(queryString);

            var ret = new List<string>();

            while (itor.HasMoreResults)
            {
                var results = itor.ReadNextAsync().GetAwaiter().GetResult();

                using System.IO.StreamReader reader = new System.IO.StreamReader(results.Content);
                {
                    //結果は JSON で取得できます。このJSONにはメタ情報も含まれています。
                    string jsonText = reader.ReadToEnd();
                    ret.Add(jsonText);

                    //using (var document = System.Text.Json.JsonDocument.Parse(jsonText))
                    //{
                    //    //アイテムは Documents という名前のJSON配列に入っています。
                    //    var items = document.RootElement.GetProperty("Documents");

                    //    //アイテムのJSON配列を列挙して、アイテムの値を取り出します。
                    //    foreach (var person in items.EnumerateArray())
                    //    {
                    //        string id = person.GetProperty("id").GetString();
                    //        string Name = person.GetProperty("Name").GetString();
                    //        int Age = person.GetProperty("Age").GetInt32();

                    //        System.Diagnostics.Debug.WriteLine($"{id} {Name} {Age}");
                    //    } //foreach 
                    //} //using document

                }
            }

            return ret;
        }
    }
}
