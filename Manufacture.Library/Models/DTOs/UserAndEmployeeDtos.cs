using System.ComponentModel.DataAnnotations;

namespace Manufacture.Models.DTOs
{
    public class LoginDto
    {
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(256, ErrorMessage = "Email address is too long.")]
        public string? Email { get; set; }

        public string? Password { get; set; }

        [RegularExpression(@"^\d{6}$", ErrorMessage = "PIN must be exactly 6 digits.")]
        public string? PinCode { get; set; } // For 6-digit numeric login

        public bool UsePinMode { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Select a system role.")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "Select the employee record for this user.")]
        public int? EmployeeId { get; set; }

        public string? EmployeeName { get; set; }
        public string? EmployeeDepartment { get; set; }
        public string? EmployeeDesignation { get; set; }

        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }

        /// <summary>Role machine name, e.g. SuperAdmin. Populated for display.</summary>
        public string Role { get; set; } = "SalesRep";

        /// <summary>Friendly role name, e.g. Super Admin. Populated for display.</summary>
        public string RoleDisplayName { get; set; } = string.Empty;

        /// <summary>Optional on edit; required when creating or resetting a PIN.</summary>
        [RegularExpression(@"^(\d{6})?$", ErrorMessage = "PIN must be exactly 6 digits.")]
        public string? PinCode { get; set; }

        /// <summary>Optional on edit; required when creating an account.</summary>
        public string? Password { get; set; }

        public string? PosTerminalId { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public bool HasPin { get; set; }
        public bool HasPassword { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSystemRole { get; set; }
    }

    public class EmployeeDto
    {
        public int Id { get; set; }
        [Required]
        public string FullName { get; set; } = string.Empty;
        [Required]
        public string StaffCode { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public int? JobRoleId { get; set; }
        [Required]
        public string Department { get; set; } = "Production";
        [Required]
        public string Designation { get; set; } = string.Empty;
        [Required, Phone]
        public string PhoneNumber { get; set; } = string.Empty;
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PassportUrl { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BankAccountNumber { get; set; } = string.Empty;
        [Required]
        public string EmploymentStatus { get; set; } = "Active";
        [Range(0, 100000000)]
        public decimal BaseSalary { get; set; }

        // Emergency Contact
        [Required]
        public string EmergencyContactName { get; set; } = string.Empty;
        [Required]
        public string EmergencyContactPhone { get; set; } = string.Empty;
        public string EmergencyRelationship { get; set; } = string.Empty;

        // Guarantor Records
        [Required]
        public string GuarantorName { get; set; } = string.Empty;
        [Required]
        public string GuarantorPhone { get; set; } = string.Empty;
        public string GuarantorAddress { get; set; } = string.Empty;
        public string GuarantorOccupation { get; set; } = string.Empty;

        public DateTime EmploymentDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? TerminationDate { get; set; }
        public string TerminationReason { get; set; } = string.Empty;
    }

    public class EmployeeDependencyDto
    {
        public int PayrollRecords { get; set; }
        public int PayrollDeductionLines { get; set; }
        public int SalaryAdvances { get; set; }
        public int FuelLogs { get; set; }
        public int CharterBookings { get; set; }
        public int VehicleAssignments { get; set; }

        public int TotalRecords =>
            PayrollRecords + PayrollDeductionLines + SalaryAdvances +
            FuelLogs + CharterBookings + VehicleAssignments;

        public bool CanHardDelete => TotalRecords == 0;
    }
}
