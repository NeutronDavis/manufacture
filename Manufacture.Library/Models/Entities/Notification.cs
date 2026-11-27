using System;

namespace Manufacture.Models.Entities;

public partial class Notification
{
    public long Id { get; set; }

    public int RecipientUserId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? LinkUrl { get; set; }

    public int Type { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual User? CreatedByUser { get; set; }

    public virtual User RecipientUser { get; set; } = null!;
}
