using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using WebBanPC.Models;

namespace WebBanPC.Controllers
{
    public class AdminProductController : Controller
    {
        private readonly PcStoreDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminProductController(PcStoreDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // =========================================================================
        // HÀM CHUYỂN FILE ẢNH THÀNH CHUỖI ĐƯỜNG DẪN ĐỂ LƯU VÀO SQL
        // =========================================================================
        private async Task<string?> UploadImageToStringAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return null;

            // 1. Thư mục đích: wwwroot/img/products
            string uploadFolder = Path.Combine(_env.WebRootPath, "img", "products");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // 2. Tạo tên file độc nhất để tránh bị trùng đè file cũ
            string extension = Path.GetExtension(file.FileName).ToLower();
            string uniqueFileName = $"{Guid.NewGuid():N}_{DateTime.Now.Ticks}{extension}";
            string fullPath = Path.Combine(uploadFolder, uniqueFileName);

            // 3. Ghi file lên ổ cứng máy chủ
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 4. Trả về chuỗi đường dẫn chuẩn để lưu vào cột ImageUrl trong SQL Server
            return $"img/products/{uniqueFileName}";
        }

        // Helper tạo slug URL chuẩn
        private string GenerateSlug(string text)
        {
            text = text.ToLowerInvariant();
            text = Regex.Replace(text, @"[^a-z0-9\s-]", "");
            text = Regex.Replace(text, @"\s+", "-").Trim('-');
            return text;
        }

        private async Task LoadDropdownData()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName");
            ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "BrandId", "BrandName");
        }

        // =========================================================================
        // 1. DANH SÁCH SẢN PHẨM (INDEX)
        // =========================================================================
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .OrderByDescending(p => p.ProductId)
                .ToListAsync();

            return View(products);
        }

        // =========================================================================
        // 2. THÊM MỚI (CREATE)
        // =========================================================================
        public async Task<IActionResult> Create()
        {
            await LoadDropdownData();
            return View(new ProductCrudVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCrudVM vm)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng SKU
                if (await _context.Products.AnyAsync(p => p.Sku == vm.Sku))
                {
                    ModelState.AddModelError("Sku", "Mã SKU này đã tồn tại!");
                    await LoadDropdownData();
                    return View(vm);
                }

                // 1. Tạo đối tượng Product
                var product = new Product
                {
                    CategoryId = vm.CategoryId,
                    BrandId = vm.BrandId,
                    Sku = vm.Sku,
                    Name = vm.Name,
                    Slug = GenerateSlug(vm.Name) + "-" + Guid.NewGuid().ToString("N").Substring(0, 5),
                    OldPrice = vm.OldPrice,
                    Price = vm.Price,
                    StockQuantity = vm.StockQuantity,
                    WarrantyMonths = vm.WarrantyMonths,
                    ShortDescription = vm.ShortDescription,
                    FullDescription = vm.FullDescription,
                    IsNew = vm.IsNew,
                    IsFeatured = vm.IsFeatured,
                    CreatedAt = DateTime.Now
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // 2. Chuyển ảnh thành chuỗi URL và lưu vào bảng ProductImages
                string? imageUrlString = await UploadImageToStringAsync(vm.ImageFile);
                if (!string.IsNullOrEmpty(imageUrlString))
                {
                    var productImage = new ProductImage
                    {
                        ProductId = product.ProductId,
                        ImageUrl = imageUrlString,
                        IsDefault = true,
                        DisplayOrder = 1
                    };
                    _context.ProductImages.Add(productImage);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownData();
            return View(vm);
        }

        // =========================================================================
        // 3. CẬP NHẬT / SỬA (EDIT)
        // =========================================================================
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            var defaultImg = product.ProductImages.FirstOrDefault(i => i.IsDefault)?.ImageUrl
                          ?? product.ProductImages.FirstOrDefault()?.ImageUrl;

            var vm = new ProductCrudVM
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Sku = product.Sku,
                CategoryId = product.CategoryId,
                BrandId = product.BrandId,
                OldPrice = product.OldPrice,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                WarrantyMonths = product.WarrantyMonths,
                ShortDescription = product.ShortDescription,
                FullDescription = product.FullDescription,
                IsNew = product.IsNew,
                IsFeatured = product.IsFeatured,
                CurrentImageUrl = defaultImg
            };

            await LoadDropdownData();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductCrudVM vm)
        {
            if (id != vm.ProductId) return BadRequest();

            if (ModelState.IsValid)
            {
                var product = await _context.Products
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null) return NotFound();

                // Cập nhật thông số cơ bản
                product.Name = vm.Name;
                product.Sku = vm.Sku;
                product.CategoryId = vm.CategoryId;
                product.BrandId = vm.BrandId;
                product.OldPrice = vm.OldPrice;
                product.Price = vm.Price;
                product.StockQuantity = vm.StockQuantity;
                product.WarrantyMonths = vm.WarrantyMonths;
                product.ShortDescription = vm.ShortDescription;
                product.FullDescription = vm.FullDescription;
                product.IsNew = vm.IsNew;
                product.IsFeatured = vm.IsFeatured;

                // Nếu người dùng chọn ảnh mới -> gọi hàm đổi thành chuỗi và cập nhật
                if (vm.ImageFile != null && vm.ImageFile.Length > 0)
                {
                    string? newImageUrl = await UploadImageToStringAsync(vm.ImageFile);
                    var currentImage = product.ProductImages.FirstOrDefault(i => i.IsDefault);

                    if (currentImage != null)
                    {
                        currentImage.ImageUrl = newImageUrl!;
                    }
                    else
                    {
                        product.ProductImages.Add(new ProductImage
                        {
                            ProductId = product.ProductId,
                            ImageUrl = newImageUrl!,
                            IsDefault = true,
                            DisplayOrder = 1
                        });
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownData();
            return View(vm);
        }

        // =========================================================================
        // 4. XÓA (DELETE)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product != null)
            {
                // Xóa file ảnh vật lý trong wwwroot để giải phóng bộ nhớ
                foreach (var img in product.ProductImages)
                {
                    string filePhysicalPath = Path.Combine(_env.WebRootPath, img.ImageUrl.Replace("/", "\\"));
                    if (System.IO.File.Exists(filePhysicalPath))
                    {
                        System.IO.File.Delete(filePhysicalPath);
                    }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa sản phẩm thành công!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}