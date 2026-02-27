using AUIR_Software.Services.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AUIR_Software.Controllers.Admin
{
    // Để [AllowAnonymous] ở đây để hàm Index và LoginPage luôn truy cập được
    [AllowAnonymous]
    public class AdminController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IContactService _contactService;
        private readonly IContentService _contentService;

        public AdminController(IAuthService authService, IContactService contactService, IContentService contentService)
        {
            _authService = authService;
            _contactService = contactService;
            _contentService = contentService;
        }

        // 1. TRANG CHỦ ADMIN 
        [HttpGet]
        public async Task<IActionResult> Index()
        {
           await GetDashboardData();
           return View();
        }

        // 2. TRANG ĐĂNG NHẬP (Dùng nếu bạn muốn trang riêng, nếu dùng Modal thì có thể bỏ qua)
        [HttpGet]
        public IActionResult LoginPage()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        // 3. XỬ LÝ ĐĂNG NHẬP (API)
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var result = await _authService.LoginAsync(username, password);
            if (result)
            {                
                return RedirectToAction("Index", "Admin");
            }
            
            TempData["Error"] = "Tài khoản hoặc mật khẩu không đúng!";
            return RedirectToAction("Index");
        }

        // 4. XỬ LÝ ĐĂNG XUẤT (API)
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Json(new { success = true });
        }

        // 5. LẤY DỮ LIỆU DASHBOARD (API Bảo mật cao)
        [Authorize] // CHỈ hàm này mới thực sự chặn để bảo vệ dữ liệu
        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                var users = await _authService.GetAllUsersAsync();
                var contacts = await _contactService.GetAllContactsAsync();
                var contents = await _contentService.GetAllContentsAsync();

                ViewBag.UserList = users;
                ViewBag.ContactList = contacts;
                ViewBag.ContentList = contents;

                return PartialView("_DashboardData");
            }
            catch (Exception)
            {
                // Nên log lỗi ex ở đây nếu cần
                return StatusCode(500, "Lỗi khi tải dữ liệu hệ thống");
            }
        }
    }
}