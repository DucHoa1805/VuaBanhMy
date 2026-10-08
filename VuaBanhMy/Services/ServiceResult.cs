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

    // Kết quả có kèm dữ liệu trả về (VD: đường dẫn ảnh vừa lưu)
    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; init; }

        public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
        public static new ServiceResult<T> Fail(string message) => new() { Success = false, ErrorMessage = message };
    }
}
