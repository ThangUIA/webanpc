using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanPC.Models
{
    [Table("Banners")]
    public class Banner
    {
        [Key]
        public int BannerId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề banner")]
        [StringLength(250)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [StringLength(250)]
        [Display(Name = "Phụ đề / Khuyến mãi ngắn")]
        public string? Subtitle { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Đường dẫn ảnh")]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Link khi bấm vào banner")]
        public string? LinkUrl { get; set; } = "/Home/Index";

        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 1;

        [Display(Name = "Vị trí")]
        public string Position { get; set; } = "MainCarousel"; // 'MainCarousel' (Slider lớn), 'HeaderRight' (Góc phải), 'MidBanner' (Giữa trang)

        [Display(Name = "Kích hoạt hiển thị")]
        public bool IsActive { get; set; } = true;

        public DateTime? CreatedAt { get; set; } = DateTime.Now;
    }
}