using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Data;
using SmartDeviceMatch.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddScoped<RequireProfileFilter>();

// Identity + Roles
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<RequireProfileFilter>();
});

var app = builder.Build();


// ============================================================
// ROLE + ADMIN SEEDING
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var roleManager =
        services.GetRequiredService<RoleManager<IdentityRole>>();

    var userManager =
        services.GetRequiredService<UserManager<IdentityUser>>();

    // Create application roles
    string[] roles =
    {
        "Admin",
        "DeviceOwner",
        "RepairShop",
        "Buyer"
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleResult =
                await roleManager.CreateAsync(
                    new IdentityRole(role));

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    roleResult.Errors.Select(e => e.Description));

                throw new Exception(
                    $"Failed to create role '{role}': {errors}");
            }
        }
    }


    // --------------------------------------------------------
    // Create the single Admin account
    // Credentials come from User Secrets.
    // --------------------------------------------------------

    var adminEmail =
        builder.Configuration["AdminCredentials:Email"];

    var adminPassword =
        builder.Configuration["AdminCredentials:Password"];


    if (string.IsNullOrWhiteSpace(adminEmail) ||
        string.IsNullOrWhiteSpace(adminPassword))
    {
        throw new InvalidOperationException(
            "Admin credentials are not configured. " +
            "Set AdminCredentials:Email and " +
            "AdminCredentials:Password using User Secrets.");
    }


    // Check whether Admin already exists
    var adminUser =
        await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var createResult =
            await userManager.CreateAsync(
                adminUser,
                adminPassword);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(
                ", ",
                createResult.Errors.Select(e => e.Description));

            throw new Exception(
                $"Failed to create Admin user: {errors}");
        }
    }


    // Make sure the Admin user has the Admin role
    if (!await userManager.IsInRoleAsync(
            adminUser,
            "Admin"))
    {
        var roleResult =
            await userManager.AddToRoleAsync(
                adminUser,
                "Admin");

        if (!roleResult.Succeeded)
        {
            var errors = string.Join(
                ", ",
                roleResult.Errors.Select(e => e.Description));

            throw new Exception(
                $"Failed to assign Admin role: {errors}");
        }
    }
}


// ============================================================
// Configure the HTTP request pipeline.
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");

    // The default HSTS value is 30 days.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();