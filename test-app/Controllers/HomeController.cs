using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TestApp.Models;
using TestApp.Services;

namespace TestApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {

        private readonly ResourceManagerService _resourceManagerService;

        // Inject the tag updater service
        public HomeController(ResourceManagerService resourceManagerService)
        {
            _resourceManagerService = resourceManagerService;
        }

        public IActionResult Index()
        {
            return View(new ResourceTagUpdateModel()
            {
                ResourceId = "resourceid",
                TagsInput = ""
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTags(string resourceId, string tags)
        {
            // Call your service to update tags.
            // For example, assume ResourceManagerService.UpdateTagsAsync returns a string result.
            var result = await _resourceManagerService.UpdateTagsAsync(resourceId, tags);
            ViewBag.Result = result;
            return View("Index");
        } 
    }
}