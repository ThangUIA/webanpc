using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using WebBanPC.Models;

namespace WebBanPC.Controllers
{
    public class CartController : Controller
    {
        private readonly PcStoreDbContext _context;
        private const string GUEST_CART_COOKIE = "PcStore_GuestCart";

        public CartController(PcStoreDbContext context)
        {
            _context = context;
        }

        // Helper: Lấy UserId nếu đã đăng nhập
        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int uid))
            {
                return uid;
            }
            return null;
        }

        // Helper: Đọc giỏ hàng của khách vãng lai từ Cookie
        private List<CartItem> GetGuestCart()
        {
            var cookieData = Request.Cookies[GUEST_CART_COOKIE];
            if (string.IsNullOrEmpty(cookieData)) return new List<CartItem>();
            try
            {
                return JsonSerializer.Deserialize<List<CartItem>>(cookieData) ?? new List<CartItem>();
            }
            catch
            {
                return new List<CartItem>();
            }
        }

        // Helper: Lưu giỏ hàng của khách vãng lai vào Cookie (30 ngày)
        private void SaveGuestCart(List<CartItem> cart)
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(30),
                HttpOnly = true,
                IsEssential = true
            };
            Response.Cookies.Append(GUEST_CART_COOKIE, JsonSerializer.Serialize(cart), cookieOptions);
        }

        // GET: /Cart
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            if (userId != null)
            {
                // Người dùng đã đăng nhập: Đọc từ Database
                var cartData = await _context.Carts
                    .Include(c => c.Product)
                        .ThenInclude(p => p.ProductImages)
                    .Where(c => c.UserId == userId.Value)
                    .ToListAsync();

                var cartItems = cartData.Select(c => new CartItem
                {
                    ProductId = c.ProductId,
                    Name = c.Product?.Name ?? string.Empty,
                    Sku = c.Product?.Sku ?? string.Empty,
                    Price = c.Product?.Price ?? 0,
                    Quantity = c.Quantity,
                    ImageUrl = c.Product?.ProductImages.FirstOrDefault(i => i.IsDefault)?.ImageUrl
                               ?? c.Product?.ProductImages.FirstOrDefault()?.ImageUrl
                               ?? "img/products/default.png"
                }).ToList();

                return View(cartItems);
            }
            else
            {
                // Khách vãng lai: Đọc từ Cookie
                return View(GetGuestCart());
            }
        }

        // POST: /Cart/AddToCart (Hỗ trợ cả khách vãng lai lẫn đã đăng nhập)
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại!" });
            }

            var userId = GetCurrentUserId();
            int totalCount = 0;

            if (userId != null)
            {
                // 1. Lưu vào Database cho thành viên
                var existingCartItem = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value && c.ProductId == productId);

                if (existingCartItem != null)
                {
                    existingCartItem.Quantity += quantity;
                }
                else
                {
                    var newCart = new Cart
                    {
                        UserId = userId.Value,
                        ProductId = productId,
                        Quantity = quantity,
                        CreatedAt = DateTime.Now
                    };
                    _context.Carts.Add(newCart);
                }

                await _context.SaveChangesAsync();

                totalCount = await _context.Carts
                    .Where(c => c.UserId == userId.Value)
                    .SumAsync(c => c.Quantity);
            }
            else
            {
                // 2. Lưu vào Cookie cho khách vãng lai
                var cart = GetGuestCart();
                var item = cart.FirstOrDefault(c => c.ProductId == productId);

                if (item != null)
                {
                    item.Quantity += quantity;
                }
                else
                {
                    var defaultImage = product.ProductImages.FirstOrDefault(i => i.IsDefault)?.ImageUrl
                                       ?? product.ProductImages.FirstOrDefault()?.ImageUrl
                                       ?? "img/products/default.png";

                    cart.Add(new CartItem
                    {
                        ProductId = product.ProductId,
                        Name = product.Name,
                        Sku = product.Sku ?? string.Empty,
                        Price = product.Price,
                        Quantity = quantity,
                        ImageUrl = defaultImage
                    });
                }

                SaveGuestCart(cart);
                totalCount = cart.Sum(c => c.Quantity);
            }

            return Json(new
            {
                success = true,
                message = $"Đã thêm \"{product.Name}\" vào giỏ hàng!",
                cartCount = totalCount
            });
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int change)
        {
            var userId = GetCurrentUserId();

            if (userId != null)
            {
                var cartItem = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value && c.ProductId == productId);

                if (cartItem != null)
                {
                    cartItem.Quantity += change;
                    if (cartItem.Quantity <= 0)
                    {
                        _context.Carts.Remove(cartItem);
                    }
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var cart = GetGuestCart();
                var item = cart.FirstOrDefault(c => c.ProductId == productId);
                if (item != null)
                {
                    item.Quantity += change;
                    if (item.Quantity <= 0)
                    {
                        cart.Remove(item);
                    }
                    SaveGuestCart(cart);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/RemoveItem
        [HttpPost]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            var userId = GetCurrentUserId();

            if (userId != null)
            {
                var cartItem = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value && c.ProductId == productId);

                if (cartItem != null)
                {
                    _context.Carts.Remove(cartItem);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var cart = GetGuestCart();
                var item = cart.FirstOrDefault(c => c.ProductId == productId);
                if (item != null)
                {
                    cart.Remove(item);
                    SaveGuestCart(cart);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Clear
        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            var userId = GetCurrentUserId();

            if (userId != null)
            {
                var items = await _context.Carts.Where(c => c.UserId == userId.Value).ToListAsync();
                _context.Carts.RemoveRange(items);
                await _context.SaveChangesAsync();
            }
            else
            {
                Response.Cookies.Delete(GUEST_CART_COOKIE);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}