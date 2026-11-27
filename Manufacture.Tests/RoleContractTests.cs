using Manufacture.Models.Domain;
using Xunit;

namespace Manufacture.Tests
{
    public class RoleContractTests
    {
        [Fact]
        public void RoleIdsMatchTheDatabaseContract()
        {
            Assert.Equal(1, (int)UserRole.SuperAdmin);
            Assert.Equal(2, (int)UserRole.ProductionManager);
            Assert.Equal(3, (int)UserRole.StoreManager);
            Assert.Equal(4, (int)UserRole.SalesRep);
            Assert.Equal(5, (int)UserRole.Customer);
            Assert.Equal(6, (int)UserRole.HrPayrollManager);

            Assert.Equal(
                new[]
                {
                    "SuperAdmin",
                    "ProductionManager",
                    "StoreManager",
                    "SalesRep",
                    "Customer",
                    "HrPayrollManager"
                },
                Enum.GetNames<UserRole>());
        }
    }
}
