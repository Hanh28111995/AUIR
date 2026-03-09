using AUIR_Software.Models;
using AUIR_Software.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AUIR_Software.Controllers.Admin
{
    [Authorize]
    [Route("Admin/Content/[action]")]
    public class ContentController : Controller
    {
        private readonly IContentService _contentService;

        public ContentController(IContentService contentService)
        {
            _contentService = contentService;
        }

        // 1. THÊM MỚI (Sửa lại để nhận DTO thay vì string)
        
        [HttpPost]
        public async Task<IActionResult> Create( ContentCreateDto dto, string sectionName)
        {
            // 1. Kiểm tra sectionName được truyền riêng từ tham số Action
            if (string.IsNullOrEmpty(sectionName))
            {
                return Json(ApiResponse.Fail("Tên Section không được để trống!"));
            }

            // 2. Kiểm tra tính hợp lệ của Model (ItemTitles, ItemImages...)
            if (!ModelState.IsValid)
            {
                return Json(ApiResponse.Fail("Dữ liệu nhập vào không hợp lệ!"));
            }

            try
            {
                // 3. Gọi Service và truyền riêng lẻ sectionName cùng với dto
                // Service sẽ lo việc upload ảnh vào root và đóng gói JSON
                var result = await _contentService.CreateContentAsync(dto, sectionName);

                if (result)
                {
                    return Json(ApiResponse.Ok("Thêm mới Section thành công!"));
                }

                return Json(ApiResponse.Fail("Section đã tồn tại hoặc lưu thất bại."));
            }
            catch (Exception ex)
            {
                return Json(ApiResponse.Fail("Lỗi hệ thống: " + ex.Message));
            }
        }

        // 2. CẬP NHẬT (Giữ nguyên hoặc tùy biến theo nhu cầu)
        [HttpPost]        
        public async Task<IActionResult> Update(int id, string sectionName, string jsonData, List<IFormFile> files)
        {
            try
            {
                // Giải mã chuỗi JSON từ Client gửi lên thành Object SectionData
                var newData = JsonConvert.DeserializeObject<SectionData>(jsonData);

                if (newData == null) return Json(ApiResponse.Fail("Dữ liệu JSON không hợp lệ"));

                var result = await _contentService.UpdateContentAsync(id, newData, files, sectionName);

                return Json(result ? ApiResponse.Ok("Cập nhật thành công!") : ApiResponse.Fail("Cập nhật thất bại."));
            }
            catch (Exception ex)
            {
                return Json(ApiResponse.Fail(ex.Message));
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetContentById(int id)
        {
            var content = await _contentService.GetContentById(id);
            if (content == null) return NotFound();


            var sectionData = JsonConvert.DeserializeObject<SectionData>(content.RawJson);

            var data = new
            {
                id = content.Id,
                sectionName = content.SectionName,
                introduction = sectionData?.Introduce,
                list = sectionData?.List
            };

            return Json(ApiResponse.Ok(null, data));
        }

        
        [HttpPost]
        public async Task<IActionResult> Delete(int ID)
        {
            var result = await _contentService.DeleteContentAsync(ID);
            return Json(result ? ApiResponse.Ok("Xóa thành công!") : ApiResponse.Fail("Không thể xóa."));
        }
    }
}