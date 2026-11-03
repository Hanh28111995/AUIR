using System.Text;
using UglyToad.PdfPig;

namespace AUIR_Software.Services.Text
{
    public static class PdfTextExtractor
    {
        public static string Extract(string filePath)
        {            
            var reader = new StringBuilder();
            using var document = PdfDocument.Open(filePath);
            foreach (var page in document.GetPages())
               reader.AppendLine(page.Text);
            return reader.ToString();
        }
    }
}
