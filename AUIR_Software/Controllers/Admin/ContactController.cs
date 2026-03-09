using AUIR_Software.Models;
using AUIR_Software.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AUIR_Software.Controllers.Admin
{
    [Authorize]
    [Route("Admin/Contact/[action]")]
    public class ContactController : Controller
    {
        private readonly IContactService _contactService;

        public ContactController(IContactService contactService)
        {
            _contactService = contactService;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetDetail(int id)
        {
            var contact = await _contactService.GetContactByIdAsync(id);
            if (contact == null) return NotFound(ApiResponse.Fail("Không tìm thấy tin nhắn."));

            return Json(ApiResponse.Ok(null, contact));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _contactService.DeleteContactAsync(id);
            return Json(result ? ApiResponse.Ok("Xóa thành công!") : ApiResponse.Fail("Lỗi khi xóa tin nhắn."));
        }
    }
}