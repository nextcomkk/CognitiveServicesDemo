using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using CognitiveServicesDemo.Data;
using CognitiveServicesDemo.Models;
using CognitiveServicesDemo.Services;
using CognitiveServicesDemo.Utilities;
using CognitiveServicesDemo;

namespace CognitiveServicesDemo.Controllers
{
    public class UserMediaManageController : UserMediaController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const int pageSize = dev_Settings.pageSize_manage;

        StorageBlobService blobService;
        Base64stringUtility encode = new("UTF-8");

        public UserMediaManageController(UserManager<ApplicationUser> userManager, ApplicationDbContext context): base(userManager, context)
        {
            _userManager = userManager;
            _context = context;
            blobService = new StorageBlobService();
        }

        [Authorize]
        public IActionResult Index(int? pageNumber)
        {
            string loggedInUserId = _userManager.GetUserId(User);
            var userMedias = (from a in _context.UserMedia where a.UserId == loggedInUserId orderby a.MediaId descending select a);       // Linking images to users
            //var userMedias = (from a in _context.UserMedia orderby a.MediaId descending select a);                                      // Share images with all users
            ViewBag.ItemCount = userMedias.Count();

            return View("~/Views/UserMedia/Manage/Index.cshtml", PagedListData(userMedias, pageNumber).Result);
        }

        [Authorize]
        public async Task<PaginatedList<UserMedia>> PagedListData(IOrderedQueryable<UserMedia> userMedias, int? pageNumber)
        {
            return await PaginatedList<UserMedia>.CreateAsync(userMedias, pageNumber ?? 1, pageSize);
        } 
    }
}
