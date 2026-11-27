using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using NotificationEntity = Manufacture.Models.Entities.Notification;

namespace Manufacture.Services;

public sealed class NotificationService : INotificationService
{
    private readonly ManufactureDbContext _db;

    public NotificationService(ManufactureDbContext db)
    {
        _db = db;
    }

    public NotificationBellDto GetBell(int userId, int take = 6)
    {
        if (userId <= 0)
        {
            return new NotificationBellDto();
        }

        var query = _db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId && !n.IsRead);

        return new NotificationBellDto
        {
            UnreadCount = query.Count(),
            Items = query
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Take(Math.Clamp(take, 1, 20))
                .ToList()
                .Select(Map)
                .ToList()
        };
    }

    public List<NotificationDto> GetForUser(int userId, bool unreadOnly = false, int take = 200)
    {
        if (userId <= 0)
        {
            return new List<NotificationDto>();
        }

        var query = _db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Take(Math.Clamp(take, 1, 500))
            .ToList()
            .Select(Map)
            .ToList();
    }

    public int GetUnreadCount(int userId)
    {
        return userId <= 0
            ? 0
            : _db.Notifications.Count(n => n.RecipientUserId == userId && !n.IsRead);
    }

    public bool MarkRead(long notificationId, int userId)
    {
        var notification = _db.Notifications
            .FirstOrDefault(n => n.Id == notificationId && n.RecipientUserId == userId);

        if (notification == null)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            _db.SaveChanges();
        }

        return true;
    }

    public int MarkAllRead(int userId)
    {
        if (userId <= 0)
        {
            return 0;
        }

        var unread = _db.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ToList();

        if (unread.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        _db.SaveChanges();
        return unread.Count;
    }

    public List<RoleDto> GetAudienceRoles()
    {
        return _db.Roles
            .AsNoTracking()
            .Where(r => r.Id != (int)UserRole.Customer)
            .OrderBy(r => r.DisplayName)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                DisplayName = r.DisplayName,
                Description = r.Description,
                IsSystemRole = r.IsSystemRole
            })
            .ToList();
    }

    public int SendGeneral(SendNotificationDto dto, int createdByUserId)
    {
        var title = (dto.Title ?? string.Empty).Trim();
        var message = (dto.Message ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Enter a notification title.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new InvalidOperationException("Enter a notification message.");
        }

        var recipients = ResolveRecipients(dto.AudienceRoleId);
        if (recipients.Count == 0)
        {
            return 0;
        }

        var link = NormalizeLink(dto.LinkUrl);
        var now = DateTime.UtcNow;

        foreach (var recipientId in recipients)
        {
            _db.Notifications.Add(new NotificationEntity
            {
                RecipientUserId = recipientId,
                Title = title[..Math.Min(title.Length, 200)],
                Message = message[..Math.Min(message.Length, 1000)],
                LinkUrl = link,
                Type = (int)NotificationType.General,
                IsRead = false,
                CreatedByUserId = createdByUserId > 0 ? createdByUserId : null,
                CreatedAt = now
            });
        }

        _db.SaveChanges();
        return recipients.Count;
    }

    public int AddPriceChangeSubmitted(int requestedByUserId, string requestedByName, int submittedCount)
    {
        if (submittedCount <= 0)
        {
            return 0;
        }

        var recipients = _db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.RoleId == (int)UserRole.SuperAdmin)
            .Select(u => u.Id)
            .ToList();

        if (recipients.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        var title = "Price change approval needed";
        var message = NotificationText.PriceChangeSubmittedMessage(requestedByName, submittedCount);

        foreach (var recipientId in recipients)
        {
            _db.Notifications.Add(new NotificationEntity
            {
                RecipientUserId = recipientId,
                Title = title,
                Message = message,
                LinkUrl = "/Production/Pricing",
                Type = (int)NotificationType.PriceChangeSubmitted,
                IsRead = false,
                CreatedByUserId = requestedByUserId > 0 ? requestedByUserId : null,
                CreatedAt = now
            });
        }

        return recipients.Count;
    }

    public void AddPriceChangeDecision(PriceChangeDecisionNotificationDto dto)
    {
        var recipients = ResolveDecisionRecipients(dto.RequestedByUserId);
        if (recipients.Count == 0)
        {
            return;
        }

        var type = dto.Approved ? NotificationType.PriceChangeApproved : NotificationType.PriceChangeRejected;
        var title = dto.Approved ? "Price change approved" : "Price change rejected";
        var message = dto.Approved
            ? NotificationText.PriceChangeApprovedMessage(dto.InventoryItemName, dto.PreviousUnitCost, dto.RequestedUnitCost)
            : NotificationText.PriceChangeRejectedMessage(dto.InventoryItemName, dto.Reason);

        var now = DateTime.UtcNow;
        foreach (var recipientId in recipients)
        {
            _db.Notifications.Add(new NotificationEntity
            {
                RecipientUserId = recipientId,
                Title = title,
                Message = message,
                LinkUrl = $"/Production/Pricing/History/{dto.RequestId}",
                Type = (int)type,
                IsRead = false,
                CreatedAt = now
            });
        }
    }

    private List<int> ResolveDecisionRecipients(int? requestedByUserId)
    {
        if (requestedByUserId.HasValue && requestedByUserId.Value > 0)
        {
            var requesterIsActive = _db.Users
                .AsNoTracking()
                .Any(u => u.Id == requestedByUserId.Value && u.IsActive);

            if (requesterIsActive)
            {
                return new List<int> { requestedByUserId.Value };
            }
        }

        return _db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.RoleId == (int)UserRole.ProductionManager)
            .Select(u => u.Id)
            .ToList();
    }

    private List<int> ResolveRecipients(int? roleId)
    {
        if (!roleId.HasValue)
        {
            return _db.Users
                .AsNoTracking()
                .Where(u => u.IsActive && u.RoleId != (int)UserRole.Customer)
                .Select(u => u.Id)
                .ToList();
        }

        if (roleId.Value == (int)UserRole.Customer)
        {
            throw new InvalidOperationException("Customers cannot receive in-app notifications.");
        }

        var roleExists = _db.Roles.Any(r => r.Id == roleId.Value);
        if (!roleExists)
        {
            throw new InvalidOperationException("Select a valid notification audience.");
        }

        return _db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.RoleId == roleId.Value)
            .Select(u => u.Id)
            .ToList();
    }

    private static string? NormalizeLink(string? linkUrl)
    {
        var link = (linkUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(link))
        {
            return null;
        }

        if (!link.StartsWith('/') || link.StartsWith("//", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Notification links must be app-relative paths such as /Production/Pricing.");
        }

        return link.Length > 300 ? link[..300] : link;
    }

    private static NotificationDto Map(NotificationEntity notification) => new()
    {
        Id = notification.Id,
        Title = notification.Title,
        Message = notification.Message,
        LinkUrl = notification.LinkUrl,
        Type = (NotificationType)notification.Type,
        TypeLabel = NotificationText.TypeLabel((NotificationType)notification.Type),
        IsRead = notification.IsRead,
        ReadAt = notification.ReadAt,
        CreatedAt = notification.CreatedAt
    };
}
