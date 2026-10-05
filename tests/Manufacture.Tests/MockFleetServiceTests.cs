using System;
using System.Collections.Generic;
using System.Linq;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
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
        public void RouteRates_ShouldSupportNationwideStatewideCorridors()
        {
            var rates = _service.GetAllRouteRates();
            
            Assert.NotEmpty(rates);
            Assert.Contains(rates, r => r.OriginState == "Lagos" && r.DestinationState.Contains("Port Harcourt"));
            Assert.Contains(rates, r => r.OriginState == "Lagos" && r.DestinationState.Contains("Kano"));
            Assert.Contains(rates, r => r.OriginState == "Lagos" && r.DestinationState.Contains("Abuja"));
        }

        [Fact]
        public void CalculateTripRate_SingleLegWithFuel_ShouldApplyWithFuelRate()
        {
            var legs = new List<TripLegDto>
            {
                new TripLegDto
                {
                    LegNumber = 1,
                    OriginState = "Lagos",
                    DestinationState = "Rivers (Port Harcourt)",
                    FuelOption = FuelOption.WithFuel
                }
            };

            var total = _service.CalculateTripRate(legs);

            Assert.Equal(1_850_000m, total);
            Assert.Equal(1_850_000m, legs[0].LegCost);
        }

        [Fact]
        public void CalculateTripRate_SingleLegWithoutFuel_ShouldApplyWithoutFuelRate()
        {
            var legs = new List<TripLegDto>
            {
                new TripLegDto
                {
                    LegNumber = 1,
                    OriginState = "Lagos",
                    DestinationState = "Rivers (Port Harcourt)",
                    FuelOption = FuelOption.WithoutFuel
                }
            };

            var total = _service.CalculateTripRate(legs);

            Assert.Equal(1_350_000m, total);
            Assert.Equal(1_350_000m, legs[0].LegCost);
        }

        [Fact]
        public void CalculateTripRate_MultiLegLayovers_ShouldAggregateAccurately()
        {
            // Leg 1: Lagos -> Edo (Benin City Hub) With Fuel (750,000)
            // Leg 2: Edo (Benin City Hub) -> Rivers (Port Harcourt) Freight Only (700,000)
            var legs = new List<TripLegDto>
            {
                new TripLegDto
                {
                    LegNumber = 1,
                    OriginState = "Lagos",
                    DestinationState = "Edo (Benin City Hub)",
                    FuelOption = FuelOption.WithFuel
                },
                new TripLegDto
                {
                    LegNumber = 2,
                    OriginState = "Edo (Benin City Hub)",
                    DestinationState = "Rivers (Port Harcourt)",
                    FuelOption = FuelOption.WithoutFuel
                }
            };

            var total = _service.CalculateTripRate(legs);

            Assert.Equal(750_000m + 700_000m, total);
            Assert.Equal(1_450_000m, total);
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
        public void CreateCharterBooking_ShouldPersistAndAssignTripLegs()
        {
            var vehicle = _service.GetAllVehicles().First(v => v.Type == "Truck");

            var dto = new CharterBookingDto
            {
                CustomerName = "Nigerian Breweries PLC",
                CustomerPhone = "+2348023456789",
                VehicleId = vehicle.Id,
                DriverId = vehicle.Id,
                DriverAllowance = 80_000m,
                SecurityEscortFee = 200_000m,
                TollsAndRoadLevies = 50_000m,
                DepartureDate = DateTime.UtcNow.Date.AddDays(2),
                Legs = new List<TripLegDto>
                {
                    new TripLegDto
                    {
                        LegNumber = 1,
                        OriginState = "Lagos",
                        DestinationState = "Kano (Dawanau Grain Corridor)",
                        FuelOption = FuelOption.WithFuel
                    }
                }
            };

            var created = _service.CreateCharterBooking(dto);

            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.StartsWith("LOG-", created.BookingRef);
            Assert.Equal(2_200_000m, created.GrossCharterFee);
            Assert.Equal(330_000m, created.TotalTripIncidentals);
            Assert.Equal(1_870_000m, created.NetCharterRevenue);
            Assert.Single(created.Legs);
            Assert.Equal(CharterTripStatus.Booked, created.TripStatus);
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

        [Fact]
        public void RouteRate_CreateAndUpdate_ShouldWorkCorrectly()
        {
            var newRateDto = new RouteRateDto
            {
                OriginState = "Lagos",
                DestinationState = "Calabar",
                VehicleTypeRequired = "30-Ton Haulage Truck",
                RateWithFuel = 2_400_000m,
                RateWithoutFuel = 1_600_000m,
                EstimatedHours = 18,
                IsActive = true
            };

            var created = _service.CreateRouteRate(newRateDto);
            Assert.NotNull(created);
            Assert.True(created.Id > 0);

            // Update
            newRateDto.RateWithFuel = 2_500_000m;
            var updated = _service.UpdateRouteRate(created.Id, newRateDto);
            Assert.NotNull(updated);
            Assert.Equal(2_500_000m, updated.RateWithFuel);

            // Delete
            var deleted = _service.DeleteRouteRate(created.Id);
            Assert.True(deleted);
            Assert.Null(_service.GetRouteRateById(created.Id));
        }
    }
}
