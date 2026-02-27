using AUIR_Software.Models;

namespace AUIR_Software.Services.IServices;

public interface IAuthService
{
    Task<List<User>> GetAllUsersAsync();
    Task<bool> LoginAsync(string username, string password);
    Task<bool> CreateUserAsync(User user);
    Task<bool> UpdateUserAsync(User user, string? password);
    Task<bool> DeleteUserAsync(int id);
    // Bạn có thể thêm LogoutAsync nếu xử lý Cookie tại đây
    Task LogoutAsync();
}