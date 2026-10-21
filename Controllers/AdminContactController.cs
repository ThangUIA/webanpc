using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanPC.Models;

namespace WebBanPC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminContactController : Controller
    {
        private readonly PcStoreDbContext _context;

        public AdminContactController(PcStoreDbContext context)
        {
            _context = context;
        }

        // GET: /AdminContact (Danh sách tin nhắn)
        [HttpGet]
        public async Task<IActionResult> Index(bool? unreadOnly, int page = 1)
        {
            int pageSize = 10;
            var query = _context.ContactMessages.AsNoTracking().AsQueryable();

            if (unreadOnly.HasValue && unreadOnly.Value)
            {
                query = query.Where(m => !m.IsRead);
            }

            int total = await query.CountAsync();
            var messages = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.UnreadOnly = unreadOnly;

            return View(messages);
        }

        // GET: /AdminContact/Details/5 (Đọc chi tiết tin nhắn)
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg == null) return NotFound();

            // Đánh dấu là đã đọc khi bấm vào xem
            if (!msg.IsRead)
            {
                msg.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return View(msg);
        }

        // POST: /AdminContact/Delete/5 (Xóa tin nhắn)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg != null)
            {
                _context.ContactMessages.Remove(msg);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa tin nhắn thành công!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}