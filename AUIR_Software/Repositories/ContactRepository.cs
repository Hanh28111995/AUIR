using AUIR_Software.Models;
using ApplicationDbContext.Data;
using Microsoft.EntityFrameworkCore;

namespace AUIR_Software.Repositories
{
    public class ContactRepository
    {
        private readonly ApplicationDbContext.Data.ApplicationDbContext _context;

        public ContactRepository(ApplicationDbContext.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Lấy toàn bộ danh sách liên hệ (Sắp xếp mới nhất lên đầu)
        public async Task<List<Contact>> GetAll()
        {
            return await _context.Contacts
                                 .OrderByDescending(c => c.Id) // Hoặc CreatedAt nếu bạn có trường đó
                                 .ToListAsync();
        }

        // 2. Lấy liên hệ theo ID
        public async Task<Contact?> GetById(int id)
        {
            return await _context.Contacts.FindAsync(id);
        }

        // 3. Thêm liên hệ mới (Khách gửi từ Landing Page)
        public async Task<bool> Add(Contact contact)
        {
            try
            {
                await _context.Contacts.AddAsync(contact);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // 4. Xóa liên hệ (Khi Admin dọn dẹp hòm thư)
        public async Task<bool> Delete(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact == null) return false;

            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}