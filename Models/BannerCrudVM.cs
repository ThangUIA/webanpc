using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebBanPC.Models
{
    public class BannerCrudVM
    {
        public int BannerId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề banner")]
        [Display(Name = "Tiêu đề banner")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Mô tả ngắn / Phụ đề")]
        public string? Subtitle { get; set; }

        [Display(Name = "Đường dẫn liên kết (Link)")]
        public string? LinkUrl { get; set; }

        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 1;

        [Display(Name = "Vị trí hiển thị")]
        public string Position { get; set; } = "MainCarousel";

        [Display(Name = "Hiển thị lên trang chủ")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Chọn ảnh mới")]
        public IFormFile? ImageFile { get; set; }

        public string? CurrentImageUrl { get; set; }
    }
}