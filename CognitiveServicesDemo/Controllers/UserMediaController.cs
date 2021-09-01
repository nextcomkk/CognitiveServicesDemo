using System;
using System.IO;
using System.Linq;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using CognitiveServicesDemo;
using CognitiveServicesDemo.Data;
using CognitiveServicesDemo.Models;
using CognitiveServicesDemo.Services;
using CognitiveServicesDemo.Utilities;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Web;

namespace CognitiveServicesDemo.Controllers
{
    public class UserMediaController : Controller
    {
        [TempData]
        public string ErrorMessage { get; set; }

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        StorageBlobService blobService;
        Base64stringUtility encode = new("UTF-8");

        public UserMediaController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
            blobService = new StorageBlobService();
        }

        [Authorize]
        public ActionResult DeleteMediaFile_(int id, string controller)
        {
            UserMedia userMedia = _context.UserMedia.Find(id);
            MediaId mediaId = _context.MediaId.Find(id);

            try
            {
                ////////////////////// 1) SQL database ////////////////////
                _context.UserMedia.Remove(userMedia);
                _context.SaveChanges();

                ////////////////////// 2) Azure Cosmos DB ////////////////////
                CosmosDbService cosmosDbService = new();
                _ = cosmosDbService.DeleteItemAsync(dev_Settings.cosmos_databaseName, dev_Settings.cosmos_containerName, mediaId.Cosmos_db);

                ////////////////////// 3) Azure Storage Table ////////////////////
                string[] keys = mediaId.Storage_table.Split('|');
                StorageTableService tableService = new();
                _ = tableService.DeleteItemAsync(keys[0], keys[1]);

                ////////////////////// 4) Azure Storage Blob ////////////////////
                blobService.DeleteBlob(mediaId.Storage_blob, dev_Settings.blob_containerName_json);

                ////////////////////// [TEST] Corresponding Id (MediaId) ////////////////////
                _context.MediaId.Remove(mediaId);
                _context.SaveChanges();

                // Delete image file in Blob
                string blobImageToDelete = userMedia.MediaUrl.Split('/').Last();
                blobService.DeleteBlob(blobImageToDelete, dev_Settings.blob_containerName_image);

                return RedirectToAction("Index", controller);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to delete '{_userManager.GetUserId(User)},{userMedia.MediaFileName}':{e.Message}");
                ErrorMessage = "イメージファイルまたはイメージ情報の削除に失敗しました";
                return RedirectToAction("Index", controller);
            }
        }

