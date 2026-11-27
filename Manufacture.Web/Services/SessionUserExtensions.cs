using Manufacture.Models.Domain;

namespace Manufacture.Services
{
    /// <summary>
    /// Session helpers for the signed-in user's role. Authorization reads the
    /// numeric role ID so a role rename cannot change behaviour.
    /// </summary>
    public static class SessionUserExtensions
    {
        /// <summary>
        /// Returns the signed-in role ID, or 0 when the session has no role.
        /// Callers must treat 0 as "no access".
        /// </summary>
        public static int GetRoleId(this ISession session) =>
            session.GetInt32(SessionKeys.UserRoleId) ?? 0;

        /// <summary>Role machine name for display only.</summary>
        public static string GetRoleName(this ISession session) =>
            session.GetString(SessionKeys.UserRole) ?? string.Empty;

        public static bool IsInRole(this ISession session, UserRole role) =>
            session.GetRoleId() == (int)role;
    }
}
