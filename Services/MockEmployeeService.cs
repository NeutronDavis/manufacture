using Manufacture.Models.Entities;
using Manufacture.Models.DTOs;

namespace Manufacture.Services
{
    public class MockEmployeeService
    {
        private readonly List<Employee> _employees = new();

        public MockEmployeeService()
        {
            SeedInitialEmployees();
        }

        private void SeedInitialEmployees()
        {
            _employees.AddRange(new[]
            {
                new Employee
                {
                    Id = 1,
                    FullName = "Emeka Obi",
                    StaffCode = "EMP-001",
                    Department = "Production",
                    Designation = "Head Baker / Mixer",
                    PhoneNumber = "+2348034567890",
                    Email = "emeka.obi@bakery.com",
                    Address = "14 Ikorodu Road, Ketu, Lagos",
                    BaseSalary = 120000,
                    EmergencyContactName = "Blessing Obi",
                    EmergencyContactPhone = "+2348034567891",
                    EmergencyRelationship = "Wife",
                    GuarantorName = "Chief Arthur Eze",
                    GuarantorPhone = "+2348021113333",
                    GuarantorAddress = "8 Commercial Ave, Yaba, Lagos",
                    GuarantorOccupation = "Business Executive",
                    EmploymentDate = DateTime.UtcNow.AddYears(-2),
                    IsActive = true
                },
                new Employee
                {
                    Id = 2,
                    FullName = "Folashade Adeyemi",
                    StaffCode = "EMP-002",
                    Department = "Sales",
                    Designation = "Senior Sales Executive",
                    PhoneNumber = "+2348045678901",
                    Email = "folashade.a@bakery.com",
                    Address = "22 Allen Avenue, Ikeja, Lagos",
                    BaseSalary = 95000,
                    EmergencyContactName = "Tunde Adeyemi",
                    EmergencyContactPhone = "+2348045678902",
                    EmergencyRelationship = "Brother",
                    GuarantorName = "Barrister Kayode Salami",
                    GuarantorPhone = "+2348092224444",
                    GuarantorAddress = "15 Toyin Street, Ikeja, Lagos",
                    GuarantorOccupation = "Legal Practitioner",
                    EmploymentDate = DateTime.UtcNow.AddYears(-1),
                    IsActive = true
                },
                new Employee
                {
                    Id = 3,
                    FullName = "Usman Danjuma",
                    StaffCode = "EMP-003",
                    Department = "Logistics",
                    Designation = "Lead Delivery Driver",
                    PhoneNumber = "+2348056789012",
                    Email = "usman.d@bakery.com",
                    Address = "5 Agege Motor Road, Mushin, Lagos",
                    BaseSalary = 85000,
                    EmergencyContactName = "Amina Danjuma",
                    EmergencyContactPhone = "+2348056789013",
                    EmergencyRelationship = "Sister",
                    GuarantorName = "Alhaji Bello Garba",
                    GuarantorPhone = "+2348083335555",
                    GuarantorAddress = "40 Mile 12 Market Rd, Kosofe, Lagos",
                    GuarantorOccupation = "Logistics Contractor",
                    EmploymentDate = DateTime.UtcNow.AddMonths(-18),
                    IsActive = true
                },
                new Employee
                {
                    Id = 4,
                    FullName = "Kelechi Nnamdi",
                    StaffCode = "EMP-004",
                    Department = "Store",
                    Designation = "Store Keeper",
                    PhoneNumber = "+2348067890123",
                    Email = "kelechi.n@bakery.com",
                    Address = "9 Oshodi Expressway, Lagos",
                    BaseSalary = 80000,
                    EmergencyContactName = "Nkechi Nnamdi",
                    EmergencyContactPhone = "+2348067890124",
                    EmergencyRelationship = "Mother",
                    GuarantorName = "Dr. Samuel Okonkwo",
                    GuarantorPhone = "+2348074446666",
                    GuarantorAddress = "33 Awolowo Way, Ikeja, Lagos",
                    GuarantorOccupation = "Medical Doctor",
                    EmploymentDate = DateTime.UtcNow.AddMonths(-8),
                    IsActive = true
                }
            });
        }

        public List<Employee> GetAll(bool includeInactive = true)
        {
            return includeInactive ? _employees.OrderBy(e => e.FullName).ToList() : _employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToList();
        }

        public Employee? GetById(int id) => _employees.FirstOrDefault(e => e.Id == id);

        public Employee Create(EmployeeDto dto)
        {
            var emp = new Employee
            {
                Id = _employees.Any() ? _employees.Max(e => e.Id) + 1 : 1,
                FullName = dto.FullName,
                StaffCode = string.IsNullOrWhiteSpace(dto.StaffCode) ? $"EMP-00{_employees.Count + 1}" : dto.StaffCode,
                Department = dto.Department,
                Designation = dto.Designation,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
                Address = dto.Address,
                BaseSalary = dto.BaseSalary,
                EmergencyContactName = dto.EmergencyContactName,
                EmergencyContactPhone = dto.EmergencyContactPhone,
                EmergencyRelationship = dto.EmergencyRelationship,
                GuarantorName = dto.GuarantorName,
                GuarantorPhone = dto.GuarantorPhone,
                GuarantorAddress = dto.GuarantorAddress,
                GuarantorOccupation = dto.GuarantorOccupation,
                EmploymentDate = dto.EmploymentDate,
                IsActive = true
            };
            _employees.Add(emp);
            return emp;
        }

        public bool Update(EmployeeDto dto)
        {
            var emp = GetById(dto.Id);
            if (emp == null) return false;

            emp.FullName = dto.FullName;
            emp.StaffCode = dto.StaffCode;
            emp.Department = dto.Department;
            emp.Designation = dto.Designation;
            emp.PhoneNumber = dto.PhoneNumber;
            emp.Email = dto.Email;
            emp.Address = dto.Address;
            emp.BaseSalary = dto.BaseSalary;
            emp.EmergencyContactName = dto.EmergencyContactName;
            emp.EmergencyContactPhone = dto.EmergencyContactPhone;
            emp.EmergencyRelationship = dto.EmergencyRelationship;
            emp.GuarantorName = dto.GuarantorName;
            emp.GuarantorPhone = dto.GuarantorPhone;
            emp.GuarantorAddress = dto.GuarantorAddress;
            emp.GuarantorOccupation = dto.GuarantorOccupation;
            emp.EmploymentDate = dto.EmploymentDate;
            emp.IsActive = dto.IsActive;
            return true;
        }

        public bool SoftDeleteToggle(int id)
        {
            var emp = GetById(id);
            if (emp == null) return false;
            emp.IsActive = !emp.IsActive;
            return true;
        }
    }
}
