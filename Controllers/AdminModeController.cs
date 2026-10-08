using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebBanPC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminModeController : Controller
    {
        public const string EDIT_MODE_KEY = "Admin_Visual_Edit_Mode";

        // Bật chế độ quản lý trực quan
        [HttpGet]
        public IActionResult Enable(string? returnUrl)
        {
            HttpContext.Session.SetString(EDIT_MODE_KEY, "true");
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "TrangChu");
        }

        // Thoát khỏi chế độ quản lý trực quan
        [HttpGet]
        public IActionResult Disable(string? returnUrl)
        {
            HttpContext.Session.Remove(EDIT_MODE_KEY);
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "TrangChu");
        }
    }
}