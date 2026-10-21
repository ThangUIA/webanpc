namespace WebBanPC.Models
{
    public class CategoryMenuVM
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }
}