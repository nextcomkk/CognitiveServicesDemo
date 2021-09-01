using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Web.Http;
using System.Collections.Generic;

using Newtonsoft.Json;
using Microsoft.AspNetCore.Identity;

using CognitiveServicesDemo;
using CognitiveServicesDemo.Data;
using CognitiveServicesDemo.Models;
using CognitiveServicesDemo.Services;
using CognitiveServicesDemo.Utilities;
using CognitiveServicesDemo.Controllers;
using System.Web;

namespace UserMediaAPIController.Controllers
{
    public class UserMediaAPIController : UserMediaController
    {
        ApplicationDbContext _context;
        StorageBlobService blobService;

        Base64stringUtility encode = new("UTF-8");

        public UserMediaAPIController(UserManager<ApplicationUser> userManager, ApplicationDbContext context) : base(userManager, context)
        {
            _context = context;
            blobService = new StorageBlobService();
        }

        public HttpStatusCode Import([FromUri] string userId, [FromUri] string imagePath, [FromUri] string[] imageFiles)
        {
            string loggedInUserId = userId;
            List<UserMedia> userMedia = (from a in _context.UserMedia where a.UserId == loggedInUserId orderby a.MediaId descending select a).ToList();

            int success_count = 0;
            int fail_count = 0;

            foreach (var imageFile in imageFiles)
            {
                bool skip = false;
                var importUrl = imagePath + "/" + imageFile;
                foreach (var imported in userMedia)
                {
                    if (importUrl == imported.MediaUrl)
                    {
                        skip = true;
                        break;
                    }
                }
                if (!skip) { 
                    if (ImportMediaFile(userId, importUrl))
                    {
                        success_count++;
                    }
                    else {
                        fail_count++;
                    }
                }
            }

            Console.WriteLine($"Success: {success_count.ToString()}");
            Console.WriteLine($"Fail: {fail_count.ToString()}");

            return HttpStatusCode.OK;
        }

