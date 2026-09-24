using System;
using System.Collections.Generic;

namespace WebBanPC.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int? UserId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string CustomerPhone { get; set; } = null!;

    public string? CustomerEmail { get; set; }

    public string ShippingAddress { get; set; } = null!;

    public string? Note { get; set; }

    public decimal TotalAmount { get; set; }

    public int OrderStatus { get; set; }

    public DateTime? OrderDate { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual User? User { get; set; }
}
