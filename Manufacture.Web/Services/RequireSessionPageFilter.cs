using Manufacture.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Manufacture.Services
{
    /// <summary>
    /// Rejects direct navigation to application pages when no authenticated
    /// session exists. Public pages (login, logout, error) are excluded.
    /// Role routing keys on dbo.Roles.Id, never on the changeable role name.
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

            if (!context.HttpContext.Session.GetInt32(SessionKeys.UserId).HasValue)
            {
                context.Result = new RedirectToPageResult("/Login");
                return;
            }

            var roleId = context.HttpContext.Session.GetInt32(SessionKeys.UserRoleId) ?? 0;
            if (roleId <= 0)
            {
                // No usable role: fail closed instead of assuming elevated rights.
                context.HttpContext.Session.Clear();
                context.Result = new RedirectToPageResult("/Login");
                return;
            }

            if (roleId == (int)UserRole.SalesRep)
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

            if (roleId == (int)UserRole.StoreManager)
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

            if (roleId == (int)UserRole.ProductionManager)
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

            if (roleId != (int)UserRole.Customer)
            {
                return;
            }

            var allowedForCustomer = path.StartsWithSegments("/Dashboard", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/Sales/Orders/Create", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/Sales/PreOrderSchedule", StringComparison.OrdinalIgnoreCase)
                || AnonymousPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));

            if (!allowedForCustomer)
            {
                context.Result = new RedirectToPageResult("/Sales/PreOrderSchedule/Index");
            }
        }

        public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
        {
        }
    }
}
