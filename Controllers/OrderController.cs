using Microsoft.AspNetCore.Authorization;
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
        private const string GUEST_CART_COOKIE = "PcStore_GuestCart";


        // Cấu hình tài khoản ngân hàng nhận tiền qua VietQR
        private const string BANK_ID = "MB"; // Mã ngân hàng: MB, VCB, ICB, ACB, VPB...
        private const string ACCOUNT_NO = "0988888888"; // Số tài khoản thụ hưởng
        private const string ACCOUNT_NAME = "NGUYEN VAN A"; // Tên chủ tài khoản không dấu

        public OrderController(PcStoreDbContext context)
        {
            _context = context;
        }

        // Helper lấy UserId của tài khoản đang đăng nhập
        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int uid))
            {
                return uid;
            }
            return null;
        }

        // Helper lấy giỏ hàng tổng quát (nếu đăng nhập thì đọc DB, chưa đăng nhập thì đọc Cookie)
        private async Task<List<CartItem>> GetCurrentCartItemsAsync()
        {
            var userId = GetCurrentUserId();
            if (userId != null)
            {
                var cartData = await _context.Carts
                    .Include(c => c.Product)
                        .ThenInclude(p => p.ProductImages)
                    .Where(c => c.UserId == userId.Value)
                    .ToListAsync();

                return cartData.Select(c => new CartItem
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
            }
            else
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
        }

        // GET: /Order/Checkout
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            var cart = await GetCurrentCartItemsAsync();
            if (!cart.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var vm = new CheckoutVM
            {
                CartItems = cart,
                CustomerName = User.Identity?.Name ?? string.Empty,
                CustomerEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty
            };

            return View(vm);
        }

        // POST: /Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutVM vm)
        {
            var userId = GetCurrentUserId();

            var cart = await GetCurrentCartItemsAsync();
            if (!cart.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            vm.CartItems = cart;

            if (ModelState.IsValid)
            {
                // 1. Tạo bản ghi Order
                var order = new Order
                {
                    UserId = userId.Value,
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

                // 3. Dọn sạch giỏ hàng trong bảng Carts của User này
                var userCartEntries = await _context.Carts
                    .Where(c => c.UserId == userId.Value)
                    .ToListAsync();
                _context.Carts.RemoveRange(userCartEntries);

                await _context.SaveChangesAsync();

                // 4. Chuyển sang trang xuất mã VietQR
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

            string transferContent = $"PC{order.OrderId}";
            string qrUrl = $"https://img.vietqr.io/image/{BANK_ID}-{ACCOUNT_NO}-compact2.png?amount={order.TotalAmount:0}&addInfo={transferContent}&accountName={Uri.EscapeDataString(ACCOUNT_NAME)}";

            ViewBag.QrUrl = qrUrl;
            ViewBag.TransferContent = transferContent;
            ViewBag.AccountNo = ACCOUNT_NO;
            ViewBag.AccountName = ACCOUNT_NAME;
            ViewBag.BankId = BANK_ID;

            return View(order);
        }
        // POST: /Order/ApplyVoucher
        [HttpPost]
        public async Task<IActionResult> ApplyVoucher(string code, decimal subtotal)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá!" });
            }

            code = code.Trim().ToUpper();
            var now = DateTime.Now;

            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Code == code && v.IsActive && v.StartDate <= now && v.EndDate >= now);

            if (voucher == null)
            {
                return Json(new { success = false, message = "Mã voucher không hợp lệ hoặc đã hết hạn!" });
            }

            if (voucher.Quantity <= 0)
            {
                return Json(new { success = false, message = "Mã voucher này đã hết lượt sử dụng!" });
            }

            if (subtotal < voucher.MinOrderAmount)
            {
                return Json(new
                {
                    success = false,
                    message = $"Đơn hàng phải từ {voucher.MinOrderAmount:N0} đ trở lên mới áp dụng được mã này!"
                });
            }

            // Tính số tiền giảm (không giảm quá tổng giá trị đơn)
            decimal discount = Math.Min(voucher.DiscountAmount, subtotal);

            return Json(new
            {
                success = true,
                message = $"Áp dụng mã {voucher.Code} thành công! Giảm {discount:N0} đ",
                discount = discount,
                voucherCode = voucher.Code
            });
        }
    }
}