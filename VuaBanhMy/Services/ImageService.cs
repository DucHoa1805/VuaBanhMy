namespace VuaBanhMy.Services
{
    public class ImageService(IWebHostEnvironment env) : IImageService
    {
        private const long MaxFileSize = 2 * 1024 * 1024; // 2 MB
        private const string ProductImageFolder = "images/products";

        // Đuôi file cho phép → các "chữ ký" (byte đầu file) hợp lệ tương ứng
        private static readonly Dictionary<string, byte[][]> AllowedSignatures = new()
        {
            [".jpg"] = [[0xFF, 0xD8, 0xFF]],
            [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
            [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
            [".webp"] = [[0x52, 0x49, 0x46, 0x46]], // "RIFF" (kiểm tra thêm "WEBP" ở byte 8-11)
        };

        public async Task<ServiceResult<string>> SaveProductImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return ServiceResult<string>.Fail("Vui lòng chọn file ảnh.");

            if (file.Length > MaxFileSize)
                return ServiceResult<string>.Fail("Ảnh tối đa 2 MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedSignatures.ContainsKey(extension))
                return ServiceResult<string>.Fail("Chỉ chấp nhận ảnh .jpg, .jpeg, .png, .webp.");

            // Đuôi file có thể bị đổi tên giả mạo → kiểm tra thêm nội dung thật
            if (!await HasValidSignatureAsync(file, extension))
                return ServiceResult<string>.Fail("File không phải là ảnh hợp lệ.");

            // Không dùng tên file người dùng gửi — tự đặt tên mới bằng Guid
            var fileName = $"{Guid.NewGuid()}{extension}";
            var folder = Path.Combine(env.WebRootPath, ProductImageFolder);
            Directory.CreateDirectory(folder);

            using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return ServiceResult<string>.Ok($"/{ProductImageFolder}/{fileName}");
        }

        private static async Task<bool> HasValidSignatureAsync(IFormFile file, string extension)
        {
            var header = new byte[12];
            int read;
            using (var stream = file.OpenReadStream())
            {
                read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
            }

            var matches = AllowedSignatures[extension]
                .Any(signature => read >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature));

            if (matches && extension == ".webp")
                matches = read >= 12 && header.AsSpan(8, 4).SequenceEqual("WEBP"u8);

            return matches;
        }
    }
}
