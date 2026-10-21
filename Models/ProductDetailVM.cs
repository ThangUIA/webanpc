namespace WebBanPC.Models
{
    public class ProductDetailVM
    {
        public Product Product { get; set; } = null!;
        public List<Product> RelatedProducts { get; set; } = new();
    }
}