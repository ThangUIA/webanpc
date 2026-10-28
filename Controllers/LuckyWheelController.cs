using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanPC.Models;

namespace WebBanPC.Controllers
{
    public class LuckyWheelController : Controller
    {
        private readonly PcStoreDbContext _context;

        public LuckyWheelController(PcStoreDbContext context)
        {
            _context = context;
        }

        // Hàm phụ trợ tìm User an toàn bất kể Claim lưu Email, Name hay ID
        private async Task<User?> GetCurrentUserAsync()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return null;

            // 1. Thử lấy theo NameIdentifier (UserId)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                var userById = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (userById != null) return userById;
            }

            // 2. Thử lấy theo Email claim hoặc Name claim
            var emailOrName = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity.Name;
            if (!string.IsNullOrEmpty(emailOrName))
            {
                return await _context.Users.FirstOrDefaultAsync(u => u.Email == emailOrName || u.FullName == emailOrName);
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();

            ViewBag.IsLoggedIn = user != null;
            ViewBag.Points = user?.RewardPoints ?? 0;
            ViewBag.SpinsAvailable = (user?.RewardPoints ?? 0) / 10;

            // Lấy danh sách giải thưởng để truyền cho Canvas
            var activePrizes = await _context.LuckyWheelPrizes
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            ViewBag.PrizesJson = System.Text.Json.JsonSerializer.Serialize(activePrizes.Select(p => p.PrizeName));
            ViewBag.ColorsJson = System.Text.Json.JsonSerializer.Serialize(activePrizes.Select(p => p.ColorHex));

            List<SpinHistory> history = new List<SpinHistory>();
            if (user != null)
            {
                history = await _context.SpinHistories
                    .Where(h => h.UserId == user.UserId)
                    .OrderByDescending(h => h.CreatedAt)
                    .Take(10)
                    .ToListAsync();
            }

            return View(history);
        }

        // POST: /LuckyWheel/Spin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Spin()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để tham gia quay thưởng!" });
            }

            if (user.RewardPoints < 10)
            {
                return Json(new { success = false, message = "Bạn cần tối thiểu 10 điểm để quay thưởng!" });
            }

            var activePrizes = await _context.LuckyWheelPrizes
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            if (!activePrizes.Any())
            {
                return Json(new { success = false, message = "Vòng quay hiện đang bảo trì phần thưởng!" });
            }

            // 1. Trừ 10 điểm của khách
            user.RewardPoints -= 10;

            // 2. Thuật toán chọn quà theo trọng số (Probability)
            int totalWeight = activePrizes.Sum(p => p.Probability);
            var rand = new Random();
            int randomValue = rand.Next(1, totalWeight + 1);
            int currentSum = 0;
            int winningIndex = 0;

            for (int i = 0; i < activePrizes.Count; i++)
            {
                currentSum += activePrizes[i].Probability;
                if (randomValue <= currentSum)
                {
                    winningIndex = i;
                    break;
                }
            }

            var wonPrize = activePrizes[winningIndex];
            string? giftCode = null;
            string detailMessage = "";

            // 3. Xử lý nghiệp vụ theo từng loại quà:
            if (wonPrize.PrizeName.Contains("Voucher", StringComparison.OrdinalIgnoreCase))
            {
                // Tách hoặc tìm voucher trong database để cấp mã
                // Ví dụ: Tạo một mã Voucher độc quyền riêng cho user này dùng 1 lần
                giftCode = "LUCKY" + wonPrize.PrizeId + "_" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

                // Thêm trực tiếp vào bảng Vouchers nếu bạn muốn khách nhập mã được ngay
                var voucher = new Voucher
                {
                    Code = giftCode,
                    DiscountAmount = wonPrize.PrizeName.Contains("50K") ? 50000 :
                                     wonPrize.PrizeName.Contains("100K") ? 100000 :
                                     wonPrize.PrizeName.Contains("200K") ? 200000 : 50000,
                    Quantity = 1,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(30),
                    IsActive = true
                };
                _context.Vouchers.Add(voucher);

                detailMessage = $"Mã Voucher của bạn: {giftCode} (Hạn dùng 30 ngày)";
            }
            else if (wonPrize.PrizeName.Contains("May Mắn", StringComparison.OrdinalIgnoreCase))
            {
                detailMessage = "Chúc bạn may mắn ở những lượt quay tiếp theo!";
            }
            else
            {
                // Quà hiện vật (Chuột, Tai nghe, Áo thun...)
                detailMessage = "TCSHOP sẽ liên hệ số điện thoại của bạn để gửi quà tặng tận nơi!";
            }

            // 4. Lưu lại lịch sử trúng thưởng
            var historyItem = new SpinHistory
            {
                UserId = user.UserId,
                PrizeName = wonPrize.PrizeName,
                GiftCode = giftCode,
                IsClaimed = false,
                CreatedAt = DateTime.Now
            };
            _context.SpinHistories.Add(historyItem);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                prizeIndex = winningIndex,
                prizeName = wonPrize.PrizeName,
                giftCode = giftCode,
                detailMessage = detailMessage,
                remainingPoints = user.RewardPoints,
                spinsAvailable = user.RewardPoints / 10
            });
        }
    }
}