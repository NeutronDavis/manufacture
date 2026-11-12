using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Manufacture.Services
{
    /// <summary>
    /// Rejects direct navigation to application pages when no authenticated
    /// session exists. Public pages (login, logout, error) are excluded.
    /// </summary>
    public class RequireSessionPageFilter : IPageFilter
    {
        private static readonly string[] AnonymousPaths =
        {
            "/Login",
            "/Logout",
            "/Error"
        };

        public void OnPageHandlerSelected(PageHandlerSelectedContext context)
        {
        }

        public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            var path = context.HttpContext.Request.Path;

            if (AnonymousPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            if (!context.HttpContext.Session.GetInt32("UserId").HasValue)
            {
                context.Result = new RedirectToPageResult("/Login");
                return;
            }

            var role = context.HttpContext.Session.GetString("UserRole");
            if (string.Equals(role, "SalesRep", StringComparison.OrdinalIgnoreCase))
            {
                if (path.StartsWithSegments("/Sales/Terminals", StringComparison.OrdinalIgnoreCase))
                {
                    context.Result = new RedirectToPageResult("/Sales/Index");
                }

                return;
            }

            if (!string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var allowedForVendor = path.StartsWithSegments("/Dashboard", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/Sales/Orders/Create", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/Sales/PreOrderSchedule", StringComparison.OrdinalIgnoreCase)
                || AnonymousPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));

            if (!allowedForVendor)
            {
                context.Result = new RedirectToPageResult("/Sales/PreOrderSchedule/Index");
            }
        }

        public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
        {
        }
    }
}
