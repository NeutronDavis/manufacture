using Manufacture.Library.Calculations;
using Xunit;

namespace Manufacture.Tests
{
    public class LoanRecoveryCalculatorTests
    {
        [Fact]
        public void ScheduledRecovery_UsesOriginalPrincipal()
        {
            var amount = LoanRecoveryCalculator.CalculateScheduledRecovery(
                principalAmount: 60_000m,
                totalRepaid: 12_000m,
                monthlyRecoveryPercent: 20m);

            Assert.Equal(12_000m, amount);
        }

        [Fact]
        public void FinalInstallment_RecoversEntireRemainingBalance_WhenBelowPolicyAmount()
        {
            var amount = LoanRecoveryCalculator.CalculateScheduledRecovery(
                principalAmount: 60_000m,
                totalRepaid: 59_000m,
                monthlyRecoveryPercent: 20m);

            Assert.Equal(1_000m, amount);
        }

        [Fact]
        public void FinalInstallment_RecoversExactRemainingBalance()
        {
            var amount = LoanRecoveryCalculator.CalculateScheduledRecovery(
                principalAmount: 30_000m,
                totalRepaid: 28_000m,
                monthlyRecoveryPercent: 20m);

            Assert.Equal(2_000m, amount);
        }

        [Fact]
        public void FullyRepaidLoan_ReturnsZero()
        {
            var amount = LoanRecoveryCalculator.CalculateScheduledRecovery(
                principalAmount: 60_000m,
                totalRepaid: 60_000m,
                monthlyRecoveryPercent: 20m);

            Assert.Equal(0m, amount);
        }
    }
}
