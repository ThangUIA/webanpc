using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanPC.Models;

namespace WebBanPC.Controllers
{
    public class ProductController : Controller
    {
        private readonly PcStoreDbContext _context;

        public ProductController(PcStoreDbContext context)
        {
            _context = context;
        }

        // GET: /Product/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // Lấy 4 sản phẩm cùng danh mục để hiển thị ở mục Sản phẩm liên quan
            var relatedProducts = await _context.Products
                .AsNoTracking()
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != product.ProductId)
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .Take(4)
                .ToListAsync();

            var vm = new ProductDetailVM
            {
                Product = product,
                RelatedProducts = relatedProducts
            };

            return View(vm);
        }
    }
}