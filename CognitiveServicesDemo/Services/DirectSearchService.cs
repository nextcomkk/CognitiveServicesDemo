using CognitiveServicesDemo.Data;
using CognitiveServicesDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CognitiveServicesDemo.Services
{
    public class DirectSearchService
    {
        ApplicationDbContext dbContext;

        public DirectSearchService(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }


        private string GetTags(IEnumerable<string> tags)
        {
            string buff = "";

            foreach (var tag in tags.Select((value, index) => new { value, index }))
            {
                if (tag.value.Equals("|") || tag.value.Equals("")) break;
                var tag_array = tag.value.Split(":");

                if (tag.index == 0 || (Double.Parse(tag_array[1]) >= dev_Settings.tag_confidence_threshold))
                    buff += tag_array[0] + "%2B";
            }
            int del_index = buff.LastIndexOf("%2B");
            if (del_index > 0) buff = buff.Remove(del_index);

            return buff;
        }


        public List<SearchResultUserMedia> Search_SQL(string searchKeywords, string userId)
        {
            var tags = searchKeywords.Split("+");

            // hack: tagsでlike検索というわけには行かないので、RDBを使う場合はTagテーブルを別途用意した方が速いと思われる。
            var query = dbContext.UserMedia.Where(x => x.UserId == userId).ToArray();
            var result = query.Where(x =>
            {
                var tagNames = x.Tags.Split("|").Select(a => a.Split(":")[0]);
                foreach(var tag in tags)
                {
                    if (!tagNames.Any(a => a == tag)) return false;
                }
                return true;
            });

            return result.Select(x =>
            {
                SearchResultUserMedia document = new();
                document.Id = x.MediaId.ToString();
                document.UserId = x.UserId;
                document.MediaFileName = x.MediaFileName;
                document.MediaFileType = x.MediaFileType;
                document.MediaUrl = x.MediaUrl;
                document.DateTimeUploaded = x.DateTimeUploaded;
                document.Tags = GetTags(x.Tags.Split("|"));

                document.SearchScore = "";

                return document;

            }).ToList();
        }


        public List<SearchResultUserMedia> Search_Cosmos(string searchKeywords, string userId)
        {
            var tags = searchKeywords.Split("+");

            // hack: 項目名は全て小文字で登録されている
            var tagCriteria = tags.Aggregate("", (sum, x) => $"{sum}{(string.IsNullOrEmpty(sum) ? "" : " AND ")}{$"ARRAY_CONTAINS(c.tags, {{ \"name\": \"{x}\" }}, true)"}");
            var query = $"SELECT * FROM c WHERE c.userid = '{userId}' {(string.IsNullOrEmpty(tagCriteria) ? "" : " AND ")}{tagCriteria}";
            Console.WriteLine($"Search_Cosmos query= {query}");

            var dbService = new CosmosDbService();

            var task = dbService.QueryUserMedia(dev_Settings.cosmos_databaseName, dev_Settings.cosmos_containerName, query);
            var result = task.Result;

            return result.Select(x =>
            {
                SearchResultUserMedia document = new();
                document.Id = x.Id;
                document.UserId = x.UserId;
                document.MediaFileName = x.MediaFileName;
                document.MediaFileType = x.MediaFileType;
                document.MediaUrl = x.MediaUrl;
                document.DateTimeUploaded = x.DateTimeUploaded;
                document.Tags = x.Tags.Aggregate("", (sum, x) => $"{sum}{(string.IsNullOrEmpty(sum) ? "" : "%2B")}{x.Name}");

                document.SearchScore = "";

                return document;

            }).ToList();
        }
    }
}
