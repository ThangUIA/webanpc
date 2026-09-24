using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanPC.Models;

namespace WebBanPC.ViewComponents
{
    public class SidebarViewComponent : ViewComponent
    {
        private readonly PcStoreDbContext _context;

        public SidebarViewComponent(PcStoreDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = new SidebarViewModel
            {
                // 1. Lấy danh mục kèm đếm số lượng linh kiện
                Categories = await _context.Categories
                    .Select(c => new CategoryItemVm
                    {
                        CategoryId = c.CategoryId,
                        CategoryName = c.CategoryName,
                        IconClass = c.IconClass ?? "fas fa-desktop",
                        ProductCount = c.Products.Count
                    }).ToListAsync(),

                // 2. Lấy thương hiệu linh kiện (thay cho Color)
                Brands = await _context.Brands.Take(6).ToListAsync(),

                // 3. Lấy sản phẩm nổi bật
                FeaturedProducts = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Where(p => p.IsFeatured)
                    .Take(3)
                    .ToListAsync()
            };

            return View(model);
        }
    }
}