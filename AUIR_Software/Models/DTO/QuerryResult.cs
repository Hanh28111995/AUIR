namespace AUIR_Software.Models.DTO;

public record QueryRequest(string Question, int? Topk);
public record QueryHit(string Id, float Score, string Text, string? Source);
public record QueryResponse(string? answer, List<QueryHit> Contexts);


