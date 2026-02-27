using AUIR_Software.Models;

namespace AUIR_Software.Services.IServices
{
    public interface IContactService
    {
        // Lấy toàn bộ danh sách liên hệ (Contact)
        Task<List<Contact>> GetAllContactsAsync();

        // Lấy một liên hệ cụ thể theo ID
        Task<Contact?> GetContactByIdAsync(int id);

        // Xóa một liên hệ
        Task<bool> DeleteContactAsync(int id);

        // Lưu liên hệ mới (dùng cho phía trang chủ Landing Page)
        Task<bool> SubmitContactAsync(Contact contact);
    }
}