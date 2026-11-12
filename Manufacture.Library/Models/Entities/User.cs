using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class User
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int RoleId { get; set; }

    public int? EmployeeId { get; set; }

    public int? CustomerId { get; set; }

    public string PinCodeHash { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public Guid SecurityStamp { get; set; }

    public bool MustChangePassword { get; set; }

    public string? PosTerminalCode { get; set; }

    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual Customer? Customer { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual ICollection<PosTerminal> PosTerminals { get; set; } = new List<PosTerminal>();

    public virtual Role Role { get; set; } = null!;

    public virtual SalesRep? SalesRep { get; set; }
}
