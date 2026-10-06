using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace VuaBanhMy.Models
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
        public string Address { get; set; }
        // Bạn tự viết: thuộc tính Address
        //  - cho phép null (khách có thể chưa nhập địa chỉ)
        //  - tối đa 255 ký tự
    }
}