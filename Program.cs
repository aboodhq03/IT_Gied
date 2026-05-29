using IT_Gied.Models;
using IT_Gied.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Gied
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

         
            builder.Services.AddControllersWithViews();

          
            builder.Services.AddHttpClient<GeminiService>();
            builder.Services.AddHttpClient<AiExplanationService>();
            builder.Services.AddHttpClient<RssNewsService>();
            builder.Services.AddScoped<AcademicAdvisorService>();
            builder.Services.AddScoped<IFileService, FileService>();
            builder.Services.AddScoped<IStudentBenefitService, StudentBenefitService>();

            builder.Services.AddDbContext<AppDbContext>(option => 
                option.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection")));

          
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(option =>
            {
                option.Password.RequiredLength = 8;
                option.Password.RequireNonAlphanumeric = true;
                option.Password.RequireUppercase = true;
                option.Password.RequireLowercase = true;
                option.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

           
            builder.Services.ConfigureApplicationCookie(option =>
            {
                option.AccessDeniedPath = "/User/AccessDenied";
                option.Cookie.Name = "ITGuide_Session";
                option.Cookie.HttpOnly = true;
                option.ExpireTimeSpan = TimeSpan.FromMinutes(28);
                option.LoginPath = "/User/Login";
                option.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
            });

            builder.Services.AddMemoryCache();

            var app = builder.Build();

          
            ApplyDatabaseMigrations(app);

            
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            
               app.UseHsts();

            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

         
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                var db = services.GetRequiredService<AppDbContext>();

              
                if (!roleManager.RoleExistsAsync("Admin").Result)
                    roleManager.CreateAsync(new IdentityRole("Admin")).Wait();

                var adminEmail = "admin@admin.com";
                if (userManager.FindByEmailAsync(adminEmail).Result == null)
                {
                    var newAdmin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                    if (userManager.CreateAsync(newAdmin, "Admin@123").Result.Succeeded)
                        userManager.AddToRoleAsync(newAdmin, "Admin").Wait();
                }

            }

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            static void ApplyDatabaseMigrations(WebApplication app)
            {
                using var scope = app.Services.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                try
                {
                    db.Database.Migrate();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database migration failed: {ex.Message}");
                }
            }
            app.Run();
        }
    }
}