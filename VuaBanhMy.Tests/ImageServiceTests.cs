using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using VuaBanhMy.Services;

namespace VuaBanhMy.Tests
{
    public class ImageServiceTests : IDisposable
    {
        // Mỗi test dùng một thư mục wwwroot tạm riêng, xóa khi test xong
        private readonly string _webRoot = Path.Combine(Path.GetTempPath(), "VuaBanhMyTests", Guid.NewGuid().ToString());
        private readonly ImageService _service;

        // Byte đầu (chữ ký) của file PNG / JPEG thật
        private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        private static readonly byte[] JpegHeader = [0xFF, 0xD8, 0xFF, 0xE0];

        public ImageServiceTests()
        {
            Directory.CreateDirectory(_webRoot);
            _service = new ImageService(new FakeWebHostEnvironment { WebRootPath = _webRoot });
        }

        public void Dispose() => Directory.Delete(_webRoot, recursive: true);

        private static IFormFile MakeFile(string fileName, byte[] content)
        {
            var stream = new MemoryStream(content);
            return new FormFile(stream, 0, content.Length, "ImageFile", fileName);
        }

        private static byte[] WithPadding(byte[] header, int totalLength)
        {
            var bytes = new byte[totalLength];
            header.CopyTo(bytes, 0);
            return bytes;
        }

        private string ProductsFolder => Path.Combine(_webRoot, "images", "products");

        [Fact]
        public async Task SaveProductImageAsync_NoFile_Fails()
        {
            var result = await _service.SaveProductImageAsync(null);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task SaveProductImageAsync_EmptyFile_Fails()
        {
            var result = await _service.SaveProductImageAsync(MakeFile("a.png", []));

            Assert.False(result.Success);
        }

        [Fact]
        public async Task SaveProductImageAsync_TooLarge_Fails()
        {
            var bigPng = WithPadding(PngHeader, 2 * 1024 * 1024 + 1); // vượt 2 MB 1 byte

            var result = await _service.SaveProductImageAsync(MakeFile("big.png", bigPng));

            Assert.False(result.Success);
            Assert.False(Directory.Exists(ProductsFolder) && Directory.EnumerateFiles(ProductsFolder).Any());
        }

        [Theory]
        [InlineData("virus.exe")]
        [InlineData("note.txt")]
        [InlineData("noextension")]
        public async Task SaveProductImageAsync_ExtensionNotAllowed_Fails(string fileName)
        {
            var result = await _service.SaveProductImageAsync(MakeFile(fileName, WithPadding(PngHeader, 100)));

            Assert.False(result.Success);
        }

        [Fact]
        public async Task SaveProductImageAsync_FakeImage_Fails()
        {
            // File chữ đổi tên thành .jpg — đuôi hợp lệ nhưng nội dung không phải ảnh
            var text = System.Text.Encoding.UTF8.GetBytes("day khong phai la anh");

            var result = await _service.SaveProductImageAsync(MakeFile("fake.jpg", text));

            Assert.False(result.Success);
        }

        [Fact]
        public async Task SaveProductImageAsync_ValidJpeg_SavesWithGuidName()
        {
            var result = await _service.SaveProductImageAsync(MakeFile("../../Bánh Mì.JPG", WithPadding(JpegHeader, 500)));

            Assert.True(result.Success);
            Assert.StartsWith("/images/products/", result.Data);
            Assert.EndsWith(".jpg", result.Data);
            var fileName = Path.GetFileNameWithoutExtension(result.Data!);
            Assert.True(Guid.TryParse(fileName, out _)); // tên file là Guid, không dùng tên người dùng gửi
            var savedPath = Path.Combine(ProductsFolder, Path.GetFileName(result.Data!));
            Assert.Equal(500, new FileInfo(savedPath).Length);
        }

        [Fact]
        public async Task SaveProductImageAsync_ValidPng_Succeeds()
        {
            var result = await _service.SaveProductImageAsync(MakeFile("anh.png", WithPadding(PngHeader, 100)));

            Assert.True(result.Success);
            Assert.EndsWith(".png", result.Data);
        }

        private class FakeWebHostEnvironment : IWebHostEnvironment
        {
            public string WebRootPath { get; set; } = string.Empty;
            public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
            public string ApplicationName { get; set; } = "VuaBanhMy";
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
            public string ContentRootPath { get; set; } = string.Empty;
            public string EnvironmentName { get; set; } = "Test";
        }
    }
}
