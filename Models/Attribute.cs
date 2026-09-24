using System;
using System.Collections.Generic;

namespace WebBanPC.Models;

public partial class Attribute
{
    public int AttributeId { get; set; }

    public string AttributeName { get; set; } = null!;

    public virtual ICollection<ProductAttribute> ProductAttributes { get; set; } = new List<ProductAttribute>();
}
