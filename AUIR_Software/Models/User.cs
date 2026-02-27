namespace AUIR_Software.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;

        public string Password { get; set; } = null!;        
        public string? Email { get; set; } = null!;
        
    }
}