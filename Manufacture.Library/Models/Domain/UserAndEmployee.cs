namespace Manufacture.Models.Entities
{
    public enum UserRole
    {
        SuperAdmin,
        ProductionManager,
        StoreManager,
        SalesRep,
        Vendor,
        HrPayrollManager
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = UserRole.SalesRep.ToString();
        public string PinCode { get; set; } = "123456"; // 6-digit numeric login PIN
        public string Password { get; set; } = "Password123!";
        public string? PosTerminalId { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Employee
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string StaffCode { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty; // Production, Sales, Logistics, Store, Admin
        public string Designation { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }

        // Emergency Contact
        public string EmergencyContactName { get; set; } = string.Empty;
        public string EmergencyContactPhone { get; set; } = string.Empty;
        public string EmergencyRelationship { get; set; } = string.Empty;

        // Guarantor Records
        public string GuarantorName { get; set; } = string.Empty;
        public string GuarantorPhone { get; set; } = string.Empty;
        public string GuarantorAddress { get; set; } = string.Empty;
        public string GuarantorOccupation { get; set; } = string.Empty;

        public DateTime EmploymentDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
