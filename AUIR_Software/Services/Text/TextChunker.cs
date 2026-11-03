using System.Text;

namespace AUIR_Software.Services.Text
{
    using System.Text;
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

        public static IEnumerable<string> Chunk(string text, int chunkSize, int chunkOverlap)
        {
            if(string.IsNullOrWhiteSpace(text)) yield break;
            var cleaned = Normalize(text);
            int start = 0;
            while (start < cleaned.Length)
            {
                int end = Math.Min(start + chunkSize, cleaned.Length);
                int lastPeriod = cleaned.LastIndexOf('.', end - 1, end - start);
                if (lastPeriod > start + 100) end = lastPeriod+1;
                yield return cleaned.Substring(start, end - start).Trim();
                if(end >= cleaned.Length) break;
                start = Math.Max(0, end - chunkOverlap);
            }                     
        }        
    }
}
