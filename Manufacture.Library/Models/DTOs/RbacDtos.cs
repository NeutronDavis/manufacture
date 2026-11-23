namespace Manufacture.Models.DTOs
{
    public class RbacModuleDto
    {
        public int Id { get; set; }
        public string ModuleKey { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class RbacPermissionLevelDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
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
    }

    public class RbacMatrixCellDto
    {
        public int ModuleId { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string RoleDisplayName { get; set; } = string.Empty;
        public int PermissionLevelId { get; set; }
        public string PermissionLevelName { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
    }

    public class RbacMatrixRowDto
    {
        public RbacModuleDto Module { get; set; } = new();
        public List<RbacMatrixCellDto> Cells { get; set; } = new();
    }

    public class RbacMatrixDto
    {
        public List<RoleDto> Roles { get; set; } = new();
        public List<RbacModuleDto> Modules { get; set; } = new();
        public List<RbacPermissionLevelDto> PermissionLevels { get; set; } = new();
        public List<RbacMatrixRowDto> Rows { get; set; } = new();
        public DateTime? LastUpdatedAt { get; set; }
        public string? LastUpdatedBy { get; set; }
    }

    public class RbacCellInputDto
    {
        public int ModuleId { get; set; }
        public int RoleId { get; set; }
        public int PermissionLevelId { get; set; }
    }

    public class RbacSaveInputDto
    {
        public List<RbacCellInputDto> Cells { get; set; } = new();
    }
}
