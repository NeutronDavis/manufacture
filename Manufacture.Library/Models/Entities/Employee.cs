using System;
using System.Collections.Generic;

namespace Manufacture.Models.Entities;

public partial class Employee
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string StaffCode { get; set; } = null!;

    public int? DepartmentId { get; set; }

    public int? JobRoleId { get; set; }

    public string Department { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? PassportUrl { get; set; }

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

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public DateOnly? TerminationDate { get; set; }

    public string TerminationReason { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<CharterBooking> CharterBookings { get; set; } = new List<CharterBooking>();

    public virtual Department? DepartmentNavigation { get; set; }

    public virtual ICollection<FuelLog> FuelLogs { get; set; } = new List<FuelLog>();

    public virtual JobRole? JobRole { get; set; }

    public virtual ICollection<PayrollDeductionLine> PayrollDeductionLines { get; set; } = new List<PayrollDeductionLine>();

    public virtual ICollection<PayrollRecord> PayrollRecords { get; set; } = new List<PayrollRecord>();

    public virtual ICollection<PosTerminal> PosTerminals { get; set; } = new List<PosTerminal>();

    public virtual ICollection<SalaryAdvance> SalaryAdvances { get; set; } = new List<SalaryAdvance>();

    public virtual ICollection<SalesRep> SalesReps { get; set; } = new List<SalesRep>();

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
