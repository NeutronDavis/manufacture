using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class RbacPermissionAudit
{
    public long Id { get; set; }

    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public int ModuleId { get; set; }

    public string ModuleKey { get; set; } = null!;

    public int? OldPermissionLevelId { get; set; }

    public string? OldPermissionLevelName { get; set; }

    public int NewPermissionLevelId { get; set; }

    public string NewPermissionLevelName { get; set; } = null!;

    public string ChangedBy { get; set; } = null!;

    public DateTime ChangedAt { get; set; }
}
