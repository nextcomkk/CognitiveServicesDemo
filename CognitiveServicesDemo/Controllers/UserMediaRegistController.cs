using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Collections.Generic;

using Newtonsoft.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

using CognitiveServicesDemo;
using CognitiveServicesDemo.Data;
using CognitiveServicesDemo.Models;
using CognitiveServicesDemo.Services;
using CognitiveServicesDemo.Utilities;

namespace CognitiveServicesDemo.Controllers
{
    public class UserMediaRegistController : UserMediaController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string sessionKey_pageNumber = "pageNumber";
        private const int pageSize = dev_Settings.pageSize_regist;

        public UserMediaRegistController(UserManager<ApplicationUser> userManager, ApplicationDbContext context): base(userManager, context)
        {
            _userManager = userManager;
            _context = context;
        }

        [Authorize]
        public IActionResult Index(int? pageNumber)
        {
            if (pageNumber is null)
            {
                HttpContext.Session.SetString(sessionKey_pageNumber, 1.ToString());
            }
            else {
                HttpContext.Session.SetString(sessionKey_pageNumber, pageNumber.ToString());
            }

            string loggedInUserId = _userManager.GetUserId(User);
            var userMedias = (from a in _context.UserMedia where a.UserId == loggedInUserId orderby a.MediaId descending select a);     // Linking images to users
            //var userMedias = (from a in _context.UserMedia orderby a.MediaId descending select a);                                    // Share images with all users
            ViewBag.ItemCount = userMedias.Count();

            return View("~/Views/UserMedia/Regist/Index.cshtml", PagedListData(userMedias, pageNumber).Result);
        }

        [Authorize]
        public async Task<PaginatedList<UserMedia>> PagedListData(IOrderedQueryable<UserMedia> userMedias, int? pageNumber)
        {
            return await PaginatedList<UserMedia>.CreateAsync(userMedias, pageNumber ?? 1, pageSize);
        }

        [Authorize]
        [HttpPost]
        public IActionResult UploadMediaFile(List<IFormFile> upfiles)
        {
            return UploadMediaFile(upfiles, "UserMediaRegist");
        }

    [HttpGet]
        public PartialViewResult EditTags(string id)
        {
            try
            {
                UserMedia userMedia = _context.UserMedia
                                            .Where(a => a.UserId == _userManager.GetUserId(User))
                                            .Where(a => a.MediaId == Int32.Parse(id)).Single();

                EditTags mTags = new();
                mTags.tags = new List<EditTag>();

                mTags.id = id;
                string[] tags = userMedia.Tags.Split("|");

                foreach (var tag in tags)
                {
                    if (tag.Equals("|") || String.IsNullOrEmpty(tag)) break;

                    EditTag mTag = new();
                    var tag_array = tag.Split(":");

                    // User can edit only manual tags
                    if (tag_array[2] == "1") {
                        mTag.Name = tag_array[0];
                        mTag.Confidence = Double.Parse(tag_array[1]);
                        mTag.Type = Int32.Parse(tag_array[2]);
                        mTags.tags.Add(mTag);
                    }
                }

                return PartialView("~/Views/UserMedia/Regist/_EditTags.cshtml", mTags);

            }
            catch (Exception e) {
                Console.WriteLine($"Error: {e.Message}");
                return PartialView("Error");
            }
        }

