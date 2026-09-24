using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebBanPC.Models;

namespace WebBanPC.Controllers
{
    public class CartController : Controller
    {
        private readonly PcStoreDbContext _context;
        private const string CART_KEY = "MyCartSession";

        public CartController(PcStoreDbContext context)
        {
            _context = context;
        }

        // Helper lấy giỏ hàng từ Session
        private List<CartItem> GetCartItems()
        {
            var sessionData = HttpContext.Session.GetString(CART_KEY);
            if (string.IsNullOrEmpty(sessionData))
            {
                return new List<CartItem>();
            }
            return JsonSerializer.Deserialize<List<CartItem>>(sessionData) ?? new List<CartItem>();
        }

        // Helper lưu giỏ hàng vào Session
        private void SaveCartSession(List<CartItem> cart)
        {
            HttpContext.Session.SetString(CART_KEY, JsonSerializer.Serialize(cart));
        }

        // GET: /Cart
        public IActionResult Index()
        {
            var cart = GetCartItems();
            return View(cart);
        }

        // POST: /Cart/AddToCart
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

            var cart = GetCartItems();
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
                    Sku = product.Sku, // hoặc string.Empty
                    Price = product.Price,
                    Quantity = quantity,
                    ImageUrl = defaultImage
                });
            }

            SaveCartSession(cart);

            // Tính tổng số lượng hàng đang có trong giỏ
            int totalCount = cart.Sum(c => c.Quantity);

            // Trả về JSON cho Ajax
            return Json(new
            {
                success = true,
                message = $"Đã thêm \"{product.Name}\" vào giỏ hàng!",
                cartCount = totalCount
            });
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int change)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
            {
                item.Quantity += change;
                if (item.Quantity <= 0)
                {
                    cart.Remove(item);
                }
            }

            SaveCartSession(cart);
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/RemoveItem
        [HttpPost]
        public IActionResult RemoveItem(int productId)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
            {
                cart.Remove(item);
            }

            SaveCartSession(cart);
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Clear
        [HttpPost]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CART_KEY);
            return RedirectToAction(nameof(Index));
        }
    }
}