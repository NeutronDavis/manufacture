using System.ComponentModel.DataAnnotations;

namespace Manufacture.Models.DTOs
{
    public class LoginDto
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
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
        [Required]
        public string Role { get; set; } = "SalesRep";
        [StringLength(6, MinimumLength = 6, ErrorMessage = "PIN must be exactly 6 digits")]
        public string PinCode { get; set; } = "123456";
        public string Password { get; set; } = "Password123!";
        public string? PosTerminalId { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class EmployeeDto
    {
        public int Id { get; set; }
        [Required]
        public string FullName { get; set; } = string.Empty;
        [Required]
        public string StaffCode { get; set; } = string.Empty;
        [Required]
        public string Department { get; set; } = "Production";
        [Required]
        public string Designation { get; set; } = string.Empty;
        [Required, Phone]
        public string PhoneNumber { get; set; } = string.Empty;
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
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
    }
}
