using Manufacture.Models.DTOs;

namespace Manufacture.Services;

public static class NotificationText
{
    public static string TypeLabel(NotificationType type) => type switch
    {
        NotificationType.PriceChangeSubmitted => "Price Approval",
        NotificationType.PriceChangeApproved => "Price Approved",
        NotificationType.PriceChangeRejected => "Price Rejected",
        _ => "General"
    };

    public static string IconClass(NotificationType type) => type switch
    {
        NotificationType.PriceChangeSubmitted => "fa-solid fa-tags",
        NotificationType.PriceChangeApproved => "fa-solid fa-circle-check",
        NotificationType.PriceChangeRejected => "fa-solid fa-circle-xmark",
        _ => "fa-regular fa-bell"
    };

    public static string ToneClass(NotificationType type) => type switch
    {
        NotificationType.PriceChangeSubmitted => "text-warning",
        NotificationType.PriceChangeApproved => "text-success",
        NotificationType.PriceChangeRejected => "text-error",
        _ => "text-info"
    };

    public static string PriceChangeSubmittedMessage(string? requesterName, int count)
    {
        var name = string.IsNullOrWhiteSpace(requesterName) ? "A production manager" : requesterName.Trim();
        var itemWord = count == 1 ? "price change" : "price changes";
        return $"{name} submitted {count} {itemWord} for approval.";
    }

    public static string PriceChangeApprovedMessage(string itemName, decimal previousCost, decimal newCost) =>
        $"{itemName} price approved: ₦{previousCost:N2} → ₦{newCost:N2}.";

    public static string PriceChangeRejectedMessage(string itemName, string? reason)
    {
        var suffix = string.IsNullOrWhiteSpace(reason) ? string.Empty : $" Reason: {reason.Trim()}";
        return $"{itemName} price change was rejected.{suffix}";
    }
}
