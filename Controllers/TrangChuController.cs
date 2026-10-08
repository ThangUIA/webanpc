using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanPC.Models;

namespace WebBanPC.Controllers
{
    public class TrangChuController : Controller
    {
        private readonly PcStoreDbContext _context;

        public TrangChuController(PcStoreDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var baseQuery = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .AsNoTracking();
            var activeBanners = await _context.Banners
                .Where(b => b.IsActive)
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();

            var viewModel = new HomeIndexVM
            {
                // 1. Banner động
                MainBanners = activeBanners.Where(b => b.Position == "MainCarousel").ToList(),
                RightBanner = activeBanners.FirstOrDefault(b => b.Position == "HeaderRight"),

                // 2. Sản phẩm hiển thị theo thuộc tính do Admin tích chọn
                AllProducts = await baseQuery.OrderByDescending(p => p.ProductId).Take(8).ToListAsync(),
                NewArrivals = await baseQuery.Where(p => p.IsNew).OrderByDescending(p => p.CreatedAt).Take(8).ToListAsync(),
                FeaturedProducts = await baseQuery.Where(p => p.IsFeatured).OrderByDescending(p => p.ProductId).Take(8).ToListAsync(),
                BestSellers = await baseQuery.OrderByDescending(p => p.StockQuantity).Take(6).ToListAsync()
            };

            return View(viewModel);
        }
    }
}