using AUIR_Software.Models;
using AUIR_Software.Repositories;
using AUIR_Software.Services.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace AUIR_Software.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserRepository _userRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(UserRepository userRepo, IHttpContextAccessor httpContextAccessor)
        {
            _userRepo = userRepo;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _userRepo.GetAll();
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var user = await _userRepo.ValidateUser(username, password);
            if (user == null) return false;

            // Tạo danh tính (Identity)
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Role, "Admin")
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            if (_httpContextAccessor.HttpContext != null)
            {
                // Thiết lập thuộc tính Cookie (Ghi nhớ đăng nhập, thời gian hết hạn)
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                };

                await _httpContextAccessor.HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return true;
            }

            return false;
        }

        public async Task<bool> UpdateUserAsync(User user, string? newPassword)
        {
            var existingUser = await _userRepo.GetById(user.Id); // Tìm theo ID thay vì Username
            if (existingUser == null) return false;

            // Cập nhật thông tin
            existingUser.Username = user.Username;
            existingUser.Email = user.Email;
            existingUser.PhoneNumber = user.PhoneNumber;

            // Nếu admin nhập mật khẩu mới thì mới cập nhật
            if (!string.IsNullOrEmpty(newPassword))
            {
                existingUser.Password = newPassword; // Nhớ hash mật khẩu nếu Repository chưa làm
            }

            return await _userRepo.Update(existingUser);
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            return await _userRepo.Delete(id);
        }

        public async Task LogoutAsync()
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }

        public async Task<bool> CreateUserAsync(User user)
        {
            var existingUser = await _userRepo.GetByUsername(user.Username);
            if (existingUser != null) return false; // Tài khoản đã tồn tại

            return await _userRepo.Add(user);
        }

        
    }
}