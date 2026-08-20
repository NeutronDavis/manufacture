using Manufacture.Models.Entities;
using Manufacture.Models.DTOs;

namespace Manufacture.Services
{
    public class MockLogisticsService
    {
        private readonly List<Vehicle> _vehicles = new();
        private readonly List<FuelLog> _fuelLogs = new();

        public MockLogisticsService()
        {
            SeedInitialData();
        }

        private void SeedInitialData()
        {
            _vehicles.AddRange(new[]
            {
                new Vehicle
                {
                    Id = 1,
                    RegistrationNumber = "KJA-892-XA",
                    MakeAndModel = "Toyota HiAce Bakery Delivery Van 1",
                    Type = "Van",
                    AssignedDriverName = "Usman Danjuma",
                    DriverPhoneNumber = "+2348056789012",
                    FuelType = "PMS (Petrol)",
                    TankCapacityLitres = 70,
                    CurrentOdometerKm = 142500,
                    Status = "Active",
                    LastServiceDate = DateTime.UtcNow.AddDays(-20)
                },
                new Vehicle
                {
                    Id = 2,
                    RegistrationNumber = "LSR-441-YB",
                    MakeAndModel = "Ford Transit Delivery Van 2",
                    Type = "Van",
                    AssignedDriverName = "Ganiyu Muritala",
                    DriverPhoneNumber = "+2348031199221",
                    FuelType = "PMS (Petrol)",
                    TankCapacityLitres = 80,
                    CurrentOdometerKm = 98300,
                    Status = "Active",
                    LastServiceDate = DateTime.UtcNow.AddDays(-12)
                },
                new Vehicle
                {
                    Id = 3,
                    RegistrationNumber = "EKY-319-ZC",
                    MakeAndModel = "Mitsubishi Canter Flour Supply Truck",
                    Type = "Truck",
                    AssignedDriverName = "Sunday Peters",
                    DriverPhoneNumber = "+2348062288339",
                    FuelType = "AGO (Diesel)",
                    TankCapacityLitres = 120,
                    CurrentOdometerKm = 215400,
                    Status = "InMaintenance",
                    LastServiceDate = DateTime.UtcNow.AddDays(-3)
                }
            });

            _fuelLogs.AddRange(new[]
            {
                new FuelLog
                {
                    Id = 1,
                    VehicleId = 1,
                    VehicleRegNumber = "KJA-892-XA",
                    DriverName = "Usman Danjuma",
                    Date = DateTime.UtcNow.Date,
                    LitresDispensed = 45,
                    PricePerLitre = 980,
                    OdometerReadingKm = 142500,
                    PetrolStation = "NNPC Mega Station, Ikeja",
                    ReceiptNumber = "NNPC-90182",
                    RouteDescription = "Ikeja - Maryland - Victoria Island Supermarket Supply"
                },
                new FuelLog
                {
                    Id = 2,
                    VehicleId = 2,
                    VehicleRegNumber = "LSR-441-YB",
                    DriverName = "Ganiyu Muritala",
                    Date = DateTime.UtcNow.Date.AddDays(-1),
                    LitresDispensed = 50,
                    PricePerLitre = 980,
                    OdometerReadingKm = 98150,
                    PetrolStation = "TotalEnergies, Ikorodu Rd",
                    ReceiptNumber = "TOT-44019",
                    RouteDescription = "Mainland Vendor Stores & Walk-in Hub Delivery"
                },
                new FuelLog
                {
                    Id = 3,
                    VehicleId = 3,
                    VehicleRegNumber = "EKY-319-ZC",
                    DriverName = "Sunday Peters",
                    Date = DateTime.UtcNow.Date.AddDays(-3),
                    LitresDispensed = 90,
                    PricePerLitre = 1250,
                    OdometerReadingKm = 215400,
                    PetrolStation = "Mobil Station, Apapa",
                    ReceiptNumber = "MOB-81726",
                    RouteDescription = "Flour Mills Apapa Port to Factory Raw Material Haulage"
                }
            });
        }

        // Vehicles
        public List<Vehicle> GetAllVehicles() => _vehicles.ToList();
        public Vehicle? GetVehicleById(int id) => _vehicles.FirstOrDefault(v => v.Id == id);
        public Vehicle CreateVehicle(VehicleDto dto)
        {
            var vehicle = new Vehicle
            {
                Id = _vehicles.Any() ? _vehicles.Max(v => v.Id) + 1 : 1,
                RegistrationNumber = dto.RegistrationNumber,
                MakeAndModel = dto.MakeAndModel,
                Type = dto.Type,
                AssignedDriverName = dto.AssignedDriverName,
                DriverPhoneNumber = dto.DriverPhoneNumber,
                FuelType = dto.FuelType,
                TankCapacityLitres = dto.TankCapacityLitres,
                CurrentOdometerKm = dto.CurrentOdometerKm,
                Status = dto.Status,
                LastServiceDate = DateTime.UtcNow
            };
            _vehicles.Add(vehicle);
            return vehicle;
        }

        // Fuel Logs
        public List<FuelLog> GetAllFuelLogs() => _fuelLogs.OrderByDescending(f => f.Date).ThenByDescending(f => f.Id).ToList();
        public FuelLog CreateFuelLog(FuelLogDto dto)
        {
            var vehicle = GetVehicleById(dto.VehicleId);
            var log = new FuelLog
            {
                Id = _fuelLogs.Any() ? _fuelLogs.Max(f => f.Id) + 1 : 1,
                VehicleId = dto.VehicleId,
                VehicleRegNumber = vehicle?.RegistrationNumber ?? "Unknown",
                DriverName = vehicle?.AssignedDriverName ?? "Driver",
                Date = dto.Date,
                LitresDispensed = dto.LitresDispensed,
                PricePerLitre = dto.PricePerLitre,
                OdometerReadingKm = dto.OdometerReadingKm,
                PetrolStation = dto.PetrolStation,
                ReceiptNumber = dto.ReceiptNumber,
                RouteDescription = dto.RouteDescription
            };
            _fuelLogs.Add(log);

            if (vehicle != null && dto.OdometerReadingKm > vehicle.CurrentOdometerKm)
            {
                vehicle.CurrentOdometerKm = dto.OdometerReadingKm;
            }

            return log;
        }
    }
}
