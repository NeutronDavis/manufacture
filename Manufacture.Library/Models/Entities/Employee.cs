using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class Employee
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string StaffCode { get; set; } = null!;

    public string Department { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string BankName { get; set; } = null!;

    public string BankAccountNumber { get; set; } = null!;

    public string EmploymentStatus { get; set; } = null!;

    public decimal BaseSalary { get; set; }

    public string EmergencyContactName { get; set; } = null!;

    public string EmergencyContactPhone { get; set; } = null!;

    public string EmergencyRelationship { get; set; } = null!;

    public string GuarantorName { get; set; } = null!;

    public string GuarantorPhone { get; set; } = null!;

    public string GuarantorAddress { get; set; } = null!;

    public string GuarantorOccupation { get; set; } = null!;

    public DateOnly EmploymentDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<PayrollDeductionLine> PayrollDeductionLines { get; set; } = new List<PayrollDeductionLine>();

    public virtual ICollection<PayrollRecord> PayrollRecords { get; set; } = new List<PayrollRecord>();

    public virtual ICollection<SalaryAdvance> SalaryAdvances { get; set; } = new List<SalaryAdvance>();

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
