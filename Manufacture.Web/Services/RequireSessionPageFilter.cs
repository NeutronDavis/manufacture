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
                // Sales reps do not see the Sales hub; the order workspace is their entry point.
                if (path.StartsWithSegments("/Sales/Terminals", StringComparison.OrdinalIgnoreCase))
                {
                    context.Result = new RedirectToPageResult("/Sales/Orders/Index");
                    return;
                }

                // Production is fully outside the SalesRep scope, regardless of any
                // legacy Recipes view permission.
                if (path.StartsWithSegments("/Production", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWithSegments("/Recipes", StringComparison.OrdinalIgnoreCase))
                {
                    context.Result = new RedirectToPageResult("/Dashboard/Index");
                    return;
                }

                return;
            }

            if (string.Equals(role, "StoreManager", StringComparison.OrdinalIgnoreCase))
            {
                if (path.StartsWithSegments("/Inventory/Suppliers", StringComparison.OrdinalIgnoreCase))
                {
                    context.Result = new RedirectToPageResult("/Inventory/Index");
                    return;
                }

                if (path.StartsWithSegments("/Production", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWithSegments("/Recipes", StringComparison.OrdinalIgnoreCase))
                {
                    context.Result = new RedirectToPageResult("/Dashboard/Index");
                }

                return;
            }

            if (string.Equals(role, "ProductionManager", StringComparison.OrdinalIgnoreCase))
            {
                if (path.StartsWithSegments("/Logistics", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWithSegments("/Fleet", StringComparison.OrdinalIgnoreCase))
                {
                    context.Result = new RedirectToPageResult("/Dashboard/Index");
                    return;
                }

                if (path.StartsWithSegments("/Sales/Orders/Create", StringComparison.OrdinalIgnoreCase))
                {
                    context.Result = new RedirectToPageResult("/Sales/Orders/Index");
                    return;
                }

                if (path.StartsWithSegments("/Inventory/Items/Create", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWithSegments("/Inventory/Items/Edit", StringComparison.OrdinalIgnoreCase))
                {
                    context.Result = new RedirectToPageResult("/Inventory/Index");
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
