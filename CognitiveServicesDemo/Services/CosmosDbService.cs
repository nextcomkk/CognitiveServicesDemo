﻿using System;
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
    }
}
