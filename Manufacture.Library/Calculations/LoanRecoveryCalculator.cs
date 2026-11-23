namespace Manufacture.Library.Calculations
{
    /// <summary>
    /// Centralised salary-advance recovery maths.
    /// <para>
    /// The policy percentage is applied to the original principal, not the
    /// reducing balance. When the remaining balance is smaller than the normal
    /// monthly recovery, the entire remaining balance is recovered in that run.
    /// </para>
    /// </summary>
    public static class LoanRecoveryCalculator
    {
        public static decimal CalculateScheduledRecovery(
            decimal principalAmount,
            decimal totalRepaid,
            decimal monthlyRecoveryPercent)
        {
            var remaining = Math.Max(0m, principalAmount - totalRepaid);
            return CalculateForRemaining(principalAmount, remaining, monthlyRecoveryPercent);
        }

        public static decimal CalculateForRemaining(
            decimal principalAmount,
            decimal remainingBalance,
            decimal monthlyRecoveryPercent)
        {
            if (remainingBalance <= 0m || principalAmount <= 0m || monthlyRecoveryPercent <= 0m)
            {
                return 0m;
            }

            var scheduled = Math.Round(
                principalAmount * monthlyRecoveryPercent / 100m,
                2,
                MidpointRounding.AwayFromZero);

            return Math.Min(scheduled, remainingBalance);
        }
    }
}
