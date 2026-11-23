using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class RbacPermissionLevel
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int Rank { get; set; }

    public bool CanView { get; set; }

    public bool CanCreate { get; set; }

    public bool CanEdit { get; set; }

    public bool CanDelete { get; set; }

    public bool CanApprove { get; set; }

    public bool CanPrint { get; set; }

    public bool CanManage { get; set; }

    public bool CanRequest { get; set; }

    public bool CanIssue { get; set; }

    public bool CanDisburse { get; set; }

    public bool CanBookOwn { get; set; }

    public bool IsNone { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<RbacRolePermission> RbacRolePermissions { get; set; } = new List<RbacRolePermission>();
}
