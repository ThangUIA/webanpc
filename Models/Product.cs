using System;
using System.Collections.Generic;

namespace WebBanPC.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public int CategoryId { get; set; }

    public int BrandId { get; set; }

    public string Sku { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public decimal? OldPrice { get; set; }

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public int WarrantyMonths { get; set; }

    public string? ShortDescription { get; set; }

    public string? FullDescription { get; set; }

    public bool IsNew { get; set; }

    public bool IsFeatured { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Brand Brand { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<ProductAttribute> ProductAttributes { get; set; } = new List<ProductAttribute>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
}
