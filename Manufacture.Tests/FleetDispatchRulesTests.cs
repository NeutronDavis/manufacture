using Manufacture.Library.Calculations;
using Manufacture.Models.DTOs;
using Xunit;

namespace Manufacture.Tests
{
    public class FleetDispatchRulesTests
    {
        [Fact]
        public void PlannedUnlinkedDispatch_IsAvailable()
        {
            Assert.True(FleetDispatchRules.IsAvailableForRouteSheet(FleetDispatchStatus.Planned, alreadyLinked: false));
        }

        [Fact]
        public void PlannedButAlreadyLinkedDispatch_IsNotAvailable()
        {
            Assert.False(FleetDispatchRules.IsAvailableForRouteSheet(FleetDispatchStatus.Planned, alreadyLinked: true));
        }

        [Theory]
        [InlineData(FleetDispatchStatus.Loading)]
        [InlineData(FleetDispatchStatus.InTransit)]
        [InlineData(FleetDispatchStatus.Delivered)]
        [InlineData(FleetDispatchStatus.Completed)]
        [InlineData(FleetDispatchStatus.Cancelled)]
        public void NonPlannedDispatch_IsNotAvailable(FleetDispatchStatus status)
        {
            Assert.False(FleetDispatchRules.IsAvailableForRouteSheet(status, alreadyLinked: false));
        }
    }
}
