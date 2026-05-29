using Microsoft.AspNetCore.Authorization;
using IT_Gied.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using IT_Gied.Services;

namespace IT_Gied.Controllers
{
    
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IFileService _fileService;

        public UserController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IFileService fileService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _fileService = fileService;
        }

        // ==========================================
        // Registration (تسجيل مستخدم جديد)
        // ==========================================

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Creating the user identity object
            // إنشاء كائن الهوية للمستخدم الجديد
            var user = new ApplicationUser
            {
                UserName = model.Email?.Trim(),
                Email = model.Email?.Trim(),
            };

            // Handling profile image upload via FileService
            // معالجة رفع صورة الملف الشخصي من خلال خدمة الملفات
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                user.Image_Name = await _fileService.UploadImageAsync(model.ImageFile, "Profiles");
            }

            // Saving user to database using ASP.NET Core Identity
            // حفظ المستخدم في قاعدة البيانات باستخدام نظام الهوية المدمج
            var result = await _userManager.CreateAsync(user, model.Password!);

            if (result.Succeeded)
            {
                // Auto-promote specific emails to Admin role (ترقية تلقائية للإيميل المحدد)
                if (user.Email!.Equals("admin@admin.com", StringComparison.OrdinalIgnoreCase))
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                }

                // Auto-login after successful registration
                // تسجيل الدخول التلقائي بعد نجاح عملية التسجيل
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            // Propagate errors to the view
            // تمرير أخطاء النظام (مثل كلمة مرور ضعيفة) إلى الواجهة
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // ==========================================
        // Login & Logout (الدخول والخروج)
        // ==========================================

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Attempting to sign in
            // محاولة تسجيل الدخول
            var result = await _signInManager.PasswordSignInAsync(model.Email!, model.Password!, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // Profile Management (إدارة الملف الشخصي)
        // ==========================================

        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var model = new EditProfileViewModel
            {
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CurrentImageName = user.Image_Name
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            // Syncing changes to Identity User
            // مزامنة التغييرات مع مستخدم الهوية
            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                await _userManager.SetEmailAsync(user, model.Email);
                await _userManager.SetUserNameAsync(user, model.Email);
            }

            await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber);

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                // Use FileService to manage server storage
                // استخدام خدمة الملفات لإدارة التخزين على الخادم
                if (!string.IsNullOrEmpty(user.Image_Name))
                    _fileService.DeleteImage(user.Image_Name, "Profiles");

                user.Image_Name = await _fileService.UploadImageAsync(model.ImageFile, "Profiles");
                await _userManager.UpdateAsync(user);
            }

            await _signInManager.RefreshSignInAsync(user);
            ViewBag.Success = "Profile updated successfully.";
            return View(model);
        }

        // ==========================================
        // Password Security (أمان كلمة المرور)
        // ==========================================

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword!, model.NewPassword!);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                ViewBag.Success = "Password changed successfully.";
                return View(new ChangePasswordViewModel());
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }
    }
}
