using AUIR_Software.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AUIR_Software.Controllers.Admin
{
    [Authorize] // Bảo mật toàn bộ Controller
    public class ContactController : Controller
    {
        private readonly IContactService _contactService;

        public ContactController(IContactService contactService)
        {
            _contactService = contactService;
        }

        // 1. Lấy chi tiết một tin nhắn (Dùng để hiện lên modal xem chi tiết)
        [HttpGet]
        public async Task<IActionResult> GetDetail(int id)
        {
            var contact = await _contactService.GetContactByIdAsync(id);
            if (contact == null) return NotFound(new { message = "Không tìm thấy tin nhắn." });

            return Json(new
            {
                success = true,
                data = contact
            });
        }

        // 2. Xóa tin nhắn khách hàng
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _contactService.DeleteContactAsync(id);
            if (result) return Json(new { success = true });

            return Json(new { success = false, message = "Lỗi khi xóa tin nhắn." });
        }
    }
}