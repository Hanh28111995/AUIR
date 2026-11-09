using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AUIR_Software.Models.LLM;
using Microsoft.Extensions.Caching.Memory;

namespace AUIR_Software.Services.Chat;

public sealed class ChatAnswerCache
{
    private readonly MemoryCache _cache = new(
        new MemoryCacheOptions { SizeLimit = 500 });

    private long _knowledgeVersion;

    public string CreateKey(
        string query,
        string? sourceHint,
        AppConfig config)
    {
        var rawKey = string.Join("|",
            Interlocked.Read(ref _knowledgeVersion),
            Normalize(query),
            sourceHint?.Trim() ?? "",
            config.Rag.TopK,
            config.Rag.MinScore.ToString(CultureInfo.InvariantCulture),
            config.Gemini.ChatModel);

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
    }

    public bool TryGet(string key, out string answer)
    {
        if (_cache.TryGetValue(key, out string? cached) &&
            !string.IsNullOrWhiteSpace(cached))
        {
            answer = cached;
            return true;
        }

        answer = string.Empty;
        return false;
    }

    public void Set(string key, string answer)
    {
        if (string.IsNullOrWhiteSpace(answer)) return;

        _cache.Set(key, answer, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2),
            Size = 1
        });
    }

    public void InvalidateKnowledge()
    {
        Interlocked.Increment(ref _knowledgeVersion);
    }

    private static string Normalize(string value)
    {
        return Regex.Replace(value.Trim().ToLowerInvariant(), @"\s+", " ");
    }
}