using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanPC.Models;

namespace WebBanPC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminOrderController : Controller
    {
        private readonly PcStoreDbContext _context;

        public AdminOrderController(PcStoreDbContext context)
        {
            _context = context;
        }

        // GET: /AdminOrder (Danh sách đơn hàng + Bộ lọc)
        [HttpGet]
        public async Task<IActionResult> Index(string? search, int? status, int page = 1)
        {
            int pageSize = 10;
            var query = _context.Orders.AsNoTracking().AsQueryable();

            // Tìm kiếm theo Mã đơn, Tên khách hoặc SĐT
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(o => o.OrderId.ToString().Contains(search)
                                      || (o.CustomerName != null && o.CustomerName.Contains(search))
                                      || (o.CustomerPhone != null && o.CustomerPhone.Contains(search)));
            }

            // Lọc theo trạng thái
            if (status.HasValue && status.Value > 0)
            {
                query = query.Where(o => o.OrderStatus == status.Value);
            }

            int totalOrders = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);

            return View(orders);
        }

        // GET: /AdminOrder/Details/5 (Xem chi tiết đơn hàng)
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // POST: /AdminOrder/UpdateStatus (Cập nhật trạng thái đơn hàng)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, int newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(Index));
            }
            // Khi Admin chuyển đơn sang trạng thái Hoàn thành (ví dụ OrderStatus == 4)
            if (order.OrderStatus == 4 && !order.IsPointsAwarded && order.UserId.HasValue)
            {
                // Kiểm tra đơn hàng có từ 10 triệu trở lên không
                if (order.TotalAmount >= 10000000)
                {
                    var user = await _context.Users.FindAsync(order.UserId.Value);
                    if (user != null)
                    {
                        // Kiểm tra xem đây có phải là đơn hàng hoàn thành đầu tiên của khách không
                        bool isFirstCompletedOrder = !await _context.Orders
                            .AnyAsync(o => o.UserId == user.UserId && o.OrderId != order.OrderId && o.OrderStatus == 4);

                        int pointsToAdd = isFirstCompletedOrder ? 5 : 1;
                        user.RewardPoints += pointsToAdd;
                        order.IsPointsAwarded = true; // Đánh dấu đã cộng điểm
                    }
                }
            }

            order.OrderStatus = newStatus;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn hàng #{orderId} thành công!";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }
    }
}