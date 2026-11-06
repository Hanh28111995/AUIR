namespace AUIR_Software.Models.DTO;

/// <summary>Yêu cầu tìm kiếm câu trả lời trong kho tài liệu RAG.</summary>
public sealed record QueryRequest(
    [property: System.ComponentModel.DataAnnotations.Required]
    [property: System.ComponentModel.DataAnnotations.StringLength(2000, MinimumLength = 1)]
    string Question,
    [property: System.ComponentModel.DataAnnotations.Range(1, 20)]
    int? Topk);

/// <summary>Một đoạn tài liệu phù hợp với truy vấn.</summary>
public sealed record QueryHit(string Id, float Score, string Text, string? Source);

/// <summary>Câu trả lời cùng các đoạn tài liệu được dùng làm ngữ cảnh.</summary>
public sealed record QueryResponse(string? answer, List<QueryHit> Contexts);

/// <summary>Phản hồi truy vấn RAG.</summary>
public sealed record QueryApiResponse(bool Success, QueryResponse Response);
