namespace Manufacture.Services
{
    /// <summary>
    /// Centralised session keys. Role-based decisions read
    /// <see cref="UserRoleId"/>; <see cref="UserRole"/> is kept for display only.
    /// </summary>
    public static class SessionKeys
    {
        public const string UserId = "UserId";
        public const string UserName = "UserName";
        public const string UserEmail = "UserEmail";

        /// <summary>Numeric dbo.Roles.Id of the signed-in user's role.</summary>
        public const string UserRoleId = "UserRoleId";

        /// <summary>Role machine name, used only for display text.</summary>
        public const string UserRole = "UserRole";
    }
}
