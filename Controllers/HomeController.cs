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
            int pageSize = 9;
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
                .Include(p => p.ProductReviews)
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

        // GET: /Home/Contact
        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }

        // POST: /Home/Contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(string name, string email, string phone, string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ các trường bắt buộc (*).";
                return View();
            }

            var contactMsg = new ContactMessage
            {
                Name = name.Trim(),
                Email = email.Trim(),
                Phone = phone?.Trim(),
                Subject = subject?.Trim(),
                Message = message.Trim(),
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.ContactMessages.Add(contactMsg);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cảm ơn bạn đã liên hệ! Tin nhắn đã được gửi đến ban quản trị.";
            return RedirectToAction(nameof(Contact));
        }
        // GET: /Home/About
        [HttpGet]
        public IActionResult About()
        {
            return View();
        }

        // GET: /Home/ShippingInfo
        [HttpGet]
        public IActionResult ShippingInfo()
        {
            return View();
        }
        // GET: /Home/PrivacyPolicy
        [HttpGet]
        public IActionResult PrivacyPolicy()
        {
            return View();
        }

        // GET: /Home/TermsOfService
        [HttpGet]
        public IActionResult TermsOfService()
        {
            return View();
        }
        // GET: /Home/WarrantyPolicy
        [HttpGet]
        public IActionResult WarrantyPolicy()
        {
            return View();
        }

        // GET: /Home/Faq
        [HttpGet]
        public IActionResult Faq()
        {
            return View();
        }

        // GET: /Home/PartnerPortal
        [HttpGet]
        public IActionResult PartnerPortal()
        {
            return View();
        }
        // GET: /Home/Vouchers
        [HttpGet]
        public async Task<IActionResult> Vouchers()
        {
            var now = DateTime.Now;

            // Lấy các voucher đang kích hoạt, còn hạn dùng và còn lượt sử dụng
            var vouchers = await _context.Vouchers
                .AsNoTracking()
                .Where(v => v.IsActive && v.StartDate <= now && v.EndDate >= now && v.Quantity > 0)
                .OrderByDescending(v => v.DiscountAmount)
                .ToListAsync();

            return View(vouchers);
        }

    }
}
