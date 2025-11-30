using Microsoft.AspNetCore.Identity;
using DAMH.Models; 

namespace DAMH.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roleNames = { "Admin", "Member", "User" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
            var adminEmail = "admin@library.com";
            var adminPassword = "Admin123!";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    IsMember = true,
                    SubscriptionExpiryDate = DateTime.Now.AddYears(100)
                };
                var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            var testUserEmail = "user@library.com";
            var testUser = await userManager.FindByEmailAsync(testUserEmail);

            if (testUser == null)
            {
                testUser = new ApplicationUser
                {
                    UserName = testUserEmail,
                    Email = testUserEmail,
                    EmailConfirmed = true,
                    IsMember = false, // Không phải VIP
                    SubscriptionExpiryDate = null
                };
                // Mật khẩu là User123!
                var testResult = await userManager.CreateAsync(testUser, "User123!");
                if (testResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser, "User");
                }
            }

            // 3. Tạo tài khoản Member VIP (Test Premium)
            var premiumUserEmail = "premium@library.com";
            var premiumUser = await userManager.FindByEmailAsync(premiumUserEmail);

            if (premiumUser == null)
            {
                premiumUser = new ApplicationUser
                {
                    UserName = premiumUserEmail,
                    Email = premiumUserEmail,
                    EmailConfirmed = true,
                    IsMember = true, // <--- QUAN TRỌNG: Là VIP
                    SubscriptionExpiryDate = DateTime.Now.AddMonths(12) // Hạn 1 năm
                };
                // Mật khẩu là Premium123!
                var premiumResult = await userManager.CreateAsync(premiumUser, "Premium123!");
                if (premiumResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(premiumUser, "Member");
                }
            }

        }
    }
}