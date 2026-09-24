using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebBanPC.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace WebBanPC.Controllers
{
    public class HomeController : Controller
    {
        private readonly PcStoreDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(PcStoreDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search,int? categoryId,int? brandId,decimal? maxPrice,string? sortOrder,int? page)
        {
            int pageSize = 6;
            int pageNumber = page ?? 1;

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentBrand = brandId;
            ViewBag.CurrentMaxPrice = maxPrice;
            ViewBag.CurrentSort = sortOrder;

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .AsQueryable();

            // 1. Lọc theo từ khóa tìm kiếm (Tên hoặc Mô tả)
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search) || (p.ShortDescription != null && p.ShortDescription.Contains(search)));
            }

            // 2. Lọc theo Danh mục linh kiện
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // 3. Lọc theo Thương hiệu (Brand)
            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            // 4. Lọc theo Giá tối đa
            if (maxPrice.HasValue && maxPrice > 0)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            // 5. Sắp xếp (Sort By)
            query = sortOrder switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.Name),
                _ => query.OrderByDescending(p => p.CreatedAt) // Mặc định: Mới nhất
            };

            var paginatedProducts = await PaginatedList<Product>.CreateAsync(query.AsNoTracking(), pageNumber, pageSize);

            return View(paginatedProducts);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
