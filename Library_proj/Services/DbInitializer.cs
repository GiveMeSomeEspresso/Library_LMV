using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Library_proj.Data;
using Library_proj.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Library_proj.Services
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                // Застосування міграцій (якщо вони ще не застосовані)
                context.Database.Migrate();

                // Створення ролей "Admin" та "User", якщо їх немає
                if (!await roleManager.RoleExistsAsync("Admin"))
                    await roleManager.CreateAsync(new IdentityRole("Admin"));

                if (!await roleManager.RoleExistsAsync("User"))
                    await roleManager.CreateAsync(new IdentityRole("User"));

                // Призначення ролі "Admin" першому зареєстрованому користувачеві (замініть критерій пошуку, якщо потрібно)
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "koko@koko.com"); // ЗАМІНИТИ НА ВАШ EMAIL
                if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}