using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Data;
using Microsoft.AspNetCore.Identity;

namespace SmartDeviceMatch.Filters
{
    public class RequireProfileFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public RequireProfileFilter(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            // ==========================================
            // Allow Admin users to bypass profile check
            // ==========================================

            if (context.HttpContext.User.IsInRole("Admin"))
            {
                await next();
                return;
            }


            // ==========================================
            // Check profile for normal authenticated users
            // ==========================================

            if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(
                    context.HttpContext.User);

                if (userId != null)
                {
                    var hasProfile = await _context.AppUsers
                        .AnyAsync(x =>
                            x.IdentityUserId == userId);


                    var controller =
                        context.RouteData.Values["controller"]?.ToString();

                    var action =
                        context.RouteData.Values["action"]?.ToString();


                    // ==========================================
                    // If profile does not exist, force the user
                    // to complete Profile/Create
                    // ==========================================

                    if (!hasProfile &&
                        !(controller == "Profile" &&
                          action == "Create"))
                    {
                        context.Result =
                            new RedirectToActionResult(
                                "Create",
                                "Profile",
                                null);

                        return;
                    }
                }
            }


            // ==========================================
            // Continue normally
            // ==========================================

            await next();
        }
    }
}