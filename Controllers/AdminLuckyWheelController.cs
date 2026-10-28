using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanPC.Models;

namespace WebBanPC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminLuckyWheelController : Controller
    {
        private readonly PcStoreDbContext _context;

        public AdminLuckyWheelController(PcStoreDbContext context)
        {
            _context = context;
        }

        // GET: /AdminLuckyWheel
        public async Task<IActionResult> Index()
        {
            var prizes = await _context.LuckyWheelPrizes
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            // 1. Lấy danh sách Voucher còn hiệu lực
            ViewBag.Vouchers = await _context.Vouchers
                .AsNoTracking()
                .Where(v => v.IsActive && v.Quantity > 0)
                .Select(v => new {
                    v.Id,
                    v.Code,
                    DisplayText = $"{v.Code} - Giảm {v.DiscountAmount:N0} đ"
                })
                .ToListAsync();

            // 2. Lấy danh sách Sản phẩm đang kinh doanh
            ViewBag.Products = await _context.Products
                .AsNoTracking()
                .Where(p => p.StockQuantity>0)
                .OrderBy(p => p.Name)
                .Select(p => new {
                    p.ProductId,
                    p.Name
                })
                .ToListAsync();

            return View(prizes);
        }

        // POST: /AdminLuckyWheel/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LuckyWheelPrize prize)
        {
            if (ModelState.IsValid)
            {
                prize.CreatedAt = DateTime.Now;
                _context.LuckyWheelPrizes.Add(prize);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm phần thưởng mới thành công!";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Dữ liệu không hợp lệ!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /AdminLuckyWheel/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LuckyWheelPrize model)
        {
            var prize = await _context.LuckyWheelPrizes.FindAsync(model.PrizeId);
            if (prize == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phần thưởng!";
                return RedirectToAction(nameof(Index));
            }

            prize.PrizeName = model.PrizeName.Trim();
            prize.ColorHex = model.ColorHex;
            prize.Probability = model.Probability;
            prize.DisplayOrder = model.DisplayOrder;
            prize.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật phần thưởng thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /AdminLuckyWheel/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var prize = await _context.LuckyWheelPrizes.FindAsync(id);
            if (prize != null)
            {
                _context.LuckyWheelPrizes.Remove(prize);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa phần thưởng!";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /AdminLuckyWheel/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var prize = await _context.LuckyWheelPrizes.FindAsync(id);
            if (prize != null)
            {
                prize.IsActive = !prize.IsActive;
                await _context.SaveChangesAsync();
                return Json(new { success = true, isActive = prize.IsActive });
            }
            return Json(new { success = false });
        }
        // GET: /AdminLuckyWheel/History
        public async Task<IActionResult> History()
        {
            var histories = await _context.SpinHistories
                .Include(h => h.User)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();

            return View(histories);
        }

        // POST: /AdminLuckyWheel/MarkClaimed/5
        [HttpPost]
        public async Task<IActionResult> MarkClaimed(int id)
        {
            var item = await _context.SpinHistories.FindAsync(id);
            if (item != null)
            {
                item.IsClaimed = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(History));
        }
    }
}