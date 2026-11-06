using System.Text;

namespace AUIR_Software.Services.Text
{
    public class TextChunker
    {
        static string Normalize(string text)
        {
            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                sb.Append(char.IsControl(c) && c != '\n' && c != '\r' ? ' ' : c);
            }
            return sb.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
        }

        public static IEnumerable<string> Chunk(
       string text,
       int chunkSize,
       int chunkOverlap)
        {
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));

            if (chunkOverlap < 0 || chunkOverlap >= chunkSize)
                throw new ArgumentOutOfRangeException(nameof(chunkOverlap));

            if (string.IsNullOrWhiteSpace(text))
                yield break;

            var cleaned = Normalize(text);
            var start = 0;

            while (start < cleaned.Length)
            {
                var end = Math.Min(start + chunkSize, cleaned.Length);

                if (end < cleaned.Length)
                {
                    // Chỉ chọn dấu chấm gần cuối chunk để tránh bước tiến quá ngắn.
                    var searchStart = Math.Max(
                        start + chunkOverlap + 1,
                        end - chunkOverlap);
                    var searchLength = end - searchStart;

                    if (searchLength > 0)
                    {
                        var lastPeriod = cleaned.LastIndexOf(
                            '.',
                            end - 1,
                            searchLength);

                        if (lastPeriod >= searchStart)
                            end = lastPeriod + 1;
                    }
                }

                var chunk = cleaned.Substring(start, end - start).Trim();
                if (chunk.Length > 0)
                    yield return chunk;

                if (end >= cleaned.Length)
                    break;

                var nextStart = end - chunkOverlap;

                // Rào bảo vệ bắt buộc: luôn tiến về phía trước.
                if (nextStart <= start)
                    nextStart = start + 1;

                start = nextStart;
            }
        }
    }
}
