using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class RbacModule
{
    public int Id { get; set; }

    public string ModuleKey { get; set; } = null!;

    public string ModuleName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<RbacRolePermission> RbacRolePermissions { get; set; } = new List<RbacRolePermission>();
}
