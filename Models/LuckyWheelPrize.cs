using System;
using System.ComponentModel.DataAnnotations;

namespace WebBanPC.Models
{
    public class LuckyWheelPrize
    {
        [Key]
        public int PrizeId { get; set; }

        [Required(ErrorMessage = "Tên phần thưởng không được để trống")]
        [Display(Name = "Tên phần thưởng")]
        [StringLength(150)]
        public string PrizeName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn màu sắc")]
        [Display(Name = "Mã màu")]
        [StringLength(20)]
        public string ColorHex { get; set; } = "#e74c3c";

        [Required(ErrorMessage = "Trọng số không được để trống")]
        [Display(Name = "Tỷ lệ / Trọng số trúng")]
        [Range(1, 1000, ErrorMessage = "Trọng số phải từ 1 đến 1000")]
        public int Probability { get; set; } = 10;

        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 1;

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        public DateTime? CreatedAt { get; set; } = DateTime.Now;
    }
}