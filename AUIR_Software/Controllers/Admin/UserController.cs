using Microsoft.AspNetCore.Mvc;
using AUIR_Software.Models;
using AUIR_Software.Services.IServices;
using Microsoft.AspNetCore.Authorization;

namespace AUIR_Software.Controllers.Admin
{
    [Authorize]
    [Route("Admin/User/[action]")]
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

                if (result) return Json(ApiResponse.Ok());
                return Json(ApiResponse.Fail("Thao tác thất bại từ tầng Service."));
            }
            catch (Exception ex)
            {
                return Json(ApiResponse.Fail(ex.Message));
            }
        }

        // Action xóa User
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _authService.DeleteUserAsync(id);
            return Json(result ? ApiResponse.Ok() : ApiResponse.Fail("Không thể xóa người dùng."));
        }
    }
}