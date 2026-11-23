using Manufacture.Models.DTOs;

namespace Manufacture.Library.Calculations
{
    /// <summary>
    /// Eligibility rules for linking a sales route sheet to a delivery dispatch.
    /// <para>
    /// Only a newly created (Planned) delivery dispatch can be claimed, and a
    /// dispatch can be claimed by exactly one route sheet.
    /// </para>
    /// </summary>
    public static class FleetDispatchRules
    {
        public static bool IsAvailableForRouteSheet(FleetDispatchStatus status, bool alreadyLinked)
            => status == FleetDispatchStatus.Planned && !alreadyLinked;
    }
}
