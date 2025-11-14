using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using trainingAttendanceTracker.Data;
using trainingAttendanceTracker.Models;
using trainingAttendanceTracker.Services;

namespace trainingAttendanceTracker
{
    public class Program
    {
        public static async Task Main(string[] args)        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
                options.SignIn.RequireConfirmedAccount = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;
            })
            .AddRoles<IdentityRole>() 
            .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.AddControllersWithViews();
            builder.Services.AddScoped<QrCodeService>();

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
                options.AddPolicy("RequireTrainer", policy => policy.RequireRole("Trainer", "Admin"));
                options.AddPolicy("RequireMember", policy => policy.RequireRole("Member", "Trainer", "Admin"));
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
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
            app.MapRazorPages();

            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                await CreateRoles(roleManager, userManager);             }

            await app.RunAsync(); 
        }

        private static async Task CreateRoles(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            string[] roleNames = { "Admin", "Trainer", "Member" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            var adminUser = await userManager.FindByEmailAsync("admin@training.com");
            if (adminUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = "admin@training.com",
                    Email = "admin@training.com",
                    Ime = "Admin",
                    Prezime = "User",
                    DatumRodjenja = new DateTime(1980, 1, 1),
                    Rang = "Admin",
                    DatumUclanjenja = DateTime.Now,
                    EmailConfirmed = true,
                    QrCodePath = "/images/admin-qr.png"
                };

                string userPWD = "Admin123!";
                var createUser = await userManager.CreateAsync(user, userPWD);
                if (createUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }
        }
    }
}