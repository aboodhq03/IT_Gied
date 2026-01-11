using IT_Gied.Models;
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

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            #region connect_to_datebase
            builder.Services.AddDbContext<AppDbContext>(
              option => option.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection")));
            #endregion

            #region Identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(
                option =>
                {
                    option.Password.RequiredLength = 8;
                    option.Password.RequireNonAlphanumeric = true;
                    option.Password.RequireUppercase = true;
                    option.Password.RequireLowercase = true;
                    option.User.RequireUniqueEmail = true;
                }
            )
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(
                option =>
                {
                    option.AccessDeniedPath = "/User/AccessDenied";
                    option.Cookie.Name = "Cookie";
                    option.Cookie.HttpOnly = true;
                    option.ExpireTimeSpan = TimeSpan.FromMinutes(28);
                    option.LoginPath = "/User/Login";
                    option.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
                }
            );
            #endregion
            builder.Services.AddMemoryCache();
            builder.Services.AddHttpClient<IT_Gied.Services.RssNewsService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
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

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
