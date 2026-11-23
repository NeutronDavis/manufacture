using Manufacture.Library.Calculations;
using Xunit;

namespace Manufacture.Tests
{
    public class RouteSupermarketDebtCalculatorTests
    {
        [Fact]
        public void UnsettledDebt_FirstSave_AppliesFullAmount()
        {
            var delta = RouteSupermarketDebtCalculator.CalculateCreditDelta(
                currentlyApplied: 0m,
                isSettled: false,
                requestedDebt: 50000m);

            Assert.Equal(50000m, delta);
        }

        [Fact]
        public void SettledRow_ReversesPreviouslyAppliedDebt()
        {
            var delta = RouteSupermarketDebtCalculator.CalculateCreditDelta(
                currentlyApplied: 50000m,
                isSettled: true,
                requestedDebt: 50000m);

            Assert.Equal(-50000m, delta);
        }

        [Fact]
        public void ResavingSameAmount_ProducesNoDelta()
        {
            var delta = RouteSupermarketDebtCalculator.CalculateCreditDelta(
                currentlyApplied: 50000m,
                isSettled: false,
                requestedDebt: 50000m);

            Assert.Equal(0m, delta);
        }

        [Fact]
        public void IncreasingDebt_AppliesOnlyTheDifference()
        {
            var delta = RouteSupermarketDebtCalculator.CalculateCreditDelta(
                currentlyApplied: 50000m,
                isSettled: false,
                requestedDebt: 70000m);

            Assert.Equal(20000m, delta);
        }

        [Fact]
        public void ReducingDebt_ReversesOnlyTheDifference()
        {
            var delta = RouteSupermarketDebtCalculator.CalculateCreditDelta(
                currentlyApplied: 70000m,
                isSettled: false,
                requestedDebt: 50000m);

            Assert.Equal(-20000m, delta);
        }

        [Fact]
        public void NegativeRequestedDebt_IsTreatedAsZero()
        {
            var delta = RouteSupermarketDebtCalculator.CalculateCreditDelta(
                currentlyApplied: 12000m,
                isSettled: false,
                requestedDebt: -500m);

            Assert.Equal(-12000m, delta);
        }

        [Fact]
        public void NegativeAppliedCredit_IsTreatedAsZero()
        {
            var delta = RouteSupermarketDebtCalculator.CalculateCreditDelta(
                currentlyApplied: -5000m,
                isSettled: false,
                requestedDebt: 15000m);

            Assert.Equal(15000m, delta);
        }

        [Fact]
        public void SettledWithNoPreviousDebt_ProducesNoDelta()
        {
            var delta = RouteSupermarketDebtCalculator.CalculateCreditDelta(
                currentlyApplied: 0m,
                isSettled: true,
                requestedDebt: 25000m);

            Assert.Equal(0m, delta);
        }
    }
}
