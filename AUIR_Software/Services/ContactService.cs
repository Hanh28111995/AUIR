using AUIR_Software.Models;
using AUIR_Software.Repositories;
using AUIR_Software.Services.IServices;

namespace AUIR_Software.Services
{
    public class ContactService : IContactService
    {
        private readonly ContactRepository _contactRepo;

        public ContactService(ContactRepository contactRepo)
        {
            _contactRepo = contactRepo;
        }

        public async Task<List<Contact>> GetAllContactsAsync()
        {            
            return await _contactRepo.GetAll();
        }

        public async Task<Contact?> GetContactByIdAsync(int id)
        {
            return await _contactRepo.GetById(id);
        }

        public async Task<bool> DeleteContactAsync(int id)
        {
            return await _contactRepo.Delete(id);
        }

        public async Task<bool> SubmitContactAsync(Contact contact)
        {
            // Có thể thêm logic xử lý trước khi lưu, ví dụ: Validate email, lọc spam
            contact.CreatedAt = DateTime.Now; // Giả sử model có trường ngày tạo
            return await _contactRepo.Add(contact);
        }
    }
}

