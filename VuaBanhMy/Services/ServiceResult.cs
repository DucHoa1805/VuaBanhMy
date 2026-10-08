namespace VuaBanhMy.Services
{
    // Kết quả trả về từ service: thành công, hoặc thất bại kèm thông báo cho người dùng
    public class ServiceResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }

        public static ServiceResult Ok() => new() { Success = true };
        public static ServiceResult Fail(string message) => new() { Success = false, ErrorMessage = message };
    }
}
