using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using WebBanPC.Models;

namespace WebBanPC.Controllers
{
    public class WishlistController : Controller
    {
        private readonly PcStoreDbContext _context;
        public const string GUEST_WISHLIST_COOKIE = "PcStore_GuestWishlist";

        public WishlistController(PcStoreDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int uid) ? uid : null;
        }

        // Lấy danh sách ID sản phẩm từ Cookie cho khách vãng lai
        private List<int> GetGuestWishlist()
        {
            var cookieData = Request.Cookies[GUEST_WISHLIST_COOKIE];
            if (string.IsNullOrEmpty(cookieData)) return new List<int>();
            try
            {
                return JsonSerializer.Deserialize<List<int>>(cookieData) ?? new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }

        private void SaveGuestWishlist(List<int> list)
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(30),
                HttpOnly = true,
                IsEssential = true
            };
            Response.Cookies.Append(GUEST_WISHLIST_COOKIE, JsonSerializer.Serialize(list), cookieOptions);
        }

        // GET: /Wishlist (Trang danh sách yêu thích)
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            List<Product> products;

            if (userId != null)
            {
                products = await _context.Wishlists
                    .Where(w => w.UserId == userId.Value)
                    .Include(w => w.Product)
                        .ThenInclude(p => p.ProductImages)
                    .Include(w => w.Product)
                        .ThenInclude(p => p.Category)
                    .Select(w => w.Product!)
                    .ToListAsync();
            }
            else
            {
                var productIds = GetGuestWishlist();
                products = await _context.Products
                    .Where(p => productIds.Contains(p.ProductId))
                    .Include(p => p.ProductImages)
                    .Include(p => p.Category)
                    .ToListAsync();
            }

            return View(products);
        }

        // POST: /Wishlist/ToggleWishlist (AJAX khi bấm vào icon trái tim)
        [HttpPost]
        public async Task<IActionResult> ToggleWishlist(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại!" });
            }

            var userId = GetCurrentUserId();
            bool isAdded;
            int totalCount;

            if (userId != null)
            {
                var existing = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserId == userId.Value && w.ProductId == productId);

                if (existing != null)
                {
                    _context.Wishlists.Remove(existing);
                    isAdded = false;
                }
                else
                {
                    _context.Wishlists.Add(new Wishlist
                    {
                        UserId = userId.Value,
                        ProductId = productId,
                        CreatedAt = DateTime.Now
                    });
                    isAdded = true;
                }

                await _context.SaveChangesAsync();
                totalCount = await _context.Wishlists.CountAsync(w => w.UserId == userId.Value);
            }
            else
            {
                var list = GetGuestWishlist();
                if (list.Contains(productId))
                {
                    list.Remove(productId);
                    isAdded = false;
                }
                else
                {
                    list.Add(productId);
                    isAdded = true;
                }

                SaveGuestWishlist(list);
                totalCount = list.Count;
            }

            return Json(new
            {
                success = true,
                isAdded = isAdded,
                message = isAdded ? $"Đã thêm \"{product.Name}\" vào danh sách yêu thích!" : $"Đã bỏ \"{product.Name}\" khỏi danh sách yêu thích!",
                count = totalCount
            });
        }
    }
}