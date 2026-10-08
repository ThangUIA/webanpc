namespace WebBanPC.Models
{
    public class HomeIndexVM
    {
        public List<Banner> MainBanners { get; set; } = new();
        public Banner? RightBanner { get; set; }
        public List<Product> NewArrivals { get; set; } = new();
        public List<Product> FeaturedProducts { get; set; } = new();
        public List<Product> BestSellers { get; set; } = new();
        public List<Product> AllProducts { get; set; } = new();
    }
}