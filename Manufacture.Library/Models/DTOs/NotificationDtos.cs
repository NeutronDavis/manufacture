using System.ComponentModel.DataAnnotations;

namespace Manufacture.Models.DTOs
{
    public enum NotificationType
    {
        General = 0,
        PriceChangeSubmitted = 1,
        PriceChangeApproved = 2,
        PriceChangeRejected = 3
    }

    public class NotificationDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? LinkUrl { get; set; }
        public NotificationType Type { get; set; }
        public string TypeLabel { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationBellDto
    {
        public int UnreadCount { get; set; }
        public List<NotificationDto> Items { get; set; } = new();
    }

    public class SendNotificationDto
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? LinkUrl { get; set; }

        /// <summary>Null for every active non-Customer user, otherwise the role ID to target.</summary>
        public int? AudienceRoleId { get; set; }
    }

    public class PriceChangeDecisionNotificationDto
    {
        public int RequestId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public int InventoryItemId { get; set; }
        public string InventoryItemName { get; set; } = string.Empty;
        public decimal PreviousUnitCost { get; set; }
        public decimal RequestedUnitCost { get; set; }
        public int? RequestedByUserId { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public bool Approved { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}
