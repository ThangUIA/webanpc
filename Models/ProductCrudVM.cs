using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebBanPC.Models
{
    public class ProductCrudVM
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [StringLength(250)]
        [Display(Name = "Tên linh kiện")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mã SKU")]
        [StringLength(50)]
        public string Sku { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thương hiệu")]
        [Display(Name = "Thương hiệu")]
        public int BrandId { get; set; }

        [Display(Name = "Giá niêm yết cũ")]
        public decimal? OldPrice { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá bán")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán không hợp lệ")]
        [Display(Name = "Giá bán hiện tại")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng")]
        [Range(0, 10000, ErrorMessage = "Số lượng kho từ 0 đến 10000")]
        [Display(Name = "Số lượng trong kho")]
        public int StockQuantity { get; set; }

        [Display(Name = "Bảo hành (tháng)")]
        public int WarrantyMonths { get; set; } = 36;

        [Display(Name = "Mô tả ngắn")]
        public string? ShortDescription { get; set; }

        [Display(Name = "Mô tả chi tiết")]
        public string? FullDescription { get; set; }

        [Display(Name = "Sản phẩm mới")]
        public bool IsNew { get; set; } = true;

        [Display(Name = "Sản phẩm nổi bật")]
        public bool IsFeatured { get; set; } = false;

        // File upload từ giao diện
        [Display(Name = "Ảnh đại diện")]
        public IFormFile? ImageFile { get; set; }

        // Chuỗi lưu đường dẫn ảnh hiện tại (dùng khi chỉnh sửa)
        public string? CurrentImageUrl { get; set; }
    }
}