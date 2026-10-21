using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanPC.Models;

namespace WebBanPC.ViewComponents
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly PcStoreDbContext _context;

        public CategoryMenuViewComponent(PcStoreDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Select(c => new CategoryMenuVM
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    ProductCount = c.Products.Count()
                })
                .ToListAsync();

            return View(categories);
        }
    }
}