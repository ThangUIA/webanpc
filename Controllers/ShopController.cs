using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanPC.Models;

namespace WebBanPC.Controllers
{
    public class ShopController : Controller
    {
        private readonly PcStoreDbContext _context;

        public ShopController(PcStoreDbContext context)
        {
            _context = context;
        }

        // Action này bắt buộc phải là public và trả về Task<IActionResult> hoặc IActionResult
        [HttpGet]
        public async Task<IActionResult> Index(int? categoryId, string? search, int page = 1)
        {
            int pageSize = 9;
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include (p => p.ProductReviews)
                .AsQueryable();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(p => p.Name.Contains(search) || (p.ShortDescription != null && p.ShortDescription.Contains(search)));
            }

            int totalItems = await query.CountAsync();
            var products = await query
                .OrderByDescending(p => p.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(products);
        }
        // GET: /Shop/Bestseller?page=1
        [HttpGet]
        public async Task<IActionResult> Bestseller(int page = 1)
        {
            int pageSize = 8; // Số sản phẩm hiển thị trên 1 trang

            // 1. Lấy danh sách ID bán chạy từ OrderDetails
            var bestSellerProductIds = await _context.OrderDetails
                .AsNoTracking()
                .GroupBy(od => od.ProductId)
                .OrderByDescending(g => g.Sum(od => od.Quantity))
                .Select(g => g.Key)
                .ToListAsync();

            IQueryable<Product> query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductReviews);

            List<Product> allProducts;

            if (bestSellerProductIds.Any())
            {
                var rawList = await query
                    .Where(p => bestSellerProductIds.Contains(p.ProductId))
                    .ToListAsync();

                allProducts = rawList.OrderBy(p => bestSellerProductIds.IndexOf(p.ProductId)).ToList();
            }
            else
            {
                allProducts = await query.ToListAsync();
            }

            // 2. Tính toán phân trang
            int totalItems = allProducts.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Cắt danh sách theo trang hiện tại
            var pagedProducts = allProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(pagedProducts);
        }
    }
}