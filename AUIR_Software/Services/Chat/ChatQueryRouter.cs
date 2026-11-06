using System.Globalization;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace AUIR_Software.Services.Chat;

public sealed record ChatRoute(string Query, string? SourceHint, string? StaticReply);

public sealed class ChatQueryRouter
{
    private const string ContactSource = "lien-he.txt";
    private readonly IMemoryCache _cache;

    public ChatQueryRouter(IMemoryCache cache)
    {
        _cache = cache;
    }

    public ChatRoute Route(string message, string conversationId)
    {
        var normalized = Normalize(message);
        if (normalized is "xin chao" or "chao" or "hello" or "hi" or "hey")
        {
            return new ChatRoute(
                message,
                null,
                "Xin chào! Tôi có thể hỗ trợ bạn tìm thông tin về các dịch vụ của AUIR.");
        }

        if (normalized is "cam on" or "thanks" or "thank you")
        {
            return new ChatRoute(message, null, "Rất vui được hỗ trợ bạn.");
        }

        if (ContainsAny(normalized, "sdt", "so dien thoai", "dien thoai", "hotline", "lien he", "dia chi"))
        {
            RememberSource(conversationId, ContactSource);
            return new ChatRoute(
                $"thông tin liên hệ, số điện thoại, hotline, địa chỉ của AUIR: {message}",
                ContactSource,
                null);
        }

        return new ChatRoute(message, GetSource(conversationId), null);
    }

    public void RememberSource(string conversationId, string source)
    {
        if (!string.IsNullOrWhiteSpace(conversationId) && !string.IsNullOrWhiteSpace(source))
        {
            _cache.Set(GetKey(conversationId), source, TimeSpan.FromHours(2));
        }
    }

    private string? GetSource(string conversationId) =>
        _cache.TryGetValue(GetKey(conversationId), out string? source) ? source : null;

    private static string GetKey(string conversationId) => $"chat-source:{conversationId}";

    private static bool ContainsAny(string value, params string[] terms) =>
        terms.Any(value.Contains);

    private static string Normalize(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
