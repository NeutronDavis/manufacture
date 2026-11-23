using System;
using System.Collections.Generic;
using System.Linq;
using Manufacture.Models.DTOs;
using Manufacture.Models.Domain;
using Manufacture.Services;
using Xunit;

namespace Manufacture.Tests
{
    public class MockFleetServiceTests
    {
        private readonly MockLogisticsService _service;

        public MockFleetServiceTests()
        {
            _service = new MockLogisticsService();
        }

        [Fact]
        public void InitialFleet_ShouldIncludeDualRoleCommercialAndVIPVehicles()
        {
            var vehicles = _service.GetAllVehicles();
            
            Assert.NotNull(vehicles);
            Assert.True(vehicles.Count >= 7);
            
            // Should contain commercial haulage truck (e.g. Mack Vision 30-Ton)
            Assert.Contains(vehicles, v => v.MakeAndModel.Contains("30-Ton Commercial Haulage Truck"));
            
            // Should contain VIP Escort Patrol (Toyota Hilux)
            Assert.Contains(vehicles, v => v.MakeAndModel.Contains("Toyota Hilux 4x4 Escort"));
        }

        [Fact]
        public void InitialFleet_ShouldDescribeEachVehicleFunction()
        {
            var vehicles = _service.GetAllVehicles();

            Assert.All(vehicles, v => Assert.False(string.IsNullOrWhiteSpace(v.Function)));
            Assert.Contains(vehicles, v => v.Function == "Deliveries");
            Assert.Contains(vehicles, v => v.Function == "VIP Logistics");
            Assert.Contains(vehicles, v => v.Function == "Commercial Haulage");
        }

        [Fact]
        public void CreateVehicle_ShouldPersistVehicleFunction()
        {
            var dto = new VehicleDto
            {
                RegistrationNumber = "TEST-101-FN",
                MakeAndModel = "Toyota Coaster VIP Shuttle",
                Type = "Van",
                Function = "VIP Logistics",
                AssignedDriverName = "Test Driver",
                DriverPhoneNumber = "+2348000000000",
                FuelType = "PMS (Petrol)",
                TankCapacityLitres = 95,
                CurrentOdometerKm = 1200,
                Status = "Active"
            };

            var created = _service.CreateVehicle(dto);

            Assert.Equal("VIP Logistics", created.Function);
            Assert.Equal("VIP Logistics", _service.GetVehicleById(created.Id)?.Function);
        }

        [Fact]
        public void CalculateTripProfitability_ShouldDeductIncidentalsAccurately()
        {
            var booking = new CharterBookingDto
            {
                GrossCharterFee = 2_100_000m,
                DriverAllowance = 75_000m,
                SecurityEscortFee = 150_000m,
                TollsAndRoadLevies = 45_000m
            };

            var (incidentals, netRevenue) = _service.CalculateTripProfitability(booking);

            Assert.Equal(270_000m, incidentals);
            Assert.Equal(1_830_000m, netRevenue);
        }

        [Fact]
        public void OperatingWeekStart_ShouldAlwaysLockToSunday()
        {
            // Test multiple days of the week
            var wednesday = new DateTime(2026, 10, 7); // Wednesday
            var sundayExpected = new DateTime(2026, 10, 4); // Sunday

            var calculatedSunday = MockLogisticsService.GetOperatingWeekStart(wednesday);
            Assert.Equal(sundayExpected, calculatedSunday);
            Assert.Equal(DayOfWeek.Sunday, calculatedSunday.DayOfWeek);

            // Sunday itself should return itself
            Assert.Equal(sundayExpected, MockLogisticsService.GetOperatingWeekStart(sundayExpected));

            // Saturday should return the preceding Sunday
            var saturday = new DateTime(2026, 10, 10);
            Assert.Equal(sundayExpected, MockLogisticsService.GetOperatingWeekStart(saturday));
        }

        [Fact]
        public void GetWeeklyFleetExpenses_ShouldAggregateAllFleetVehiclesForOperatingWeek()
        {
            var expenses = _service.GetWeeklyFleetExpenses(DateTime.UtcNow);

            Assert.NotEmpty(expenses);
            var vehicles = _service.GetAllVehicles();
            Assert.Equal(vehicles.Count, expenses.Count);

            foreach (var exp in expenses)
            {
                Assert.Equal(DayOfWeek.Sunday, exp.OperatingWeekStart.DayOfWeek);
                Assert.Equal(DayOfWeek.Saturday, exp.OperatingWeekEnd.DayOfWeek);
                Assert.Equal(exp.FuelExpenseTotal + exp.MaintenanceExpenseTotal + exp.IncidentalsTotal, exp.TotalWeeklyRunningCost);
            }
        }

        [Fact]
        public void CreateMaintenanceLog_ShouldRecordCostAndAdvanceVehicleLastServiceDate()
        {
            var vehicle = _service.GetAllVehicles().First();
            var serviceDate = DateTime.UtcNow.Date;

            var dto = new MaintenanceLogDto
            {
                VehicleId = vehicle.Id,
                ServiceDate = serviceDate,
                Description = "Complete brake pad overhaul and engine synthetic oil change",
                Cost = 145_000m,
                WorkshopOrVendor = "Innoson Motors Service Center, Ikeja",
                InvoiceNumber = "INNO-2026-441"
            };

            var log = _service.CreateMaintenanceLog(dto);

            Assert.NotNull(log);
            Assert.Equal(145_000m, log.Cost);
            Assert.Equal(serviceDate, vehicle.LastServiceDate);
            Assert.Contains(log, _service.GetAllMaintenanceLogs());
        }
    }
}
