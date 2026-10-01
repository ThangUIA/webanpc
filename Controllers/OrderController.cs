using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using WebBanPC.Models;

namespace WebBanPC.Controllers
{
    public class OrderController : Controller
    {
        private readonly PcStoreDbContext _context;
        private const string CART_KEY = "MyCartSession";

        // Cấu hình tài khoản ngân hàng nhận tiền qua VietQR
        private const string BANK_ID = "MB"; // Mã ngân hàng: MB, VCB, ICB, ACB, VPB...
        private const string ACCOUNT_NO = "0988888888"; // Số tài khoản thụ hưởng của bạn
        private const string ACCOUNT_NAME = "NGUYEN VAN A"; // Tên chủ tài khoản không dấu

        public OrderController(PcStoreDbContext context)
        {
            _context = context;
        }

        private List<CartItem> GetCartItems()
        {
            var sessionData = HttpContext.Session.GetString(CART_KEY);
            return string.IsNullOrEmpty(sessionData)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(sessionData) ?? new List<CartItem>();
        }

        // GET: /Order/Checkout
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = GetCartItems();
            if (!cart.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var vm = new CheckoutVM
            {
                CartItems = cart
            };

            // Nếu người dùng đã đăng nhập, tự điền sẵn tên/email nếu có
            if (User.Identity?.IsAuthenticated == true)
            {
                vm.CustomerName = User.Identity.Name ?? string.Empty;
                var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
                if (!string.IsNullOrEmpty(emailClaim)) vm.CustomerEmail = emailClaim;
            }

            return View(vm);
        }

        // POST: /Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutVM vm)
        {
            var cart = GetCartItems();
            if (!cart.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            vm.CartItems = cart;

            if (ModelState.IsValid)
            {
                // Lấy UserId nếu đã đăng nhập
                int? currentUserId = null;
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idClaim, out int uid))
                {
                    currentUserId = uid;
                }

                // 1. Tạo bản ghi Order
                var order = new Order
                {
                    UserId = currentUserId,
                    CustomerName = vm.CustomerName,
                    CustomerPhone = vm.CustomerPhone,
                    CustomerEmail = vm.CustomerEmail ?? string.Empty,
                    ShippingAddress = vm.ShippingAddress,
                    Note = vm.Note,
                    TotalAmount = vm.TotalAmount,
                    OrderStatus = 1, // 1: Chờ xác nhận / Chờ thanh toán
                    OrderDate = DateTime.Now
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // 2. Lưu chi tiết sản phẩm vào OrderDetails
                foreach (var item in cart)
                {
                    var detail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price
                    };
                    _context.OrderDetails.Add(detail);
                }

                await _context.SaveChangesAsync();

                // 3. Xóa giỏ hàng sau khi đặt thành công
                HttpContext.Session.Remove(CART_KEY);

                // Chuyển hướng sang trang hiển thị mã QR thanh toán
                return RedirectToAction(nameof(Payment), new { id = order.OrderId });
            }

            return View(vm);
        }

        // GET: /Order/Payment/{id}
        public async Task<IActionResult> Payment(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();

            // Cú pháp nội dung chuyển khoản: PC<Mã Đơn>
            string transferContent = $"PC{order.OrderId}";

            // Tạo link ảnh VietQR động dạng QuickLink
            string qrUrl = $"https://img.vietqr.io/image/{BANK_ID}-{ACCOUNT_NO}-compact2.png?amount={order.TotalAmount:0}&addInfo={transferContent}&accountName={Uri.EscapeDataString(ACCOUNT_NAME)}";

            ViewBag.QrUrl = qrUrl;
            ViewBag.TransferContent = transferContent;
            ViewBag.AccountNo = ACCOUNT_NO;
            ViewBag.AccountName = ACCOUNT_NAME;
            ViewBag.BankId = BANK_ID;

            return View(order);
        }
    }
}