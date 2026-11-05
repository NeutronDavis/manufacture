using System;
using System.Linq;
using Manufacture.Models.DTOs;
using Manufacture.Services;
using Xunit;

namespace Manufacture.Tests
{
    public class MockEmployeeServiceTests
    {
        private readonly MockEmployeeService _service;

        public MockEmployeeServiceTests()
        {
            _service = new MockEmployeeService();
        }

        [Fact]
        public void GetFleetDrivers_ShouldOnlyReturnActiveLogisticsAndFleetStaff()
        {
            var drivers = _service.GetFleetDrivers();

            Assert.NotEmpty(drivers);
            Assert.All(drivers, d => Assert.True(d.IsActive));
            Assert.All(drivers, d =>
                Assert.True(
                    d.Department.Equals("Logistics", StringComparison.OrdinalIgnoreCase) ||
                    d.Department.Equals("Logistic & Fleet", StringComparison.OrdinalIgnoreCase) ||
                    d.Department.Equals("Logistics & Fleet", StringComparison.OrdinalIgnoreCase)));

            Assert.Contains(drivers, d => d.FullName == "Usman Danjuma" && d.Designation == "Lead Delivery Driver");
            Assert.DoesNotContain(drivers, d => d.FullName == "Emeka Obi");
        }

        [Fact]
        public void Create_LogisticsAndFleetEmployee_ShouldAppearInFleetDrivers()
        {
            var created = _service.Create(new EmployeeDto
            {
                FullName = "Charter Driver",
                StaffCode = "EMP-TEST",
                Department = "Logistic & Fleet",
                Designation = "VIP Charter Driver",
                PhoneNumber = "+2348000000000"
            });

            var drivers = _service.GetFleetDrivers();

            Assert.Contains(drivers, d => d.Id == created.Id && d.FullName == "Charter Driver");
        }
    }
}
