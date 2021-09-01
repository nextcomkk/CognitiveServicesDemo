using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using CognitiveServicesDemo;
using CognitiveServicesDemo.Data;
using CognitiveServicesDemo.Models;
using CognitiveServicesDemo.Services;

namespace CognitiveServicesDemo.Controllers
{
    public class UserMediaSearchController : UserMediaController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private const string sessionKey_searchMode = "search-mode";
        private const string sessionKey_pageNumber = "pageNumber";
        private const int displayMaxItems = dev_Settings.displayMaxItems_search;

        private const string viewIndex_cosmos = "~/Views/UserMedia/Search/Cosmos.cshtml";
        private const string viewIndex_table = "~/Views/UserMedia/Search/Table.cshtml";
        private const string viewIndex_blob = "~/Views/UserMedia/Search/Blob.cshtml";
        private const string viewIndex_sql = "~/Views/UserMedia/Search/SQL.cshtml";

        public UserMediaSearchController(UserManager<ApplicationUser> userManager, ApplicationDbContext context) : base(userManager, context)
        {
            _userManager = userManager;
            _context = context;
        }

        [Authorize]
        public IActionResult Index(string searchKeywords)
        {
            if (String.IsNullOrEmpty(HttpContext.Session.GetString(sessionKey_searchMode)))
                HttpContext.Session.SetString(sessionKey_searchMode, "Cosmos");

            switch (HttpContext.Session.GetString(sessionKey_searchMode))
            {
                case "Table": return RedirectToAction("Table", new { @searchKeywords = searchKeywords });
                case "Blob": return RedirectToAction("Blob", new { @searchKeywords = searchKeywords });
                //case "SQL": return RedirectToAction("SQL", new { @searchKeywords = searchKeywords });
                case "Cosmos":
                default: return RedirectToAction("Cosmos", new { @searchKeywords = searchKeywords });
            }
        }

        [Authorize]
        [HttpGet]

        public IActionResult SQL(string searchKeywords)
        {
            List<SearchResultUserMedia> searchResult = new();
            if (String.IsNullOrEmpty(searchKeywords)) return View(viewIndex_sql, searchResult);

            try
            {
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                HttpContext.Session.SetString(sessionKey_searchMode, "SQL");
                string loggedInUserId = _userManager.GetUserId(User);

                CognitiveSearchService searchService = new();
                searchResult = searchService.Search_SQL(searchKeywords, loggedInUserId);
                sw.Stop();

                Console.WriteLine(searchResult.Count.ToString());

                string SearchResultCount = searchResult.Count.ToString();

                Console.WriteLine(SearchResultCount);

                if (searchResult.Count > displayMaxItems) searchResult.RemoveRange(displayMaxItems, searchResult.Count- displayMaxItems);

                ViewBag.ResultInfo = "検索結果: " + SearchResultCount + " 件 ( 検索時間: " + sw.Elapsed.ToString() + " )";                
                ViewBag.SearchKeywords = searchKeywords;

                return View(viewIndex_sql, searchResult);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return View("Error");
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult Cosmos(string searchKeywords)
        {
            List<SearchResultUserMedia> searchResult = new();
            if (String.IsNullOrEmpty(searchKeywords)) return View(viewIndex_cosmos, searchResult);

            try
            {
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                HttpContext.Session.SetString(sessionKey_searchMode, "Cosmos");
                string loggedInUserId = _userManager.GetUserId(User);

                CognitiveSearchService searchService = new();
                searchResult = searchService.Search_Cosmos(searchKeywords, loggedInUserId);
                sw.Stop();

                string SearchResultCount = searchResult.Count.ToString();
                if (searchResult.Count > displayMaxItems) searchResult.RemoveRange(displayMaxItems, searchResult.Count - displayMaxItems);

                ViewBag.ResultInfo = "検索結果: " + SearchResultCount + " 件 ( 検索時間: " + sw.Elapsed.ToString() + " )";
                ViewBag.SearchKeywords = searchKeywords;

                return View(viewIndex_cosmos, searchResult);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return View("Error");
            }

        }

        [Authorize]
        [HttpGet]
        public IActionResult Table(string searchKeywords)
        {
            List<SearchResultUserMedia> searchResult = new();
            if (String.IsNullOrEmpty(searchKeywords)) return View(viewIndex_table, searchResult);

            try
            {
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                HttpContext.Session.SetString(sessionKey_searchMode, "Table");
                string loggedInUserId = _userManager.GetUserId(User);

                CognitiveSearchService searchService = new();
                searchResult = searchService.Search_Table(searchKeywords, loggedInUserId);
                sw.Stop();

                string SearchResultCount = searchResult.Count.ToString();
                if (searchResult.Count > displayMaxItems) searchResult.RemoveRange(displayMaxItems, searchResult.Count - displayMaxItems);

                ViewBag.ResultInfo = "検索結果: " + SearchResultCount + " 件 ( 検索時間: " + sw.Elapsed.ToString() + " )";
                ViewBag.SearchKeywords = searchKeywords;

                return View(viewIndex_table, searchResult);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return View("Error");
            }
        }

        [Authorize]
        public IActionResult Blob(string searchKeywords)
        {
            List<SearchResultUserMedia> searchResult = new();
            if (String.IsNullOrEmpty(searchKeywords)) return View(viewIndex_blob, searchResult);

            try
            {
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                HttpContext.Session.SetString(sessionKey_searchMode, "Blob");
                string loggedInUserId = _userManager.GetUserId(User);

                CognitiveSearchService searchService = new();
                searchResult = searchService.Search_Blob(searchKeywords, loggedInUserId);
                sw.Stop();

                string SearchResultCount = searchResult.Count.ToString();
                if (searchResult.Count > displayMaxItems) searchResult.RemoveRange(displayMaxItems, searchResult.Count - displayMaxItems);

                ViewBag.ResultInfo = "検索結果: " + SearchResultCount + " 件 ( 検索時間: " + sw.Elapsed.ToString() + " )";
                ViewBag.SearchKeywords = searchKeywords;

                return View(viewIndex_blob, searchResult);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return View("Error");
            }
        }
    }
}
