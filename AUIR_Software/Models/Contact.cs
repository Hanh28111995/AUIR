
namespace AUIR_Software.Models
{
    public partial class Contact
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
