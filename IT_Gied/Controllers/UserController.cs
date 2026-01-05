using Microsoft.AspNetCore.Authorization;
using IT_Gied.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace IT_Gied.Controllers
{
    public class UserController : Controller
    {
        private UserManager<IdentityUser> _UserManager;
        private SignInManager<IdentityUser> _SignInManager;
        private RoleManager<IdentityRole> _RoleManager;
        public UserController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _UserManager = userManager;
            _SignInManager = signInManager;
            _RoleManager = roleManager;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Regester()
        {


            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Regester(RegesterModel model)
        {
            if (!ModelState.IsValid) { return View(model); }
            var User = new IdentityUser
            {
                //User_Name=model.User_name,
                UserName = model.User_email,
                Email = model.User_email,
            };
            var result = await _UserManager.CreateAsync(User, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return View(model);
            }

        }

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
            IdentityUser User = new IdentityUser
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
                PhoneNumber = user.PhoneNumber
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

            // Normalize
            var newEmail = model.Email?.Trim();
            var newPhone = model.PhoneNumber?.Trim();

            // 1) Update Email (and keep UserName = Email)
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
            // 2) Update Phone (optional)
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

            // Refresh cookie so changes reflect immediate
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

            // منع الجديد = القديم
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


    }
}
