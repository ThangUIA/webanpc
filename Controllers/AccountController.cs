using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
    }
}