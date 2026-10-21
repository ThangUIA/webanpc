using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebBanPC.Models;

namespace WebBanPC.Controllers
{
    public class CompareController : Controller
    {
        private readonly PcStoreDbContext _context;
        private const string COMPARE_COOKIE = "PcStore_CompareList";
        private const int MAX_COMPARE_ITEMS = 4; // Tối đa so sánh 4 sản phẩm

        public CompareController(PcStoreDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách ID sản phẩm từ Cookie
        private List<int> GetCompareIds()
        {
            var cookie = Request.Cookies[COMPARE_COOKIE];
            if (string.IsNullOrEmpty(cookie)) return new List<int>();
            try
            {
                return JsonSerializer.Deserialize<List<int>>(cookie) ?? new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }

        // Lưu danh sách ID vào Cookie
        private void SaveCompareIds(List<int> ids)
        {
            var options = new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(7),
                HttpOnly = true,
                IsEssential = true
            };
            Response.Cookies.Append(COMPARE_COOKIE, JsonSerializer.Serialize(ids), options);
        }

        // GET: /Compare (Trang bảng so sánh)
        public async Task<IActionResult> Index()
        {
            var ids = GetCompareIds();
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => ids.Contains(p.ProductId))
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .ToListAsync();

            return View(products);
        }

        // POST: /Compare/Add
        [HttpPost]
        public async Task<IActionResult> Add(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại!" });
            }

            var ids = GetCompareIds();
            if (ids.Contains(productId))
            {
                return Json(new { success = false, message = "Sản phẩm đã có trong danh sách so sánh!", count = ids.Count });
            }

            if (ids.Count >= MAX_COMPARE_ITEMS)
            {
                return Json(new { success = false, message = $"Chỉ có thể so sánh tối đa {MAX_COMPARE_ITEMS} sản phẩm cùng lúc!", count = ids.Count });
            }

            ids.Add(productId);
            SaveCompareIds(ids);

            return Json(new
            {
                success = true,
                message = $"Đã thêm \"{product.Name}\" vào danh sách so sánh!",
                count = ids.Count
            });
        }

        // POST: /Compare/Remove
        [HttpPost]
        public IActionResult Remove(int productId)
        {
            var ids = GetCompareIds();
            if (ids.Contains(productId))
            {
                ids.Remove(productId);
                SaveCompareIds(ids);
            }
            return Json(new { success = true, count = ids.Count });
        }

        // POST: /Compare/ClearAll
        [HttpPost]
        public IActionResult ClearAll()
        {
            Response.Cookies.Delete(COMPARE_COOKIE);
            return RedirectToAction(nameof(Index));
        }
    }
}