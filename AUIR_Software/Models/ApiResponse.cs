namespace AUIR_Software.Models
{
    /// <summary>
    /// Chuẩn hóa phản hồi JSON cho tất cả API
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }

        public static ApiResponse Ok(string? message = null, object? data = null) => new()
        {
            Success = true,
            Message = message,
            Data = data
        };

        public static ApiResponse Fail(string message, object? data = null) => new()
        {
            Success = false,
            Message = message,
            Data = data
        };
    }
}
