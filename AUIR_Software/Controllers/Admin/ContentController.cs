using AUIR_Software.Models;
using AUIR_Software.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AUIR_Software.Controllers.Admin
{
    [Authorize]
    public class ContentController : Controller
    {
        private readonly IContentService _contentService;

        public ContentController(IContentService contentService)
        {
            _contentService = contentService;
        }

        // 1. THÊM MỚI (Sửa lại để nhận DTO thay vì string)
        [Route("Admin/Content/[action]")]
        [HttpPost]
        public async Task<IActionResult> Create( ContentCreateDto dto, string sectionName)
        {
            // 1. Kiểm tra sectionName được truyền riêng từ tham số Action
            if (string.IsNullOrEmpty(sectionName))
            {
                return Json(new { success = false, message = "Tên Section không được để trống!" });
            }

            // 2. Kiểm tra tính hợp lệ của Model (ItemTitles, ItemImages...)
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu nhập vào không hợp lệ!" });
            }

            try
            {
                // 3. Gọi Service và truyền riêng lẻ sectionName cùng với dto
                // Service sẽ lo việc upload ảnh vào root và đóng gói JSON
                var result = await _contentService.CreateContentAsync(dto, sectionName);

                if (result)
                {
                    return Json(new { success = true, message = "Thêm mới Section thành công!" });
                }

                return Json(new { success = false, message = "Section đã tồn tại hoặc lưu thất bại." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // 2. CẬP NHẬT (Giữ nguyên hoặc tùy biến theo nhu cầu)
        [HttpPost]
        [Route("Admin/Content/Update")]
        public async Task<IActionResult> Update(int id, string sectionName, string jsonData, List<IFormFile> files)
        {
            try
            {
                // Giải mã chuỗi JSON từ Client gửi lên thành Object SectionData
                var newData = JsonConvert.DeserializeObject<SectionData>(jsonData);

                if (newData == null) return Json(new { success = false, message = "Dữ liệu JSON không hợp lệ" });

                var result = await _contentService.UpdateContentAsync(id, newData, files, sectionName);

                return Json(new { success = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Route("Admin/Content/[action]")]
        [HttpGet]
        public async Task<IActionResult> GetContentById(int id)
        {
            var content = await _contentService.GetContentById(id);
            if (content == null) return NotFound();


            var sectionData = JsonConvert.DeserializeObject<SectionData>(content.RawJson);

            return Json(new
            {
                success = true,
                data = new
                {
                    id = content.Id,
                    sectionName = content.SectionName,                    
                    introduction = sectionData?.Introduce,
                    list = sectionData?.List
                }
            });
        }


            [Route("Admin/Content/[action]")]
        [HttpPost]
        public async Task<IActionResult> Delete(int ID)
        {            
            var result = await _contentService.DeleteContentAsync(ID);
            return Json(new { success = result });
        }
    }
}