using AUIR_Software.Models;

using AUIR_Software.Repositories;
using AUIR_Software.Services.IServices;
using Newtonsoft.Json;


namespace AUIR_Software.Services
{
    public class ContentService : IContentService
    {
        private readonly ContentRepository _contentRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ContentService(ContentRepository contentRepo, IWebHostEnvironment webHostEnvironment)
        {
            _contentRepo = contentRepo;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<bool> CreateContentAsync(Models.ContentCreateDto dto, string sectionName)
        {
            if (dto == null) return false;

            var sectionData = new SectionData();

            // 1. Map danh sách Introduce (hỗ trợ nhiều dòng nếu có)
            if (dto.Title_p != null)
            {
                for (int i = 0; i < dto.Title_p.Count; i++)
                {
                    sectionData.Introduce.Add(new Introduction
                    {
                        Title_p = dto.Title_p[i] ?? string.Empty,
                        Describe_p = (dto.Describe_p?.Count > i) ? dto.Describe_p[i] : string.Empty
                    });
                }
            }

            // 2. Map danh sách ContentItem (Title_c, Describe_c, Image)
            if (dto.Title_c != null)
            {
                for (int i = 0; i < dto.Title_c.Count; i++)
                {
                    string imageUrl = "https://placehold.co/300x200?text=No+Image";

                    // Xử lý upload ảnh từ danh sách Image của DTO
                    if (dto.Image != null && dto.Image.Count > i && dto.Image[i] != null && dto.Image[i].Length > 0)
                    {
                        imageUrl = await UploadImageAsync(dto.Image[i], sectionName);
                    }

                    sectionData.List.Add(new ContentItem
                    {
                        Title_c = dto.Title_c[i] ?? string.Empty,
                        Describe_c = (dto.Describe_c?.Count > i) ? dto.Describe_c[i] : string.Empty,
                        Image = imageUrl
                    });
                }
            }

            var content = new Content
            {
                SectionName = sectionName,
                RawJson = JsonConvert.SerializeObject(sectionData),
                UpdatedAt = DateTime.Now
            };

            return await _contentRepo.Add(content);
        }


        public async Task<bool> UpdateContentAsync(int id, SectionData newData, List<IFormFile> files, string sectionName)
        {
            var existingContent = await _contentRepo.GetById(id);
            if (existingContent == null || newData == null) return false;

            // Giải mã dữ liệu cũ để lấy danh sách ảnh hiện tại
            var oldData = JsonConvert.DeserializeObject<SectionData>(existingContent.RawJson) ?? new SectionData();
            const string placeholder = "https://placehold.co/300x200?text=No+Image";

            if (newData.List != null)
            {
                for (int i = 0; i < newData.List.Count; i++)
                {
                    // Trong vòng lặp for (int i = 0; i < newData.List.Count; i++)
                    var item = newData.List[i];
                    var file = (files != null && files.Count > i) ? files[i] : null;

                    // Lấy URL ảnh cũ từ DB để so sánh
                    string? oldImageUrl = (oldData.List != null && oldData.List.Count > i)
                                          ? oldData.List[i].Image
                                          : null;

                    // ĐIỀU KIỆN SỬA ĐỔI:
                    if (file != null && file.Length > 0 && file.FileName != "no-change")
                    {
                        // TRƯỜNG HỢP 1: CÓ FILE MỚI THẬT SỰ (Length > 0 và tên khác "no-change")
                        if (!string.IsNullOrEmpty(oldImageUrl) && !oldImageUrl.Contains("placehold.co"))
                        {
                            DeleteFile(oldImageUrl); // Xóa ảnh cũ nếu nó tồn tại trên server
                        }
                        // Thực hiện upload file mới
                        item.Image = await UploadImageAsync(file, sectionName);
                    }
                    else
                    {
                        // TRƯỜNG HỢP 2: KHÔNG CHỌN ẢNH MỚI (file là "no-change" hoặc null)
                        // Nếu có ảnh cũ thì giữ lại, nếu không có thì mới dùng placeholder
                        item.Image = !string.IsNullOrEmpty(oldImageUrl) ? oldImageUrl : "https://placehold.co/300x200?text=No+Image";
                    }
                }
            }

            // Cập nhật Database
            existingContent.SectionName = sectionName;
            existingContent.RawJson = JsonConvert.SerializeObject(newData);
            existingContent.UpdatedAt = DateTime.Now;

            return await _contentRepo.Update(existingContent);
        }

        public async Task<List<Content>> GetAllContentsAsync() => await _contentRepo.GetAll();

        public async Task<Content?> GetContentById(int id)
        {
            return await  _contentRepo.GetById(id);
        }

        public async Task<bool> DeleteContentAsync(int id)
        {
            var content = await _contentRepo.GetById(id);
            if (content == null) return false;

            try
            {
                // Giải mã dữ liệu từ JSON để tìm file ảnh
                var sectionData = JsonConvert.DeserializeObject<SectionData>(content.RawJson);
                if (sectionData?.List != null)
                {
                    foreach (var item in sectionData.List)
                    {
                        DeleteFile(item.Image); // Xóa ảnh vật lý
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi giải mã JSON khi xóa: {ex.Message}");
            }

            return await _contentRepo.Delete(content);
        }

        private async Task<string> UploadImageAsync(IFormFile file, string sectionName)
        {
            try
            {
                // Làm sạch sectionName để tạo folder an toàn
                string safeFolderName = string.Concat(sectionName.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "asset", "img", "bg", safeFolderName);

                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Trả về URL tương đối cho Web
                return $"/asset/img/bg/{safeFolderName}/{uniqueFileName}";
            }
            catch
            {
                return "https://placehold.co/300x200?text=Upload+Error";
            }
        }
        private void DeleteFile(string imageUrl)
        {
            try
            {
                // 1. Kiểm tra an toàn: Không xóa nếu trống hoặc là ảnh mặc định từ bên thứ 3
                if (string.IsNullOrEmpty(imageUrl) || imageUrl.Contains("placehold.co")) return;

                // 2. Chuyển URL thành đường dẫn vật lý (Ví dụ: /asset/img -> C:\wwwroot\asset\img)
                // TrimStart('/') để Path.Combine không hiểu nhầm là gốc ổ đĩa
                string relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);

                // 3. Thực hiện xóa nếu file tồn tại
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                    Console.WriteLine($"Đã xóa file cũ: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nhưng không làm sập cả chương trình
                Console.WriteLine($"Lỗi khi xóa file vật lý: {ex.Message}");
            }
        }

    }
}