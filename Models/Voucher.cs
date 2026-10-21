using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanPC.Models
{
    [Table("Vouchers")]
    public class Voucher
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Mã voucher không được để trống")]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tiêu đề voucher không được để trống")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } // Số tiền giảm (VNĐ)

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderAmount { get; set; } = 0; // Đơn tối thiểu để áp dụng

        public int Quantity { get; set; } = 100; // Số lượt dùng còn lại

        public DateTime StartDate { get; set; } = DateTime.Now;

        public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(1);

        public bool IsActive { get; set; } = true;
    }
}