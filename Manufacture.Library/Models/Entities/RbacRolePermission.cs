using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class RbacRolePermission
{
    public int Id { get; set; }

    public int RoleId { get; set; }

    public int ModuleId { get; set; }

    public int PermissionLevelId { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string UpdatedBy { get; set; } = null!;

    public virtual RbacModule Module { get; set; } = null!;

    public virtual RbacPermissionLevel PermissionLevel { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
