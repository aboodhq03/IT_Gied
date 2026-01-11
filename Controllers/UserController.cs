using Microsoft.AspNetCore.Authorization;
using IT_Gied.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.IO;

namespace IT_Gied.Controllers
{
    public class UserController : Controller
    {
        private UserManager<ApplicationUser> _UserManager;
        private SignInManager<ApplicationUser> _SignInManager;
        private RoleManager<IdentityRole> _RoleManager;

        public UserController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _UserManager = userManager;
            _SignInManager = signInManager;
            _RoleManager = roleManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        // =========================
        // Register
        // =========================

        [HttpGet]
        public IActionResult Regester()
        {
            return View();
        }

        // رفع صورة واحد فقط (Async)
        private async Task<string> UploadImageAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0) return "";

            var imageName = $"{Guid.NewGuid()}_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(file.FileName)}";
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uplode", folderName);
            Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, imageName);

            using var stream = System.IO.File.Create(filePath);
            await file.CopyToAsync(stream);

            return imageName;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Regester(RegesterModel model)
        {
            // 1) Validation
            if (!ModelState.IsValid)
                return View(model);

            var email = model.User_email?.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(nameof(model.User_email), "Email is required.");
                return View(model);
            }

            // 2) إنشاء المستخدم
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
            };

            // 3) رفع الصورة (إن وجدت)
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var imgName = await UploadImageAsync(model.ImageFile, "Profiles");
                user.Image_Name = imgName;
            }

            // 4) إنشاء في Identity (DB)
            var result = await _UserManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                // أهم شيء: عرض أسباب فشل الحفظ (Password policy / duplicate email / ...)
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);

                return View(model);
            }

            // 5) (اختياري) تسجيل الدخول مباشرة بعد التسجيل
            await _SignInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Index", "Home");
        }

        // =========================
        // Login / Logout
        // =========================

        [HttpGet]
        public IActionResult login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> login(loginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApplicationUser User = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result = await _SignInManager.PasswordSignInAsync(User.UserName, model.Password, true, true);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        public async Task<IActionResult> logout()
        {
            await _SignInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> EditProfile()
        {
            var user = await _UserManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("login");

            var model = new EditProfileViewModel
            {
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CurrentImageName = user.Image_Name
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _UserManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("login");

            var newEmail = model.Email?.Trim();
            var newPhone = model.PhoneNumber?.Trim();

            if (!string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            {
                var setEmailResult = await _UserManager.SetEmailAsync(user, newEmail);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var err in setEmailResult.Errors)
                        ModelState.AddModelError("", err.Description);
                    return View(model);
                }

                var setUserNameResult = await _UserManager.SetUserNameAsync(user, newEmail);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var err in setUserNameResult.Errors)
                        ModelState.AddModelError("", err.Description);
                    return View(model);
                }
            }

            if (!string.Equals(user.PhoneNumber, newPhone, StringComparison.OrdinalIgnoreCase))
            {
                var setPhoneResult = await _UserManager.SetPhoneNumberAsync(user, newPhone);
                if (!setPhoneResult.Succeeded)
                {
                    foreach (var err in setPhoneResult.Errors)
                        ModelState.AddModelError("", err.Description);
                    return View(model);
                }
            }

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var newImg = UploadImage(new List<IFormFile> { model.ImageFile }, "Profiles");

                if (!string.IsNullOrWhiteSpace(newImg))
                {
                    if (!string.IsNullOrWhiteSpace(user.Image_Name))
                    {
                        var oldPath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot", "Uplode", "Profiles",
                            user.Image_Name);

                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    user.Image_Name = newImg;
                    await _UserManager.UpdateAsync(user);
                }
            }

            await _SignInManager.RefreshSignInAsync(user);

            ViewBag.Success = "Profile updated successfully.";
            return View(model);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (model.CurrentPassword == model.NewPassword)
            {
                ModelState.AddModelError(nameof(model.NewPassword), "New password must be different from current password.");
                return View(model);
            }

            var user = await _UserManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("login");

            var result = await _UserManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);

                return View(model);
            }

            await _SignInManager.RefreshSignInAsync(user);

            ViewBag.Success = "Password changed successfully.";
            return View(new ChangePasswordViewModel());
        }

        // ====== دالتك القديمة كما هي (موجودة لأن EditProfile تستعملها) ======
        public string UploadImage(List<IFormFile> File, string FolderName)
        {
            foreach (var recive_file in File)
            {
                if (recive_file.Length > 0)
                {
                    string ImageName = $"{Guid.NewGuid()}_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(recive_file.FileName)}";
                    var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uplode", FolderName);
                    Directory.CreateDirectory(folderPath);

                    var FilePath = Path.Combine(folderPath, ImageName);

                    using (var stram = System.IO.File.Create(FilePath))
                    {
                        recive_file.CopyTo(stram);
                        return ImageName;
                    }
                }
            }
            return "";
        }
    }
}