        [HttpPost]
        public ActionResult EditTags(string id, string[] mTags_Name)
        {
            // Original Validation
            foreach (var item in mTags_Name)
            {
                if ((!String.IsNullOrEmpty(item)) && (item.IndexOf(":") >= 0 || item.IndexOf("|") >= 0))
                {
                    ErrorMessage = "独自タグには、「:」と「|」 の文字は使えません";
                    return RedirectToAction("Index", "UserMediaRegist");
                }
            }

            string loggedInUserId = _userManager.GetUserId(User);
            string autoTagsStr = "", manualTagsStr = "";
            List<ImageTagJSON> autoTagsJSON = new(); List<ImageTagJSON> manualTagsJSON = new();

            // Skip duplicates of tag
            foreach (var item in mTags_Name.Select((value, index) => new { value, index }))
            {
                if (!String.IsNullOrEmpty(item.value))
                {
                    for (int i = 0; i <= item.index; i++)
                    {
                        if (item.index == i) {
                            // Parsable string -> 1) SQL and 3) Storage Table
                            manualTagsStr += item.value + ":1.0:1|";

                            // JSON -> 2) Cosmos DB and 4) Storage Blob
                            ImageTagJSON tag = new();
                            tag.Name = item.value;
                            tag.Confidence = 1.0;
                            tag.Type = 1;
                            manualTagsJSON.Add(tag);
                        }
                        if (item.value.Equals(mTags_Name[i])) break;
                    }
                }
            }

            // [TEST] Get corresponding IDs
            MediaId mediaId = _context.MediaId.Where(a => a.Sql_id == Int32.Parse(id)).Single();
            if (mediaId == null) { 
                ErrorMessage = "ID紐づけデータ(SQL database)の取得に失敗しました。タグの更新ができませんでした";
                return RedirectToAction("Index", "UserMediaRegist");
            }

            // ---------- 1) [SQL database] (as SQL schema Record) ----------
            try
            {
                // Create update data
                UserMedia userMedia = _context.UserMedia
                                            .Where(a => a.UserId == loggedInUserId)
                                            .Where(a => a.MediaId == Int32.Parse(id)).Single();

                string[] tags = userMedia.Tags.Split("|");

                foreach (var tag in tags)
                {
                    if (tag.Equals("|") || String.IsNullOrEmpty(tag)) break;
                    var tag_array = tag.Split(":");

                    // Select only auto tags
                    if (tag_array[2] == "0") autoTagsStr += tag_array[0] + ":" + tag_array[1] + ":" + tag_array[2] + "|";
                }

                // Execute update
                userMedia.Tags = autoTagsStr + manualTagsStr;
                _context.UserMedia.Update(userMedia);
                _context.SaveChanges();
            }
            catch(Exception e)
            {
                Console.WriteLine($"Unable to update to SQL database. UserID:'{loggedInUserId}', MediaID:'{id}', errorMessage:'{e.Message}'");
                ErrorMessage = "SQL database の更新に失敗しました。タグの更新ができませんでした";
                return RedirectToAction("Index", "UserMediaRegist");
            }

            // ---------- 2) [Azure Cosmos DB] (as json Item) ----------
            try
            {
                // Create update data
                CosmosDbService cosmosDbService = new();
                var item = cosmosDbService.SelectOneItemAsync(dev_Settings.cosmos_databaseName, dev_Settings.cosmos_containerName, mediaId.Cosmos_db);

                UserMediaCosmosJSON userMediaCosmoJSON = item.Result;
                foreach (var tag in userMediaCosmoJSON.Tags) if (tag.Type == 0) autoTagsJSON.Add(tag);

                userMediaCosmoJSON.Tags = autoTagsJSON;
                userMediaCosmoJSON.Tags.AddRange(manualTagsJSON);

                // Execute update
                if (!cosmosDbService.UpdateOneItemAsync(dev_Settings.cosmos_databaseName, dev_Settings.cosmos_containerName, mediaId.Cosmos_db, userMediaCosmoJSON).Result)
                    ErrorMessage = "Cosmos DB の更新に失敗しました。タグの更新ができませんでした";
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to update to Azure Cosmos DB. UserID:'{loggedInUserId}', MediaID:'{id}', errorMessage:'{e.Message}'");
                ErrorMessage = "Cosmos DB の更新に失敗しました。タグの更新ができませんでした";
                return RedirectToAction("Index", "UserMediaRegist");
            }

            // 3) ---------- [Azure Storage Table] (as NoSQL Entity) ----------
            try
            {
                // Create update data
                string[] keys = mediaId.Storage_table.Split("|");

                StorageTableService tableService = new();
                UserMediaStorageTableEntity userMediaStorageTableEntity = tableService.SelectEntityAsync(keys[0], keys[1]).Result;

                // Note: UserMedia (SQL database) is used.
                userMediaStorageTableEntity.Tags = autoTagsStr + manualTagsStr;

                // Execute update
                if (!tableService.ReplaceEntityAsync(userMediaStorageTableEntity).Result)
                    ErrorMessage = "Storage Table の更新に失敗しました。タグの更新ができませんでした";
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to update to Azure Storage Table. UserID:'{loggedInUserId}', MediaID:'{id}', errorMessage:'{e.Message}'");
                ErrorMessage = "Storage Table の更新に失敗しました。タグの更新ができませんでした";
                return RedirectToAction("Index", "UserMediaRegist");
            }

            // 4) ---------- [Azure Storage Blob] (as json file) ----------
            try
            {
                // Create update data
                StorageBlobService blobService = new();
                var oldJSON = blobService.GetBlobFileString(mediaId.Storage_blob, dev_Settings.blob_containerName_json);
                UserMediaBlobJSON userMediaJSON = JsonConvert.DeserializeObject<UserMediaBlobJSON>(oldJSON.Result);

                userMediaJSON.Tags.Clear();
                userMediaJSON.Tags = autoTagsJSON;
                //userMediaJSON.Tags.AddRange(manualTagsJSON);   [NOTE] Duplicating manualTagsJSON

                // Execute update
                string newJSON = JsonConvert.SerializeObject(userMediaJSON, Formatting.None);

                if (!blobService.StoreJsonBlobAsync(mediaId.Storage_blob, dev_Settings.blob_containerName_json, newJSON).Result)
                    ErrorMessage = "Storage Blob の更新に失敗しました。タグの更新ができませんでした";
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to update to Azure Storage Blob. UserID:'{loggedInUserId}', MediaID:'{id}', errorMessage:'{e.Message}'");
                ErrorMessage = "Storage Blob の更新に失敗しました。タグの更新ができませんでした";
                return RedirectToAction("Index", "UserMediaRegist");
            }

            var pageNumber = HttpContext.Session.GetString(sessionKey_pageNumber);
            if (pageNumber is null)
            {
                return RedirectToAction("Index", "UserMediaRegist", "_" + id);
            }
            else {
                return RedirectToAction("Index", "UserMediaRegist", new { pageNumber = pageNumber.ToString() }, "_" + id);
            }
        }
    }
}
