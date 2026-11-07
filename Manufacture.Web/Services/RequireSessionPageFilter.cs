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
            }
        }

        public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
        {
        }
    }
}
