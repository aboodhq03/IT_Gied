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
            builder.Services.AddIdentity<IdentityUser, IdentityRole>(
                option =>
                {
                    option.Password.RequiredLength = 8;
                    option.Password.RequireNonAlphanumeric = true;
                    option.Password.RequireUppercase = true;
                    option.Password.RequireLowercase = true;
                    option.User.RequireUniqueEmail = true;


                }
            ).AddEntityFrameworkStores<AppDbContext>();
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
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
