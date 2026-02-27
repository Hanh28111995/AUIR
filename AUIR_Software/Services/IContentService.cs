using AUIR_Software.Models;

namespace AUIR_Software.Services.IServices
{
    public interface IContentService
    {
        // Lấy toàn bộ danh sách nội dung
        Task<List<Content>> GetAllContentsAsync();

        // Lấy nội dung theo Key
        Task<Content?> GetContentById(int id);

        // Cập nhật giá trị mới cho một Key
        Task<bool> UpdateContentAsync(int id, SectionData newData, List<IFormFile> files, string sectionName);

        // Xóa nội dung theo Key
        Task<bool> DeleteContentAsync(int Id);

        // CHỈ GIỮ LẠI MỘT HÀM CREATE DUY NHẤT NÀY
        Task<bool> CreateContentAsync(Models.ContentCreateDto dto, string sectionName);
    }
}