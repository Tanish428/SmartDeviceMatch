using Microsoft.AspNetCore.Identity;

namespace SmartDeviceMatch.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAdminAsync(
            IServiceProvider services,
            IConfiguration configuration)
        {
            var userManager =
                services.GetRequiredService<UserManager<IdentityUser>>();

            var roleManager =
                services.GetRequiredService<RoleManager<IdentityRole>>();

            var adminEmail =
                configuration["AdminCredentials:Email"];

            var adminPassword =
                configuration["AdminCredentials:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail) ||
                string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException(
                    "Admin credentials are not configured.");
            }

            // Create Admin role if it does not exist
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                var roleResult =
                    await roleManager.CreateAsync(
                        new IdentityRole("Admin"));

                if (!roleResult.Succeeded)
                {
                    throw new Exception(
                        "Failed to create Admin role.");
                }
            }

            // Check whether the admin already exists
            var admin =
                await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createResult =
                    await userManager.CreateAsync(
                        admin,
                        adminPassword);

                if (!createResult.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        createResult.Errors.Select(e => e.Description));

                    throw new Exception(
                        $"Failed to create admin: {errors}");
                }

                await userManager.AddToRoleAsync(
                    admin,
                    "Admin");
            }
            else
            {
                // Make sure existing admin has the Admin role
                if (!await userManager.IsInRoleAsync(admin, "Admin"))
                {
                    await userManager.AddToRoleAsync(
                        admin,
                        "Admin");
                }
            }
        }
    }
}