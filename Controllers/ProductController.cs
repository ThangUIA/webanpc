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
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductAttributes)
                    .ThenInclude(pa => pa.Attribute) // Nạp thông số kỹ thuật (Socket, RAM, VRAM...)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // Lấy 4 sản phẩm cùng CategoryId làm Related Products (trừ sản phẩm hiện tại)
            ViewBag.RelatedProducts = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != id)
                .Take(4)
                .ToListAsync();

            return View(product);
        }
    }
}