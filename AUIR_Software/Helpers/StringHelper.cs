namespace AUIR_Software.Helpers
{
    public static class StringHelper
    {
        /// <summary>
        /// Tách chuỗi theo ký tự '|', trim khoảng trắng và viết hoa chữ cái đầu mỗi phần tử.
        /// </summary>
        public static List<string> SplitAndFormat(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new List<string>();

            return input.Split('|')
                        .Select(s => s.Trim()) // Loại bỏ khoảng trống thừa đầu và cuối
                        .Where(s => !string.IsNullOrEmpty(s)) // Loại bỏ phần tử rỗng nếu có (ví dụ: "A||B")
                        .Select(s => char.ToUpper(s[0]) + s.Substring(1)) // Viết hoa chữ cái đầu
                        .ToList();
        }
    }
}