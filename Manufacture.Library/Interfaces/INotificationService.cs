using Manufacture.Models.DTOs;

namespace Manufacture.Library.Interfaces
{
    public interface INotificationService
    {
        NotificationBellDto GetBell(int userId, int take = 6);
        List<NotificationDto> GetForUser(int userId, bool unreadOnly = false, int take = 200);
        int GetUnreadCount(int userId);
        bool MarkRead(long notificationId, int userId);
        int MarkAllRead(int userId);
        int SendGeneral(SendNotificationDto dto, int createdByUserId);
        List<RoleDto> GetAudienceRoles();

        /// <summary>Adds submission notifications to the current DbContext. Does not save.</summary>
        int AddPriceChangeSubmitted(int requestedByUserId, string requestedByName, int submittedCount);

        /// <summary>Adds decision notifications to the current DbContext. Does not save.</summary>
        void AddPriceChangeDecision(PriceChangeDecisionNotificationDto dto);
    }
}
