using IT_Gied.Models;
using IT_Gied.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.IO;
using System.Text;

namespace IT_Gied.Controllers
{
    /// <summary>
    /// Controller for Administrative tasks (User & Role Management).
    /// المتحكم الخاص بالعمليات الإدارية (إدارة المستخدمين والأدوار).
    /// </summary>
    [Authorize(Roles = "Admin")] // Restricted to users with the "Admin" role only (الوصول محصور للمسؤولين فقط)
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IStudentBenefitService _studentBenefitService;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, AppDbContext context, IStudentBenefitService studentBenefitService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _studentBenefitService = studentBenefitService;
        }

        /// <summary>
        /// Admin Dashboard overview.
        /// لوحة التحكم الرئيسية للمسؤول.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalRoles = await _roleManager.Roles.CountAsync(),
                TotalCourses = await _context.Courses.CountAsync(),
                TotalTracks = await _context.Tracks.CountAsync(),
                TotalAdvisorChats = await _context.AdvisorChatHistories.CountAsync(),
                TotalStudentBenefits = await _context.StudentBenefits.CountAsync()
            };

            return View(viewModel);
        }

        // ==========================================
        // User Management (إدارة المستخدمين)
        // ==========================================

        public async Task<IActionResult> Users()
        {
            // Fetching all users from the Identity database
            // جلب جميع المستخدمين من قاعدة بيانات الهوية
            var allUsers = await _userManager.Users.ToListAsync();
            return View(allUsers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var userToDelete = await _userManager.FindByIdAsync(id);
            if (userToDelete != null)
            {
                // Prevent admin from deleting themselves accidentally
                // منع المسؤول من حذف حسابه الخاص عن طريق الخطأ
                if (userToDelete.UserName == User.Identity?.Name)
                    return BadRequest("Cannot delete your own admin account.");

                await _userManager.DeleteAsync(userToDelete);
            }

            return RedirectToAction(nameof(Users));
        }

        // ==========================================
        // Role Management (إدارة الأدوار وصلاحيات النظام)
        // ==========================================

        public async Task<IActionResult> Roles()
        {
            const string adminRoleName = "Admin";
            if (!await _roleManager.RoleExistsAsync(adminRoleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(adminRoleName));
            }

            var allUsers = await _userManager.Users.ToListAsync();
            var users = new List<AdminRoleUserViewModel>();
            foreach (var user in allUsers)
            {
                users.Add(new AdminRoleUserViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    IsAdmin = await _userManager.IsInRoleAsync(user, adminRoleName)
                });
            }

            var viewModel = new AdminRoleViewModel
            {
                AdminRoleExists = true,
                Users = users
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantAdmin(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                const string adminRoleName = "Admin";
                if (!await _roleManager.RoleExistsAsync(adminRoleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(adminRoleName));
                }

                if (!await _userManager.IsInRoleAsync(user, adminRoleName))
                {
                    await _userManager.AddToRoleAsync(user, adminRoleName);
                }
            }

            return RedirectToAction(nameof(Roles));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeAdmin(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
            }
            return RedirectToAction(nameof(Roles));
        }
        // ==========================================
        // Course Management (إدارة المساقات)
        // ==========================================

        public async Task<IActionResult> Courses()
        {
            var courses = await _context.Courses.ToListAsync();
            return View(courses);
        }

        public async Task<IActionResult> CreateCourse()
        {
            ViewBag.AllCourses = await _context.Courses
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View("EditCourse", new Course());
        }

        public async Task<IActionResult> EditCourse(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Prerequisites)
                    .ThenInclude(p => p.PrerequisiteCourse)
                .FirstOrDefaultAsync(c => c.Id == id);
                
            if (course == null && id != 0) return NotFound();
            
            if (course == null) course = new Course();

            ViewBag.AllCourses = await _context.Courses
                .Where(c => c.Id != id)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCourse(Course course, int[]? prereqIds, string[]? prereqRelations)
        {
            if (ModelState.IsValid)
            {
                if (course.Id == 0)
                {
                    _context.Courses.Add(course);
                }
                else
                {
                    _context.Courses.Update(course);
                    
                    // Sync Prerequisites
                    var existingPrereqs = await _context.CoursePrerequisites
                        .Where(p => p.CourseId == course.Id)
                        .ToListAsync();
                        
                    _context.CoursePrerequisites.RemoveRange(existingPrereqs);
                }
                
                await _context.SaveChangesAsync();

                // Add new prerequisites from lists
                if (prereqIds != null && prereqRelations != null)
                {
                    for (int i = 0; i < prereqIds.Length; i++)
                    {
                        var prereqId = prereqIds[i];
                        var relation = i < prereqRelations.Length ? prereqRelations[i] : "Prerequisite";

                        if (prereqId > 0 && prereqId != course.Id)
                        {
                            _context.CoursePrerequisites.Add(new CoursePrerequisite
                            {
                                CourseId = course.Id,
                                PrerequisiteCourseId = prereqId,
                                RelationType = string.IsNullOrWhiteSpace(relation) ? "Prerequisite" : relation
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Courses));
            }
            
            ViewBag.AllCourses = await _context.Courses
                .Where(c => c.Id != course.Id)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View("EditCourse", course);
        }

        public async Task<IActionResult> StudentBenefits()
        {
            var benefits = await _studentBenefitService.GetAllBenefitsAsync();
            return View(benefits);
        }

        public IActionResult CreateStudentBenefit()
        {
            ViewBag.Categories = new List<string>
            {
                "Developer Tools",
                "Student Discounts",
                "AI & Cloud Credits",
                "Learning Platforms",
                "Design Tools"
            };

            return View("EditStudentBenefit", new StudentBenefit { IsActive = true });
        }

        public async Task<IActionResult> EditStudentBenefit(int id)
        {
            var benefit = await _studentBenefitService.GetByIdAsync(id);
            if (benefit == null) return NotFound();

            ViewBag.Categories = new List<string>
            {
                "Developer Tools",
                "Student Discounts",
                "AI & Cloud Credits",
                "Learning Platforms",
                "Design Tools"
            };

            return View("EditStudentBenefit", benefit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveStudentBenefit(StudentBenefit benefit)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new List<string>
                {
                    "Developer Tools",
                    "Student Discounts",
                    "AI & Cloud Credits",
                    "Learning Platforms",
                    "Design Tools"
                };

                return View("EditStudentBenefit", benefit);
            }

            if (benefit.Id == 0)
            {
                await _studentBenefitService.AddAsync(benefit);
                TempData["Success"] = "The new student benefit has been created successfully.";
            }
            else
            {
                await _studentBenefitService.UpdateAsync(benefit);
                TempData["Success"] = "The student benefit has been updated successfully.";
            }

            return RedirectToAction(nameof(StudentBenefits));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                try
                {
                    // 1. Delete related prerequisites where this course is either the main course or the prerequisite
                    var relatedPrereqs = await _context.CoursePrerequisites
                        .Where(p => p.CourseId == id || p.PrerequisiteCourseId == id)
                        .ToListAsync();
                    if (relatedPrereqs.Any())
                    {
                        _context.CoursePrerequisites.RemoveRange(relatedPrereqs);
                    }

                    // 2. Delete related user course progress
                    var relatedProgress = await _context.UserCourseProgresses
                        .Where(p => p.CourseId == id)
                        .ToListAsync();
                    if (relatedProgress.Any())
                    {
                        _context.UserCourseProgresses.RemoveRange(relatedProgress);
                    }

                    // 3. Delete related course ratings
                    var relatedRatings = await _context.CourseRatings
                        .Where(r => r.CourseId == id)
                        .ToListAsync();
                    if (relatedRatings.Any())
                    {
                        _context.CourseRatings.RemoveRange(relatedRatings);
                    }

                    // Now safe to remove the course itself
                    _context.Courses.Remove(course);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = "تم حذف الكورس وجميع البيانات المرتبطة به بنجاح.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"حدث خطأ أثناء حذف الكورس: {ex.Message}";
                }
            }
            else
            {
                TempData["Error"] = "الكورس المراد حذفه غير موجود.";
            }
            return RedirectToAction(nameof(Courses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStudentBenefit(int id)
        {
            var benefit = await _studentBenefitService.GetByIdAsync(id);
            if (benefit == null) return NotFound();

            await _studentBenefitService.SetActiveAsync(id, !benefit.IsActive);
            TempData["Success"] = benefit.IsActive
                ? "The benefit has been disabled successfully."
                : "The benefit has been enabled successfully.";

            return RedirectToAction(nameof(StudentBenefits));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudentBenefit(int id)
        {
            await _studentBenefitService.DeleteAsync(id);
            TempData["Success"] = "The student benefit has been removed successfully.";
            return RedirectToAction(nameof(StudentBenefits));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCourses(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "الرجاء اختيار ملف نصي صالح يحتوي على بيانات المساقات.";
                return RedirectToAction(nameof(Courses));
            }

            try
            {
                var coursesList = new List<ParsedCourseDto>();
                using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
                {
                    var content = await reader.ReadToEndAsync();
                    var trimmedContent = content.Trim();

                    // 1. Try parsing JSON format
                    if (trimmedContent.StartsWith("[") && trimmedContent.EndsWith("]"))
                    {
                        try
                        {
                            var parsedJson = JsonSerializer.Deserialize<List<JsonCourseImportDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (parsedJson != null && parsedJson.Any())
                            {
                                foreach (var pj in parsedJson)
                                {
                                    if (string.IsNullOrWhiteSpace(pj.Code) || string.IsNullOrWhiteSpace(pj.Name)) continue;

                                    var dto = new ParsedCourseDto
                                    {
                                        Course = new Course
                                        {
                                            Code = pj.Code.Trim(),
                                            Name = pj.Name.Trim(),
                                            CreditHours = pj.CreditHours,
                                            Description = pj.Description?.Trim(),
                                            DetailsUrl = pj.DetailsUrl?.Trim(),
                                            EDetailsUrl = pj.EDetailsUrl?.Trim()
                                        }
                                    };

                                    if (pj.Prerequisites != null)
                                    {
                                        dto.Prerequisites.AddRange(pj.Prerequisites
                                            .Where(p => !string.IsNullOrWhiteSpace(p.Code))
                                            .Select(p => new ParsedPrerequisiteDto { Code = p.Code.Trim(), RelationType = string.IsNullOrWhiteSpace(p.RelationType) ? "Prerequisite" : p.RelationType.Trim() }));
                                    }
                                    else if (pj.Prereqs != null)
                                    {
                                        dto.Prerequisites.AddRange(pj.Prereqs
                                            .Where(p => !string.IsNullOrWhiteSpace(p))
                                            .Select(p => new ParsedPrerequisiteDto { Code = p.Trim(), RelationType = "Prerequisite" }));
                                    }

                                    coursesList.Add(dto);
                                }
                            }
                        }
                        catch { /* Fallback to line parsing if JSON fails */ }
                    }

                    // 2. Try parsing Key-Value format (separated by "---" or double newlines)
                    if (!coursesList.Any())
                    {
                        var blocks = trimmedContent.Split(new[] { "---" }, StringSplitOptions.RemoveEmptyEntries);
                        if (blocks.Length == 1 && (trimmedContent.Contains("Code:") || trimmedContent.Contains("code:")))
                        {
                            blocks = trimmedContent.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                        }

                        if (blocks.Length > 0 && (trimmedContent.Contains("Code:") || trimmedContent.Contains("code:") || trimmedContent.Contains("Name:") || trimmedContent.Contains("name:")))
                        {
                            foreach (var block in blocks)
                            {
                                if (string.IsNullOrWhiteSpace(block)) continue;

                                var lines = block.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                var dto = new ParsedCourseDto();
                                bool hasData = false;

                                foreach (var line in lines)
                                {
                                    var colonIndex = line.IndexOf(':');
                                    if (colonIndex <= 0) continue;

                                    var key = line.Substring(0, colonIndex).Trim().ToLower();
                                    var val = line.Substring(colonIndex + 1).Trim();

                                    switch (key)
                                    {
                                        case "code":
                                            dto.Course.Code = val;
                                            hasData = true;
                                            break;
                                        case "name":
                                            dto.Course.Name = val;
                                            hasData = true;
                                            break;
                                        case "credithours":
                                        case "credit":
                                        case "hours":
                                            if (int.TryParse(val, out int ch)) dto.Course.CreditHours = ch;
                                            break;
                                        case "description":
                                        case "desc":
                                            dto.Course.Description = val;
                                            break;
                                        case "detailsurl":
                                        case "details":
                                            dto.Course.DetailsUrl = val;
                                            break;
                                        case "edetailsurl":
                                        case "edetails":
                                            dto.Course.EDetailsUrl = val;
                                            break;
                                        case "prerequisites":
                                        case "prerequisite":
                                        case "prereqs":
                                        case "prereq":
                                            dto.Prerequisites = ParsePrerequisitesString(val);
                                            break;
                                    }
                                }

                                if (hasData && !string.IsNullOrWhiteSpace(dto.Course.Code) && !string.IsNullOrWhiteSpace(dto.Course.Name))
                                {
                                    dto.Course.Code = dto.Course.Code.Trim();
                                    dto.Course.Name = dto.Course.Name.Trim();
                                    coursesList.Add(dto);
                                }
                            }
                        }
                    }

                    // 3. Fallback: Line-by-Line CSV/Pipe-separated format
                    if (!coursesList.Any())
                    {
                        var lines = trimmedContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            
                            // Skip header row if present
                            if (line.ToLower().Contains("code|name") || line.ToLower().Contains("code,name")) continue;

                            string[] parts;
                            if (line.Contains('|'))
                            {
                                parts = line.Split('|');
                            }
                            else if (line.Contains(','))
                            {
                                parts = line.Split(',');
                            }
                            else
                            {
                                continue;
                            }

                            if (parts.Length >= 2)
                            {
                                var dto = new ParsedCourseDto();
                                dto.Course.Code = parts[0].Trim();
                                dto.Course.Name = parts[1].Trim();

                                if (parts.Length > 2 && int.TryParse(parts[2].Trim(), out int ch))
                                {
                                    dto.Course.CreditHours = ch;
                                }
                                if (parts.Length > 3)
                                {
                                    dto.Course.Description = parts[3].Trim();
                                }
                                if (parts.Length > 4)
                                {
                                    dto.Course.DetailsUrl = parts[4].Trim();
                                }
                                if (parts.Length > 5)
                                {
                                    dto.Course.EDetailsUrl = parts[5].Trim();
                                }
                                if (parts.Length > 6)
                                {
                                    dto.Prerequisites = ParsePrerequisitesString(parts[6].Trim());
                                }

                                if (!string.IsNullOrWhiteSpace(dto.Course.Code) && !string.IsNullOrWhiteSpace(dto.Course.Name))
                                {
                                    coursesList.Add(dto);
                                }
                            }
                        }
                    }
                }

                if (!coursesList.Any())
                {
                    TempData["Error"] = "تعذر قراءة أي مساقات من الملف. الرجاء التأكد من اتباع الصيغة الموضحة.";
                    return RedirectToAction(nameof(Courses));
                }

                // ==================== PASS 1: UPSERT COURSES ====================
                int addedCount = 0;
                int updatedCount = 0;

                foreach (var dto in coursesList)
                {
                    var existing = await _context.Courses.FirstOrDefaultAsync(c => c.Code.ToLower() == dto.Course.Code.ToLower());
                    if (existing != null)
                    {
                        existing.Name = dto.Course.Name;
                        existing.CreditHours = dto.Course.CreditHours;
                        existing.Description = dto.Course.Description;
                        existing.DetailsUrl = dto.Course.DetailsUrl;
                        existing.EDetailsUrl = dto.Course.EDetailsUrl;
                        _context.Courses.Update(existing);
                        
                        dto.Course = existing;
                        updatedCount++;
                    }
                    else
                    {
                        _context.Courses.Add(dto.Course);
                        addedCount++;
                    }
                }

                // Save changes so new courses get database-generated IDs
                await _context.SaveChangesAsync();

                // ==================== PASS 2: LINK PREREQUISITES ====================
                int linkedRelationsCount = 0;
                foreach (var dto in coursesList)
                {
                    // Clean old prerequisites for this course
                    var oldPrereqs = await _context.CoursePrerequisites
                        .Where(p => p.CourseId == dto.Course.Id)
                        .ToListAsync();
                    if (oldPrereqs.Any())
                    {
                        _context.CoursePrerequisites.RemoveRange(oldPrereqs);
                    }

                    if (dto.Prerequisites.Any())
                    {
                        foreach (var prereqDto in dto.Prerequisites)
                        {
                            // Lookup referenced course ID in the database by its code
                            var prereqCourse = await _context.Courses
                                .FirstOrDefaultAsync(c => c.Code.ToLower() == prereqDto.Code.ToLower());

                            if (prereqCourse != null && prereqCourse.Id != dto.Course.Id)
                            {
                                _context.CoursePrerequisites.Add(new CoursePrerequisite
                                {
                                    CourseId = dto.Course.Id,
                                    PrerequisiteCourseId = prereqCourse.Id,
                                    RelationType = prereqDto.RelationType
                                });
                                linkedRelationsCount++;
                            }
                        }
                    }
                }

                // Save final relationships
                await _context.SaveChangesAsync();

                TempData["Success"] = $"تم استيراد المساقات بنجاح: تم إضافة {addedCount} مساقات جديدة، وتحديث {updatedCount} مساقات موجودة، وربط {linkedRelationsCount} علاقة متطلبات سابقة.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ أثناء معالجة الملف: {ex.Message}";
            }

            return RedirectToAction(nameof(Courses));
        }

        private List<ParsedPrerequisiteDto> ParsePrerequisitesString(string prereqStr)
        {
            var list = new List<ParsedPrerequisiteDto>();
            if (string.IsNullOrWhiteSpace(prereqStr)) return list;

            var parts = prereqStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var item = part.Trim();
                if (string.IsNullOrEmpty(item)) continue;

                // Pattern 1: CS101 (RelationType)
                if (item.Contains('(') && item.EndsWith(")"))
                {
                    var openParen = item.IndexOf('(');
                    var code = item.Substring(0, openParen).Trim();
                    var relation = item.Substring(openParen + 1, item.Length - openParen - 2).Trim();
                    if (!string.IsNullOrEmpty(code))
                    {
                        list.Add(new ParsedPrerequisiteDto { Code = code, RelationType = relation });
                    }
                }
                // Pattern 2: CS101:RelationType
                else if (item.Contains(':'))
                {
                    var colonIndex = item.IndexOf(':');
                    var code = item.Substring(0, colonIndex).Trim();
                    var relation = item.Substring(colonIndex + 1).Trim();
                    if (!string.IsNullOrEmpty(code))
                    {
                        list.Add(new ParsedPrerequisiteDto { Code = code, RelationType = relation });
                    }
                }
                // Pattern 3: Simple Code (defaults to Prerequisite)
                else
                {
                    list.Add(new ParsedPrerequisiteDto { Code = item, RelationType = "Prerequisite" });
                }
            }
            return list;
        }

        // --- Import DTOs ---
        private class ParsedCourseDto
        {
            public Course Course { get; set; } = new Course();
            public List<ParsedPrerequisiteDto> Prerequisites { get; set; } = new List<ParsedPrerequisiteDto>();
        }

        private class ParsedPrerequisiteDto
        {
            public string Code { get; set; } = string.Empty;
            public string RelationType { get; set; } = "Prerequisite";
        }

        private class JsonCourseImportDto
        {
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public int CreditHours { get; set; }
            public string? Description { get; set; }
            public string? DetailsUrl { get; set; }
            public string? EDetailsUrl { get; set; }
            public List<JsonPrerequisiteDto>? Prerequisites { get; set; }
            public List<string>? Prereqs { get; set; }
        }

        private class JsonPrerequisiteDto
        {
            public string Code { get; set; } = string.Empty;
            public string RelationType { get; set; } = "Prerequisite";
        }
    }
}
