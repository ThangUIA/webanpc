using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanPC.Models;

namespace WebBanPC.Controllers
{
    public class ProductController : Controller
    {
        private readonly PcStoreDbContext _context;

        public ProductController(PcStoreDbContext context)
        {
            _context = context;
        }

        // GET: /Product/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // Lấy 4 sản phẩm cùng danh mục để hiển thị ở mục Sản phẩm liên quan
            var relatedProducts = await _context.Products
                .AsNoTracking()
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != product.ProductId)
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .Take(4)
                .ToListAsync();
            // Lấy danh sách review đã duyệt của sản phẩm này
            var reviews = await _context.ProductReviews
                .Include(r => r.User)
                .Where(r => r.ProductId == id && r.IsActive)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Reviews = reviews;
            // Tính điểm sao trung bình
            ViewBag.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 5.0;
            ViewBag.TotalReviews = reviews.Count;

            var vm = new ProductDetailVM
            {
                Product = product,
                RelatedProducts = relatedProducts
            };

            return View(vm);
        }
        // POST: Product/AddReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int productId, int rating, string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
            {
                return Json(new { success = false, message = "Vui lòng nhập nội dung đánh giá!" });
            }

            var review = new ProductReview
            {
                ProductId = productId,
                Rating = Math.Clamp(rating, 1, 5),
                Comment = comment.Trim(),
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            // Kiểm tra xem người dùng đã đăng nhập hay chưa
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // Lấy email hoặc ClaimTypes.NameIdentifier
                var userEmail = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user != null)
                {
                    review.UserId = user.UserId;
                    review.GuestName = null;
                }
                else
                {
                    review.GuestName = User.Identity.Name ?? "Người dùng";
                }
            }
            else
            {
                // Chưa đăng nhập -> gán là Khách vãng lai
                review.UserId = null;
                review.GuestName = "Khách vãng lai";
            }

            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

            // Trả về dữ liệu để hiển thị ngay lập tức lên web
            string reviewerName = review.UserId.HasValue
                ? (await _context.Users.FindAsync(review.UserId))?.FullName ?? "Thành viên"
                : review.GuestName!;

            return Json(new
            {
                success = true,
                message = "Gửi đánh giá thành công!",
                reviewerName = reviewerName,
                rating = review.Rating,
                comment = review.Comment,
                date = review.CreatedAt?.ToString("dd/MM/yyyy HH:mm")
            });
        }
    }
}