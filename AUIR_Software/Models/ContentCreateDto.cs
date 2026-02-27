namespace AUIR_Software.Models
{
    public class ContentCreateDto
    {
        public string? SectionName { get; set; }

        public List<string> Title_p { get; set; } = new();
        public List<string> Describe_p { get; set; } = new();

        public List<string> Title_c { get; set; } = new();
        public List<string> Describe_c { get; set; } = new();
        public List<IFormFile> Image { get; set; } = new();
    }
}