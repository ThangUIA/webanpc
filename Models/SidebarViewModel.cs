namespace WebBanPC.Models
{
    public class CategoryItemVm
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string IconClass { get; set; } = "fas fa-desktop";
        public int ProductCount { get; set; }
    }

    public class SidebarViewModel
    {
        public List<CategoryItemVm> Categories { get; set; } = new();
        public List<Brand> Brands { get; set; } = new();
        public List<Product> FeaturedProducts { get; set; } = new();
    }
}