using Microsoft.AspNetCore.Mvc;
using AUIR_Software.Models;
using AUIR_Software.Services.IServices;
using Microsoft.AspNetCore.Authorization;

namespace AUIR_Software.Controllers.Admin
{
    [Authorize] // Bảo mật: Chỉ admin mới được truy cập các hàm này
    public class UserController : Controller
    {
        private readonly IAuthService _authService;

        public UserController(IAuthService authService)
        {
            _authService = authService;
        }

        // Action cập nhật hoặc thêm mới User
        [HttpPost]
        public async Task<IActionResult> SaveUser(User userModel, string? Password)
        {
            try
            {
                bool result;
                if (userModel.Id == 0)
                {
                    // Logic cho Add New
                    result = await _authService.CreateUserAsync(userModel);
                }
                else
                {
                    // Logic cho Update
                    result = await _authService.UpdateUserAsync(userModel, Password);
                }

                if (result) return Json(new { success = true });
                return Json(new { success = false, message = "Thao tác thất bại từ tầng Service." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Action xóa User
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _authService.DeleteUserAsync(id);
            return Json(new { success = result });
        }
    }
}