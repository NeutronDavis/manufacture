using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class Role
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public bool IsSystemRole { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<RbacRolePermission> RbacRolePermissions { get; set; } = new List<RbacRolePermission>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
