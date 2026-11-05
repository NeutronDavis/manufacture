using Manufacture.Models.Entities;
using Manufacture.Models.DTOs;

namespace Manufacture.Services
{
    public class MockUserService
    {
        private readonly List<User> _users = new();

        public MockUserService()
        {
            SeedInitialUsers();
        }

        private void SeedInitialUsers()
        {
            _users.AddRange(new[]
            {
                new User { Id = 1, Name = "Adekunle Johnson", Email = "admin@bakery.com", Role = "SuperAdmin", PinCode = "111111", Password = "Password123!", PhoneNumber = "+2348011112222", IsActive = true },
                new User { Id = 2, Name = "Chidinma Okoro", Email = "production@bakery.com", Role = "ProductionManager", PinCode = "222222", Password = "Password123!", PhoneNumber = "+2348022223333", IsActive = true },
                new User { Id = 3, Name = "Musa Ibrahim", Email = "store@bakery.com", Role = "StoreManager", PinCode = "333333", Password = "Password123!", PhoneNumber = "+2348033334444", IsActive = true },
                new User { Id = 4, Name = "Babatunde Alabi", Email = "sales@bakery.com", Role = "SalesRep", PinCode = "444444", Password = "Password123!", PosTerminalId = "POS-01", PhoneNumber = "+2348044445555", IsActive = true },
                new User { Id = 5, Name = "Goodness Supermarkets", Email = "vendor@bakery.com", Role = "Vendor", PinCode = "555555", Password = "Password123!", PhoneNumber = "+2348055556666", IsActive = true },
                new User { Id = 6, Name = "Ngozi Eze", Email = "hr@bakery.com", Role = "HrPayrollManager", PinCode = "666666", Password = "Password123!", PhoneNumber = "+2348066667777", IsActive = true },

                // The rest of the field roster. MockSalesService binds each of these to a
                // SalesRepProfile by name, so a rep shown on the dashboard is a person who
                // can actually sign in, rather than a name invented to fill a chart.
                new User { Id = 7, Name = "Chiamaka Nwosu", Email = "chiamaka@bakery.com", Role = "SalesRep", PinCode = "444444", Password = "Password123!", PosTerminalId = "POS-03", PhoneNumber = "+2348055556677", IsActive = true },
                new User { Id = 8, Name = "Ibrahim Sanni", Email = "ibrahim@bakery.com", Role = "SalesRep", PinCode = "444444", Password = "Password123!", PosTerminalId = "POS-02", PhoneNumber = "+2348066667788", IsActive = true },
                new User { Id = 9, Name = "Blessing Etim", Email = "blessing@bakery.com", Role = "SalesRep", PinCode = "444444", Password = "Password123!", PosTerminalId = "POS-02", PhoneNumber = "+2348077778899", IsActive = true },
                new User { Id = 10, Name = "Segun Adewale", Email = "segun@bakery.com", Role = "SalesRep", PinCode = "444444", Password = "Password123!", PosTerminalId = "POS-02", PhoneNumber = "+2348088889900", IsActive = true },
                new User { Id = 11, Name = "Halima Yusuf", Email = "halima@bakery.com", Role = "SalesRep", PinCode = "444444", Password = "Password123!", PosTerminalId = "POS-03", PhoneNumber = "+2348099990011", IsActive = true }
            });
        }

        public List<User> GetAll(bool includeInactive = true)
        {
            return includeInactive ? _users.ToList() : _users.Where(u => u.IsActive).ToList();
        }

        public User? GetById(int id) => _users.FirstOrDefault(u => u.Id == id);

        public User? AuthenticatePassword(string email, string password)
        {
            return _users.FirstOrDefault(u => u.IsActive &&
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
                u.Password == password);
        }

        public User? AuthenticatePin(string pinCode)
        {
            return _users.FirstOrDefault(u => u.IsActive && u.PinCode == pinCode);
        }

        public User Create(UserDto dto)
        {
            var user = new User
            {
                Id = _users.Any() ? _users.Max(u => u.Id) + 1 : 1,
                Name = dto.Name,
                Email = dto.Email,
                Role = dto.Role,
                PinCode = string.IsNullOrWhiteSpace(dto.PinCode) ? "123456" : dto.PinCode,
                Password = string.IsNullOrWhiteSpace(dto.Password) ? "Password123!" : dto.Password,
                PosTerminalId = dto.PosTerminalId,
                PhoneNumber = dto.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _users.Add(user);
            return user;
        }

        public bool Update(UserDto dto)
        {
            var user = GetById(dto.Id);
            if (user == null) return false;

            user.Name = dto.Name;
            user.Email = dto.Email;
            user.Role = dto.Role;
            user.PinCode = dto.PinCode;
            if (!string.IsNullOrWhiteSpace(dto.Password)) user.Password = dto.Password;
            user.PosTerminalId = dto.PosTerminalId;
            user.PhoneNumber = dto.PhoneNumber;
            user.IsActive = dto.IsActive;
            return true;
        }

        public bool SoftDeleteToggle(int id)
        {
            var user = GetById(id);
            if (user == null) return false;
            user.IsActive = !user.IsActive;
            return true;
        }
    }
}
