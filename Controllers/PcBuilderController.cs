using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanPC.Models;

namespace WebBanPC.Controllers
{
    public class PcBuilderController : Controller
    {
        private readonly PcStoreDbContext _context;

        public PcBuilderController(PcStoreDbContext context)
        {
            _context = context;
        }

        // GET: /PcBuilder
        public IActionResult Index()
        {
            return View();
        }

        // AJAX: Lấy danh sách linh kiện theo từ khóa danh mục (CPU, VGA, RAM...)
        [HttpGet]
        public async Task<IActionResult> GetPartsByCategory(string categoryKeyword, string? search)
        {
            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .Where(p => p.Category.CategoryName.Contains(categoryKeyword) || p.Name.Contains(categoryKeyword));

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search));
            }

            var parts = await query
                .Take(20)
                .Select(p => new
                {
                    id = p.ProductId,
                    name = p.Name,
                    price = p.Price,
                    stock = p.StockQuantity,
                    image = p.ProductImages.FirstOrDefault(img => img.IsDefault).ImageUrl
                            ?? p.ProductImages.FirstOrDefault().ImageUrl
                            ?? "img/product-3.png"
                })
                .ToListAsync();

            return Json(parts);
        }
    }
}