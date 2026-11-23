using System;

namespace Manufacture.Models.Entities;

public partial class ProductionShift
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public TimeSpan? StartTime { get; set; }

    public TimeSpan? EndTime { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
