using System;
using System.Collections.Generic;

namespace WebBanPC.Models;

public partial class ProductAttribute
{
    public int ProductId { get; set; }

    public int AttributeId { get; set; }

    public string Value { get; set; } = null!;

    public virtual Attribute Attribute { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
