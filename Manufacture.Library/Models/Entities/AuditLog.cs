using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class AuditLog
{
    public long Id { get; set; }

    public int? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityName { get; set; } = null!;

    public string? EntityId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }

    public DateTime OccurredAt { get; set; }

    public virtual User? User { get; set; }
}
