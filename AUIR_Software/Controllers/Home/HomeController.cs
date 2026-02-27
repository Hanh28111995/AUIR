using AUIR_Software.Models;
using AUIR_Software.Services;
using AUIR_Software.Services.IServices;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;

namespace AUIR_Software.Controllers.Home    
{
    public class HomeController : Controller
    {
        private readonly IContentService _contentService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger,  IContentService contentService)
        {
            _logger = logger;
            _contentService = contentService;
        }

        public async Task<IActionResult> Index()
        {
            var contents = await _contentService.GetAllContentsAsync();

            ViewBag.ContentList = contents.ToList();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}