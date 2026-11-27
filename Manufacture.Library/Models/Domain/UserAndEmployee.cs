namespace Manufacture.Models.Domain
{
    /// <summary>
    /// Stable role identifiers. The numeric values are pinned to dbo.Roles.Id
    /// (see the role seed) and must never be reordered or renumbered, because
    /// navigation, authorization and RBAC decisions key on these IDs rather
    /// than on the changeable role name.
    /// </summary>
    public enum UserRole
    {
        SuperAdmin = 1,
        ProductionManager = 2,
        StoreManager = 3,
        SalesRep = 4,
        Customer = 5,
        HrPayrollManager = 6
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
        public string BankName { get; set; } = string.Empty;
        public string BankAccountNumber { get; set; } = string.Empty;
        public string EmploymentStatus { get; set; } = "Active"; // Active, OnLeave, Inactive
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
