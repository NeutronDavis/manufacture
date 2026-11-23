using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class JobRole
{
    public int Id { get; set; }

    public int? DepartmentId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Department? Department { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
