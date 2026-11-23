namespace Manufacture.Library.Calculations
{
    /// <summary>
    /// Centralised maths for dispatch-level supermarket debt.
    /// <para>
    /// A route sheet keeps one debt row per supermarket account. Re-saving the
    /// reconciliation panel must never double-count, so the account balance is
    /// only moved by the difference between the target debt and the amount the
    /// row has already applied (<c>AppliedCredit</c>).
    /// </para>
    /// <para>
    /// Checked ("settled") means the supermarket paid in full for this dispatch,
    /// so its target debt is zero and any previously applied amount is reversed.
    /// </para>
    /// </summary>
    public static class RouteSupermarketDebtCalculator
    {
        public static decimal CalculateCreditDelta(
            decimal currentlyApplied,
            bool isSettled,
            decimal requestedDebt)
        {
            var target = isSettled ? 0m : Math.Max(0m, requestedDebt);
            return target - Math.Max(0m, currentlyApplied);
        }
    }
}