        public bool ImportMediaFile(string userId, string url)
        {
            Console.WriteLine($"Start Import:{url}");

            string fileName = Path.GetFileName(url);
            string tagsString, tagsJson;
            string[] ids = new string[3];

            string loggedInUserId = userId;
            UserMedia userMedia = new();

            //////////////////// Get tags by Computer Vision API ////////////////////
            try
            {
                ComputerVisionService cv = new();
                // 2 patterns in the prototype
                tagsString = cv.GetImageTag_str(url);          // Get as parsable string -> 1) SQL and 3) Table
                tagsJson = cv.GetImageTag_json(url);           // Get as json            -> 2) Cosmos and 4) Blob 

                if (String.IsNullOrEmpty(tagsString) || String.IsNullOrEmpty(tagsJson))
                {
                    // Computer Vision Error
                    Console.WriteLine($"Error from Computer Vision API '{loggedInUserId},{url}'");
                    return false;
                }
            }
            catch (Exception e)
            {
                // Computer Vision Error
                Console.WriteLine($"Error from Computer Vision API '{loggedInUserId},{url}':{e.Message}");
                return false;
            }

            //////////////////// Create tags data ////////////////////
            try
            {
                // Create Model data
                userMedia.UserId = loggedInUserId;                                      // User ID
                userMedia.MediaUrl = url;                                               // URL of the blob
                userMedia.MediaFileName = fileName;                                     // File name
                userMedia.MediaFileType = fileName.Split('.').Last();                   // File type
                userMedia.Tags = tagsString;                                            // Tags (Parsable string)

                // Add image info to json (same as model)
                tagsJson = ImageAnalysisUtility.AddImageInfoToTag_json(tagsJson, userMedia);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to parse '{loggedInUserId},{userMedia.MediaFileName}':{e.Message}");
                return false;
            }

            //////////////////// Save image info and tags data ////////////////////

            // ---------- 1) [SQL database] (as SQL schema Record) ---------- 
            try
            {
                _context.UserMedia.Add(userMedia);
                _context.SaveChanges();
                Console.WriteLine($"1) Success");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to save to SQL database. UserID:'{loggedInUserId}', MediaFileName:'{userMedia.MediaFileName}', ErrorMessage:'{e.Message}'");
                return false;
            }

            // ----------  2) [Azure Cosmos DB] (as json Item) ---------- 
            try
            {
                CosmosDbService cosmosDbService = new();
                if (!cosmosDbService.InsertOneItemAsync(dev_Settings.cosmos_databaseName, dev_Settings.cosmos_containerName, tagsJson).Result)
                {
                    Console.WriteLine($"2) Failure");
                }
                else {
                    Console.WriteLine($"2) Success");
                }

                ids[0] = cosmosDbService.inserted_id;  // [TEST] for Corresponding IDs
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to save to Azure Cosmos DB. UserID:'{loggedInUserId}', MediaFileName:'{userMedia.MediaFileName}', ErrorMessage:'{e.Message}'");
                return false;
            }

            // ---------- 3) [Azure Storage Table] (as NoSQL Entity) ----------
            try
            {
                UserMediaStorageTableEntity userMediaStorageTableEntity = new();
                userMediaStorageTableEntity.UserId = userMedia.UserId;
                userMediaStorageTableEntity.MediaUrl = userMedia.MediaUrl;
                userMediaStorageTableEntity.MediaFileName = userMedia.MediaFileName;
                userMediaStorageTableEntity.MediaFileType = userMedia.MediaFileType;
                userMediaStorageTableEntity.Tags = userMedia.Tags;
                userMediaStorageTableEntity.DateTimeUploaded = userMedia.DateTimeUploaded;

                StorageTableService tableService = new();
                if (!tableService.InsertOrMerageEntityAsync(userMediaStorageTableEntity).Result)
                {
                    Console.WriteLine($"3) Failure");
                }
                else
                {
                    Console.WriteLine($"3) Success");
                }

                ids[1] = tableService.inserted_id;      // [TEST] for Corresponding IDs
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to save to Azure Storage Table. UserID:'{loggedInUserId}', MediaFileName:'{userMedia.MediaFileName}', ErrorMessage:'{e.Message}'");
                return false;
            }

            // ---------- 4) [Azure Storage Blob] (as json file) ----------
            try
            {
                string datetimeStr = DateTime.Now.ToString("yyyyMMddHHmmss_");

                UserMediaBlobJSON userMediaBlobJSON = JsonConvert.DeserializeObject<UserMediaBlobJSON>(tagsJson);
                userMediaBlobJSON.Key = (encode.Encode(userMedia.UserId + datetimeStr + userMedia.MediaFileName)).Replace("+", "==");
                tagsJson = JsonConvert.SerializeObject(userMediaBlobJSON, Formatting.None);
                fileName = "UserMedia/" + userMedia.UserId + "/" + datetimeStr + userMedia.MediaFileName + ".json";

                if (!blobService.StoreJsonBlobAsync(fileName, dev_Settings.blob_containerName_json, tagsJson).Result)
                {
                    Console.WriteLine($"4) Failure");
                }
                else
                {
                    Console.WriteLine($"4) Success");
                }

                ids[2] = fileName;      // [TEST] for Corresponding IDs
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to save to Azure Storage Blob. UserID:'{loggedInUserId}', MediaFileName:'{userMedia.MediaFileName}', ErrorMessage:'{e.Message}'");
                return false;
            }

            // ---------- Save corresponding IDs (use for edit and delete) ----------
            try
            {
                MediaId mediaId = new();
                mediaId.Sql_id = userMedia.MediaId;
                mediaId.Cosmos_db = ids[0];
                mediaId.Storage_table = ids[1];
                mediaId.Storage_blob = ids[2];

                _context.MediaId.Add(mediaId);
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to save to MediaId (SQL database). UserID:'{loggedInUserId}', MediaFileName:'{userMedia.MediaFileName}', ErrorMessage:'{e.Message}'");
                return false;
            }
            return true;
        }
    }
}
