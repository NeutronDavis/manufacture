using Manufacture.Models.DTOs;
using Manufacture.Services;
using Xunit;

namespace Manufacture.Tests
{
    public class NotificationTextTests
    {
        [Fact]
        public void TypeLabel_MapsEveryNotificationType()
        {
            Assert.Equal("General", NotificationText.TypeLabel(NotificationType.General));
            Assert.Equal("Price Approval", NotificationText.TypeLabel(NotificationType.PriceChangeSubmitted));
            Assert.Equal("Price Approved", NotificationText.TypeLabel(NotificationType.PriceChangeApproved));
            Assert.Equal("Price Rejected", NotificationText.TypeLabel(NotificationType.PriceChangeRejected));
        }

        [Fact]
        public void PriceChangeSubmittedMessage_UsesSingularAndPlural()
        {
            Assert.Contains("1 price change", NotificationText.PriceChangeSubmittedMessage("Ada", 1));
            Assert.Contains("3 price changes", NotificationText.PriceChangeSubmittedMessage("Ada", 3));
            Assert.Contains("Ada", NotificationText.PriceChangeSubmittedMessage("Ada", 2));
        }

        [Fact]
        public void PriceChangeSubmittedMessage_FallsBackWhenRequesterMissing()
        {
            Assert.Contains("A production manager", NotificationText.PriceChangeSubmittedMessage("", 2));
        }

        [Fact]
        public void PriceChangeApprovedMessage_IncludesOldAndNewPrices()
        {
            var message = NotificationText.PriceChangeApprovedMessage("Sugar", 1200m, 1350m);

            Assert.Contains("Sugar", message);
            Assert.Contains("1,200.00", message);
            Assert.Contains("1,350.00", message);
            Assert.Contains("approved", message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void PriceChangeRejectedMessage_IncludesReasonWhenProvided()
        {
            var withReason = NotificationText.PriceChangeRejectedMessage("Sugar", "Bulk discount not approved");
            var withoutReason = NotificationText.PriceChangeRejectedMessage("Sugar", null);

            Assert.Contains("Bulk discount not approved", withReason);
            Assert.Contains("rejected", withReason, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Reason:", withoutReason);
        }
    }
}
