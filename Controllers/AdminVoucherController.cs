using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanPC.Models;

namespace WebBanPC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminVoucherController : Controller
    {
        private readonly PcStoreDbContext _context;

        public AdminVoucherController(PcStoreDbContext context)
        {
            _context = context;
        }

        // GET: /AdminVoucher
        public async Task<IActionResult> Index()
        {
            var list = await _context.Vouchers.OrderByDescending(v => v.Id).ToListAsync();
            return View(list);
        }

        // GET: /AdminVoucher/Create
        public IActionResult Create()
        {
            return View(new Voucher { EndDate = DateTime.Now.AddDays(30) });
        }

        // POST: /AdminVoucher/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Voucher voucher)
        {
            voucher.Code = voucher.Code.Trim().ToUpper();

            if (await _context.Vouchers.AnyAsync(v => v.Code == voucher.Code))
            {
                ModelState.AddModelError("Code", "Mã voucher này đã tồn tại!");
            }

            if (ModelState.IsValid)
            {
                _context.Vouchers.Add(voucher);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm Voucher mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(voucher);
        }

        // POST: /AdminVoucher/Toggle/5 (Bật/Tắt kích hoạt)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var v = await _context.Vouchers.FindAsync(id);
            if (v != null)
            {
                v.IsActive = !v.IsActive;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /AdminVoucher/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var v = await _context.Vouchers.FindAsync(id);
            if (v != null)
            {
                _context.Vouchers.Remove(v);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa voucher thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}