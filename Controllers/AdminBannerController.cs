using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanPC.Models;

namespace WebBanPC.Controllers
{
    [Authorize(Roles = "Admin")] // Chỉ Admin mới được vào
    public class AdminBannerController : Controller
    {
        private readonly PcStoreDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminBannerController(PcStoreDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Helper upload ảnh banner
        private async Task<string?> UploadBannerImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            string uploadFolder = Path.Combine(_env.WebRootPath, "img", "banners");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

            string ext = Path.GetExtension(file.FileName).ToLower();
            string uniqueName = $"banner_{Guid.NewGuid():N}_{DateTime.Now.Ticks}{ext}";
            string fullPath = Path.Combine(uploadFolder, uniqueName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"img/banners/{uniqueName}";
        }

        // GET: /AdminBanner
        public async Task<IActionResult> Index()
        {
            var banners = await _context.Banners
                .OrderBy(b => b.Position)
                .ThenBy(b => b.DisplayOrder)
                .ToListAsync();
            return View(banners);
        }

        // GET: /AdminBanner/Create
        public IActionResult Create() => View(new BannerCrudVM());

        // POST: /AdminBanner/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BannerCrudVM vm)
        {
            if (vm.ImageFile == null || vm.ImageFile.Length == 0)
            {
                ModelState.AddModelError("ImageFile", "Vui lòng chọn hình ảnh cho banner!");
            }

            if (ModelState.IsValid)
            {
                string? imgPath = await UploadBannerImageAsync(vm.ImageFile);

                var banner = new Banner
                {
                    Title = vm.Title,
                    Subtitle = vm.Subtitle,
                    LinkUrl = vm.LinkUrl ?? "/Home/Index",
                    Position = vm.Position,
                    DisplayOrder = vm.DisplayOrder,
                    IsActive = vm.IsActive,
                    ImageUrl = imgPath!,
                    CreatedAt = DateTime.Now
                };

                _context.Banners.Add(banner);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã thêm banner thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(vm);
        }

        // GET: /AdminBanner/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();

            var vm = new BannerCrudVM
            {
                BannerId = banner.BannerId,
                Title = banner.Title,
                Subtitle = banner.Subtitle,
                LinkUrl = banner.LinkUrl,
                Position = banner.Position,
                DisplayOrder = banner.DisplayOrder,
                IsActive = banner.IsActive,
                CurrentImageUrl = banner.ImageUrl
            };
            return View(vm);
        }

        // POST: /AdminBanner/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BannerCrudVM vm)
        {
            if (id != vm.BannerId) return BadRequest();

            if (ModelState.IsValid)
            {
                var banner = await _context.Banners.FindAsync(id);
                if (banner == null) return NotFound();

                banner.Title = vm.Title;
                banner.Subtitle = vm.Subtitle;
                banner.LinkUrl = vm.LinkUrl;
                banner.Position = vm.Position;
                banner.DisplayOrder = vm.DisplayOrder;
                banner.IsActive = vm.IsActive;

                if (vm.ImageFile != null && vm.ImageFile.Length > 0)
                {
                    string? newImg = await UploadBannerImageAsync(vm.ImageFile);
                    if (!string.IsNullOrEmpty(newImg))
                    {
                        banner.ImageUrl = newImg;
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật banner thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(vm);
        }

        // POST: /AdminBanner/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner != null)
            {
                string path = Path.Combine(_env.WebRootPath, banner.ImageUrl.Replace("/", "\\"));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);

                _context.Banners.Remove(banner);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa banner!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}