        [Authorize]
        public ActionResult UploadMediaFile(List<IFormFile> files, string controller)
        {
            if (files != null)
            {
                foreach (IFormFile file in files) { 

                    // Upload image file to Azure Blob
                    string ContainerName = dev_Settings.blob_containerName_image;
                    string fileName = Path.GetFileName(file.FileName);

                    // If same name file exist, change file name.
                    blobService.CreateContainerIfNotExistsAsync(ContainerName);
                    var existFiles = blobService.GetBlobFileList(ContainerName);
                    if (existFiles.Result != null) {
                        foreach (var existfile in existFiles.Result)
                        {
                            if (fileName.Equals(existfile.ToString()))
                            {
                                fileName = DateTime.Now.ToString("yyyyMMddHHmmss_") + fileName;
                                break;
                            }
                        }
                    }

                    // Upload file (no Validation)
                    Stream imageStream = file.OpenReadStream();
                    var result = blobService.UploadImageBlobAsync(fileName, ContainerName, (IFormFile)file);
                    string tagsString, tagsJson;
                    string[] ids = new string[3];

                    if (result != null)
                    {
                        string loggedInUserId = _userManager.GetUserId(User);
                        UserMedia userMedia = new();

                        //////////////////// Get tags by Computer Vision API ////////////////////
                        try
                        {
                            ComputerVisionService cv = new();
                            // 2 patterns in the prototype
                            tagsString = cv.GetImageTag_str(result.Result.Uri.ToString());          // Get as parsable string -> 1) SQL and 3) Table
                            tagsJson = cv.GetImageTag_json(result.Result.Uri.ToString());           // Get as json            -> 2) Cosmos and 4) Blob 

                            if (String.IsNullOrEmpty(tagsString) || String.IsNullOrEmpty(tagsJson))
                            {
                                // Computer Vision Error
                                ErrorMessage = "Computer Vision による解析ができませんでした。ファイルは登録されません";
                                return RedirectToAction("Index", controller);
                            }
                        }
                        catch (Exception e)
                        {
                            // Computer Vision Error
                            Console.WriteLine($"Error from Computer Vision API '{loggedInUserId},{result.Result.Uri}':{e.Message}");
                            ErrorMessage = "Computer Vision による解析ができませんでした。ファイルは登録されません";
                            return RedirectToAction("Index", controller);
                        }

                        //////////////////// Create tags data ////////////////////
                        try
                        {
                            // Create Model data
                            userMedia.UserId = loggedInUserId;                                      // User ID
                            userMedia.MediaUrl = result.Result.Uri.ToString();                      // URL of the blob
                            userMedia.MediaFileName = result.Result.Name;                           // File name
                            userMedia.MediaFileType = result.Result.Name.Split('.').Last();         // File type
                            userMedia.Tags = tagsString;                                            // Tags (Parsable string)

                            // Add image info to json (same as model)
                            tagsJson = ImageAnalysisUtility.AddImageInfoToTag_json(tagsJson, userMedia);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Unable to parse '{loggedInUserId},{userMedia.MediaFileName}':{e.Message}");
                            ErrorMessage = "タグ情報を整形できませんでした。ファイルは登録されません";
                            return RedirectToAction("Index", controller);
                        }

                        //////////////////// Save image info and tags data ////////////////////

                        // ---------- 1) [SQL database] (as SQL schema Record) ---------- 
                        try
                        {
                            _context.UserMedia.Add(userMedia);
                            _context.SaveChanges();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Unable to save to SQL database. UserID:'{loggedInUserId}', MediaFileName:'{userMedia.MediaFileName}', ErrorMessage:'{e.Message}'");
                            ErrorMessage = "SQL database への登録に失敗しました";
                            return RedirectToAction("Index", controller);
                        }

                        // ----------  2) [Azure Cosmos DB] (as json Item) ---------- 
                        try
                        {
                            CosmosDbService cosmosDbService = new();
                            if (!cosmosDbService.InsertOneItemAsync(dev_Settings.cosmos_databaseName, dev_Settings.cosmos_containerName, tagsJson).Result)
                                ErrorMessage = "Cosmos DB への登録に失敗しました";

                            ids[0] = cosmosDbService.inserted_id;  // [TEST] for Corresponding IDs
                        }
                        catch (Exception e)
                        { 
                            Console.WriteLine($"Unable to save to Azure Cosmos DB. UserID:'{loggedInUserId}', MediaFileName:'{userMedia.MediaFileName}', ErrorMessage:'{e.Message}'");
                            ErrorMessage = "Cosmos DB への登録に失敗しました";
                            return RedirectToAction("Index", controller);
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
                                ErrorMessage = "Storage Table への登録に失敗しました";

                            ids[1] = tableService.inserted_id;      // [TEST] for Corresponding IDs
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Unable to save to Azure Storage Table. UserID:'{loggedInUserId}', MediaFileName:'{userMedia.MediaFileName}', ErrorMessage:'{e.Message}'");
                            ErrorMessage = "Storage Table への登録に失敗しました";
                            return RedirectToAction("Index", controller);
                        }

                        // ---------- 4) [Azure Storage Blob] (as json file) ----------
                        try
                        {
                            string datetimeStr = DateTime.Now.ToString("yyyyMMddHHmmss_");

                            UserMediaBlobJSON userMediaBlobJSON = JsonConvert.DeserializeObject<UserMediaBlobJSON>(tagsJson);
                            userMediaBlobJSON.Key = (encode.Encode(userMedia.UserId + datetimeStr + userMedia.MediaFileName)).Replace("+","=");
                            tagsJson = JsonConvert.SerializeObject(userMediaBlobJSON, Formatting.None);
                            fileName = "UserMedia/" + userMedia.UserId + "/" + datetimeStr + userMedia.MediaFileName + ".json";

                            if (!blobService.StoreJsonBlobAsync(fileName, dev_Settings.blob_containerName_json, tagsJson).Result)
                                ErrorMessage = "Storage Blob への登録に失敗しました";

                            ids[2] = fileName;      // [TEST] for Corresponding IDs
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Unable to save to Azure Storage Blob. UserID:'{loggedInUserId}', MediaFileName:'{userMedia.MediaFileName}', ErrorMessage:'{e.Message}'");
                            ErrorMessage = "Storage Blob への登録に失敗しました";
                            return RedirectToAction("Index", controller);
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
                            ErrorMessage = "ID紐づけ情報(SQL database)への登録に失敗しました";
                            return RedirectToAction("Index", controller);
                        }
                    }       
                    else
                    {
                        return RedirectToAction("Index", controller);
                    }
                }
            }
            else
            {
                return RedirectToAction("Index", controller);
            }
            return RedirectToAction("Index", controller);
        }
    }
}
