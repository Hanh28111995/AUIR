
using AUIR_Software.Models;
using Microsoft.EntityFrameworkCore;

namespace AUIR_Software.Repositories
{
    public class UserRepository
    {
        private readonly ApplicationDbContext.Data.ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        // Kiểm tra đăng nhập
        public async Task<User?> ValidateUser(string username, string password)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);
        }

        // Lấy thông tin User theo ID
        public async Task<User?> GetById(int id)
        {
            return await _context.Users.FindAsync(id);
        }
        // Lấy thông tin All User 
        public async Task<List<User>> GetAll()
        {
            return await _context.Users.ToListAsync();
        }

        // Tạo New User 
        public async Task<bool> Add(User user)
        {
            try
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Cập nhật thông tin User
        public async Task<bool> Update(User user)
        {
            try
            {
                // Gắn đối tượng vào context và đánh dấu là đã thay đổi (Modified)
                _context.Users.Update(user);

                // Lưu các thay đổi xuống Database
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Xử lý lỗi nếu bản ghi không còn tồn tại hoặc có lỗi đồng thời
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Xóa một user
        public async Task<bool> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        // Thêm hàm kiểm tra trùng tên (Rất quan trọng cho logic Service)
        public async Task<User?> GetByUsername(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

    }
}