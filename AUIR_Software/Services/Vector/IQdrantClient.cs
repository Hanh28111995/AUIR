namespace AUIR_Software.Services.Vector
{
    public interface IQdrantClient
    {
        // 1. Đảm bảo Collection tồn tại trong Qdrant
        Task EnsureCollectionAsync( int vectorSize, CancellationToken ct = default);

        // 2. Nạp (hoặc cập nhật) danh sách các Vector Chunks vào Qdrant
        Task UpsertAsync( IEnumerable<VecPoint> points, CancellationToken ct = default);

        // 3. Tìm kiếm các đoạn văn liên quan nhất (Sửa chính tả query)
        Task<List<VecHit>> SearchAsync( float[] query, int topK, CancellationToken ct = default);
    }

    // Các lớp Record và Model giữ nguyên cấu trúc rất tốt của bạn
    public record VecPoint(string Id, float[] Vector, string Text, string? Source);
    public record VecHit(string Id, float Score, string Text, string? Source); // Sửa Source viết hoa cho đồng bộ

    public class QdrantSearchResponse
    {
        public List<QdrantHit> Result { get; set; }
    }

    public class QdrantHit
    {
        public string Id { get; set; }
        public float Score { get; set; }
        public QdrantPayload Payload { get; set; }
    }

    public class QdrantPayload
    {
        public string Text { get; set; }
        public string Source { get; set; }
    }
}