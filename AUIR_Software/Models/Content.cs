using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace AUIR_Software.Models
{
    public class Content
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Tên Section")]
        public string SectionName { get; set; } = null!; // Ví dụ: Services, Hero, About

        [Required]
        public string RawJson { get; set; } = null!;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Thuộc tính hỗ trợ để truy cập nhanh dữ liệu đã giải mã
        [NotMapped]
        public SectionData? Data => string.IsNullOrEmpty(RawJson)
            ? new SectionData()
            : JsonConvert.DeserializeObject<SectionData>(RawJson);
    }

    // Cấu trúc Object bên trong RawJson
    public class SectionData
    {        
        public List<Introduction> Introduce { get; set; } = new();

        public List<ContentItem> List { get; set; } = new();
    }

    // Đối tượng trong mảng List
    public class ContentItem
    {
        public string  Title_c { get; set; } = string.Empty;
        public string  Describe_c  { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
    }
    public class Introduction
    {
        public string Title_p { get; set; } = string.Empty;
        public string Describe_p { get; set; } = string.Empty;        
    }
}