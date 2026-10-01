using System.ComponentModel.DataAnnotations;

namespace WebBanPC.Models
{
    public class CheckoutVM
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [Display(Name = "Họ và tên")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string CustomerPhone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
        [Display(Name = "Email")]
        public string? CustomerEmail { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Display(Name = "Ghi chú đơn hàng")]
        public string? Note { get; set; }

        // Dữ liệu hiển thị bảng sản phẩm & tính tiền
        public List<CartItem> CartItems { get; set; } = new();
        public decimal Subtotal => CartItems.Sum(x => x.Total);
        public decimal ShippingFee => CartItems.Any() ? 30000 : 0; // Phí ship cố định 30k
        public decimal TotalAmount => Subtotal + ShippingFee;
    }
}