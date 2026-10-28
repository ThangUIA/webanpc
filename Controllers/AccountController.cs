using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using WebBanPC.Models;

namespace WebBanPC.Controllers
{
    public class AccountController : Controller
    {
        private readonly PcStoreDbContext _context;
        private const string GUEST_CART_COOKIE = "PcStore_GuestCart";
        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int uid) ? uid : null;
        }
        public AccountController(PcStoreDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == model.Email.Trim().ToLower());
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                return View(model);
            }

            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
            int defaultRoleId = customerRole != null ? customerRole.RoleId : 2;

            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim().ToLower(),
                PhoneNumber = model.PhoneNumber.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                RoleId = defaultRoleId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.Trim().ToLower());

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
                return View(model);
            }

            string roleName = user.Role?.RoleName ?? "Customer";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                authProperties);

            // GỘP GIỎ HÀNG TỪ COOKIE VÀO CSDL CHO TÀI KHOẢN VỪA ĐĂNG NHẬP
            await MigrateGuestCartToDbAsync(user.UserId);

            await MigrateGuestWishlistToDbAsync(user.UserId);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // Hàm chuyển sản phẩm từ Cookie vào Database sau khi đăng nhập
        private async Task MigrateGuestCartToDbAsync(int userId)
        {
            // 1. Đọc giỏ hàng vãng lai từ Cookie
            var cookieData = Request.Cookies[GUEST_CART_COOKIE];
            if (string.IsNullOrEmpty(cookieData)) return;

            List<CartItem>? guestCart = null;
            try
            {
                guestCart = JsonSerializer.Deserialize<List<CartItem>>(cookieData);
            }
            catch
            {
                return;
            }

            if (guestCart == null || !guestCart.Any()) return;

            // 2. Lấy giỏ hàng hiện có trong Database của tài khoản này
            var userDbCart = await _context.Carts
                .Where(c => c.UserId == userId)
                .ToListAsync();

            // 3. Duyệt từng món từ Cookie để cộng dồn hoặc thêm mới vào DB
            foreach (var guestItem in guestCart)
            {
                var existingItem = userDbCart.FirstOrDefault(c => c.ProductId == guestItem.ProductId);

                if (existingItem != null)
                {
                    existingItem.Quantity += guestItem.Quantity;
                }
                else
                {
                    _context.Carts.Add(new Cart
                    {
                        UserId = userId,
                        ProductId = guestItem.ProductId,
                        Quantity = guestItem.Quantity,
                        CreatedAt = DateTime.Now
                    });
                }
            }


            // 4. Lưu vào SQL Server
            await _context.SaveChangesAsync();

            // 5. Xóa Cookie của khách vãng lai
            Response.Cookies.Delete(GUEST_CART_COOKIE);
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return NotFound();

            var vm = new UserProfileVM
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                RewardPoints = user.RewardPoints
            };

            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UserProfileVM vm)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return NotFound();

            // Giữ nguyên email không cho đổi
            vm.Email = user.Email;

            if (ModelState.IsValid)
            {
                // Kiểm tra nếu có yêu cầu đổi mật khẩu
                if (!string.IsNullOrWhiteSpace(vm.NewPassword))
                {
                    if (string.IsNullOrWhiteSpace(vm.CurrentPassword) || !BCrypt.Net.BCrypt.Verify(vm.CurrentPassword, user.PasswordHash))
                    {
                        ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không chính xác.");
                        return View(vm);
                    }

                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.NewPassword);
                }

                user.FullName = vm.FullName.Trim();
                user.PhoneNumber = vm.PhoneNumber.Trim();

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thông tin tài khoản thành công!";
                return RedirectToAction(nameof(Profile));
            }

            return View(vm);
        }

        // ==========================================
        // 2. ĐƠN HÀNG CỦA TÔI
        // ==========================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.ProductImages)
                .Where(o => o.UserId == userId.Value)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ==========================================
        // 3. CHI TIẾT ĐƠN HÀNG
        // ==========================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId.Value);

            if (order == null) return NotFound();

            return View(order);
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            // Tìm đơn hàng của đúng User này
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId.Value);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng cần hủy.";
                return RedirectToAction(nameof(MyOrders));
            }

            // Chỉ cho phép hủy khi đơn còn ở trạng thái 1 (Chờ xác nhận)
            if (order.OrderStatus != 1)
            {
                TempData["ErrorMessage"] = "Đơn hàng đang giao hoặc đã hoàn tất nên không thể hủy!";
                return RedirectToAction(nameof(MyOrders));
            }

            // Đổi trạng thái sang 4 (Đã hủy)
            order.OrderStatus = 4;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã hủy thành công đơn hàng #{order.OrderId}!";
            return RedirectToAction(nameof(MyOrders));
        }
        private async Task MigrateGuestWishlistToDbAsync(int userId)
        {
            var cookieData = Request.Cookies["PcStore_GuestWishlist"];
            if (string.IsNullOrEmpty(cookieData)) return;

            List<int>? guestList = null;
            try
            {
                guestList = JsonSerializer.Deserialize<List<int>>(cookieData);
            }
            catch { return; }

            if (guestList == null || !guestList.Any()) return;

            var userExistingIds = await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Select(w => w.ProductId)
                .ToListAsync();

            foreach (var pid in guestList)
            {
                if (!userExistingIds.Contains(pid))
                {
                    _context.Wishlists.Add(new Wishlist
                    {
                        UserId = userId,
                        ProductId = pid,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
            Response.Cookies.Delete("PcStore_GuestWishlist");
        }
    }
}