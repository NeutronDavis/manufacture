using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Manufacture.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Manufacture.Pages.Notifications
{
    public class IndexModel : PageModel
    {
        private readonly INotificationService _notifications;

        public IndexModel(INotificationService notifications)
        {
            _notifications = notifications;
        }

        public List<NotificationDto> Items { get; set; } = new();
        public int UnreadCount { get; set; }
        public bool UnreadOnly { get; set; }
        public bool IsSuperAdmin { get; set; }
        public List<RoleDto> AudienceRoles { get; set; } = new();
        public int CurrentUserId { get; set; }
        public int UserRoleId { get; set; }

        [BindProperty]
        public SendNotificationDto SendInput { get; set; } = new();

        public IActionResult OnGet(bool unreadOnly = false)
        {
            if (!LoadUser())
            {
                return RedirectToPage("/Login");
            }

            if (UserRoleId == (int)UserRole.Customer)
            {
                return RedirectToPage("/Sales/PreOrderSchedule/Index");
            }

            UnreadOnly = unreadOnly;
            Items = _notifications.GetForUser(CurrentUserId, unreadOnly);
            UnreadCount = _notifications.GetUnreadCount(CurrentUserId);
            IsSuperAdmin = UserRoleId == (int)UserRole.SuperAdmin;
            if (IsSuperAdmin)
            {
                AudienceRoles = _notifications.GetAudienceRoles();
            }

            return Page();
        }

        public IActionResult OnGetOpen(long id)
        {
            if (!LoadUser())
            {
                return RedirectToPage("/Login");
            }

            var notification = _notifications.GetForUser(CurrentUserId, unreadOnly: false, take: 500)
                .FirstOrDefault(n => n.Id == id);

            if (notification == null)
            {
                TempData["ErrorMessage"] = "Notification not found.";
                return RedirectToPage();
            }

            _notifications.MarkRead(id, CurrentUserId);
            if (!string.IsNullOrWhiteSpace(notification.LinkUrl)
                && notification.LinkUrl.StartsWith('/')
                && !notification.LinkUrl.StartsWith("//", StringComparison.Ordinal))
            {
                return Redirect(notification.LinkUrl);
            }

            return RedirectToPage();
        }

        public IActionResult OnPostMarkAllRead()
        {
            if (!LoadUser())
            {
                return RedirectToPage("/Login");
            }

            var count = _notifications.MarkAllRead(CurrentUserId);
            TempData["SuccessMessage"] = count == 0
                ? "You have no unread notifications."
                : $"Marked {count} notification{(count == 1 ? "" : "s")} as read.";
            return RedirectToPage();
        }

        public IActionResult OnPostSendGeneral()
        {
            if (!LoadUser())
            {
                return RedirectToPage("/Login");
            }

            if (!IsSuperAdmin)
            {
                TempData["ErrorMessage"] = "Only a SuperAdmin can send general notifications.";
                return RedirectToPage();
            }

            if (!ModelState.IsValid)
            {
                Items = _notifications.GetForUser(CurrentUserId);
                UnreadCount = _notifications.GetUnreadCount(CurrentUserId);
                IsSuperAdmin = true;
                AudienceRoles = _notifications.GetAudienceRoles();
                return Page();
            }

            try
            {
                var sent = _notifications.SendGeneral(SendInput, CurrentUserId);
                TempData["SuccessMessage"] = sent == 0
                    ? "No active recipients matched that audience."
                    : $"Notification sent to {sent} recipient{(sent == 1 ? "" : "s")}.";
                return RedirectToPage();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                Items = _notifications.GetForUser(CurrentUserId);
                UnreadCount = _notifications.GetUnreadCount(CurrentUserId);
                IsSuperAdmin = true;
                AudienceRoles = _notifications.GetAudienceRoles();
                return Page();
            }
        }

        private bool LoadUser()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return false;
            }

            CurrentUserId = userId.Value;
            UserRoleId = HttpContext.Session.GetRoleId();
            return true;
        }
    }
}
