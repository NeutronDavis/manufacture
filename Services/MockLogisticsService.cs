using Manufacture.Models.Entities;
using Manufacture.Models.DTOs;

namespace Manufacture.Services
{
    public class MockLogisticsService
    {
        private readonly List<Vehicle> _vehicles = new();
        private readonly List<FuelLog> _fuelLogs = new();
        private readonly List<RouteRate> _routeRates = new();
        private readonly List<CharterBooking> _charterBookings = new();
        private readonly List<MaintenanceLog> _maintenanceLogs = new();

        public MockLogisticsService()
        {
            SeedInitialData();
        }

        private void SeedInitialData()
        {
            // 1. Dual-Role Fleet: Internal Distribution + Commercial Haulage & VIP Logistics
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
                    Status = "Active",
                    LastServiceDate = DateTime.UtcNow.AddDays(-3)
                },
                new Vehicle
                {
                    Id = 4,
                    RegistrationNumber = "APP-714-DX",
                    MakeAndModel = "Mack Vision 30-Ton Commercial Haulage Truck",
                    Type = "Truck",
                    AssignedDriverName = "Ibrahim Yakubu",
                    DriverPhoneNumber = "+2348039871122",
                    FuelType = "AGO (Diesel)",
                    TankCapacityLitres = 450,
                    CurrentOdometerKm = 312000,
                    Status = "Active",
                    LastServiceDate = DateTime.UtcNow.AddDays(-15)
                },
                new Vehicle
                {
                    Id = 5,
                    RegistrationNumber = "IKJ-520-YY",
                    MakeAndModel = "Mercedes-Benz Actros 15-Ton Interstate Box Truck",
                    Type = "Truck",
                    AssignedDriverName = "Kingsley Obi",
                    DriverPhoneNumber = "+2348028765432",
                    FuelType = "AGO (Diesel)",
                    TankCapacityLitres = 300,
                    CurrentOdometerKm = 184500,
                    Status = "Active",
                    LastServiceDate = DateTime.UtcNow.AddDays(-8)
                },
                new Vehicle
                {
                    Id = 6,
                    RegistrationNumber = "BDG-108-LA",
                    MakeAndModel = "Toyota Hilux 4x4 Escort & Rapid Logistics Patrol",
                    Type = "Truck",
                    AssignedDriverName = "Sgt. Musa Garba (Rtd.)",
                    DriverPhoneNumber = "+2348076543210",
                    FuelType = "AGO (Diesel)",
                    TankCapacityLitres = 80,
                    CurrentOdometerKm = 89000,
                    Status = "Active",
                    LastServiceDate = DateTime.UtcNow.AddDays(-25)
                },
                new Vehicle
                {
                    Id = 7,
                    RegistrationNumber = "ABJ-903-VIP",
                    MakeAndModel = "Toyota Coaster Executive VIP Logistics Charter",
                    Type = "Van",
                    AssignedDriverName = "Emmanuel Nwankwo",
                    DriverPhoneNumber = "+2348098765432",
                    FuelType = "PMS (Petrol)",
                    TankCapacityLitres = 95,
                    CurrentOdometerKm = 64200,
                    Status = "Active",
                    LastServiceDate = DateTime.UtcNow.AddDays(-18)
                }
            });

            // 2. Statewide Route Rate Sheet (Nigeria Corridors)
            _routeRates.AddRange(new[]
            {
                new RouteRate { Id = 1, OriginState = "Lagos", DestinationState = "Rivers (Port Harcourt)", VehicleTypeRequired = "30-Ton Haulage Truck", RateWithFuel = 1850000, RateWithoutFuel = 1350000, EstimatedHours = 18 },
                new RouteRate { Id = 2, OriginState = "Lagos", DestinationState = "Kano (Dawanau Grain Corridor)", VehicleTypeRequired = "30-Ton Haulage Truck", RateWithFuel = 2200000, RateWithoutFuel = 1550000, EstimatedHours = 24 },
                new RouteRate { Id = 3, OriginState = "Lagos", DestinationState = "Abuja (FCT Commercial)", VehicleTypeRequired = "15-Ton Box Truck", RateWithFuel = 1400000, RateWithoutFuel = 1050000, EstimatedHours = 14 },
                new RouteRate { Id = 4, OriginState = "Lagos", DestinationState = "Edo (Benin City Hub)", VehicleTypeRequired = "15-Ton Box Truck", RateWithFuel = 750000, RateWithoutFuel = 550000, EstimatedHours = 7 },
                new RouteRate { Id = 5, OriginState = "Lagos", DestinationState = "Anambra (Onitsha Main Market)", VehicleTypeRequired = "30-Ton Haulage Truck", RateWithFuel = 1600000, RateWithoutFuel = 1200000, EstimatedHours = 12 },
                new RouteRate { Id = 6, OriginState = "Lagos", DestinationState = "Delta (Warri Energy Route)", VehicleTypeRequired = "30-Ton Haulage Truck", RateWithFuel = 1450000, RateWithoutFuel = 1100000, EstimatedHours = 11 },
                new RouteRate { Id = 7, OriginState = "Lagos", DestinationState = "Oyo (Ibadan Wholesale Hub)", VehicleTypeRequired = "Delivery Van", RateWithFuel = 280000, RateWithoutFuel = 210000, EstimatedHours = 3 },
                new RouteRate { Id = 8, OriginState = "Edo (Benin City Hub)", DestinationState = "Rivers (Port Harcourt)", VehicleTypeRequired = "30-Ton Haulage Truck", RateWithFuel = 950000, RateWithoutFuel = 700000, EstimatedHours = 7 },
                new RouteRate { Id = 9, OriginState = "Ogun (Agbara)", DestinationState = "Kaduna (Industrial Zone)", VehicleTypeRequired = "30-Ton Haulage Truck", RateWithFuel = 1950000, RateWithoutFuel = 1400000, EstimatedHours = 20 },
                new RouteRate { Id = 10, OriginState = "Lagos", DestinationState = "Abia (Aba Commercial Depot)", VehicleTypeRequired = "15-Ton Box Truck", RateWithFuel = 1500000, RateWithoutFuel = 1150000, EstimatedHours = 14 }
            });

            // 3. Commercial Haulage & Charter Bookings (Single & Multi-Leg Layovers)
            _charterBookings.AddRange(new[]
            {
                new CharterBooking
                {
                    Id = 1,
                    BookingRef = "LOG-20261001-01",
                    CustomerName = "Dangote Sugar Distribution Haulage",
                    CustomerPhone = "+2348028877662",
                    VehicleId = 4,
                    VehicleRegNumber = "APP-714-DX",
                    DriverId = 4,
                    DriverName = "Ibrahim Yakubu",
                    Legs = new List<TripLeg>
                    {
                        new() { Id = 1, CharterBookingId = 1, LegNumber = 1, OriginState = "Lagos", DestinationState = "Edo (Benin City Hub)", FuelOption = FuelOption.WithFuel, LegCost = 750000 },
                        new() { Id = 2, CharterBookingId = 1, LegNumber = 2, OriginState = "Edo (Benin City Hub)", DestinationState = "Rivers (Port Harcourt)", FuelOption = FuelOption.WithFuel, LegCost = 950000 }
                    },
                    DriverAllowance = 80000,
                    SecurityEscortFee = 120000,
                    TollsAndRoadLevies = 45000,
                    GrossCharterFee = 1700000,
                    TripStatus = CharterTripStatus.Completed,
                    DepartureDate = DateTime.UtcNow.AddDays(-4),
                    ReturnDate = DateTime.UtcNow.AddDays(-2),
                    Notes = "Multi-leg delivery of 600 bags refined sugar to Benin depot and Port Harcourt warehouse."
                },
                new CharterBooking
                {
                    Id = 2,
                    BookingRef = "LOG-20261003-02",
                    CustomerName = "PZ Cussons Consumer Goods Logistics",
                    CustomerPhone = "+2348035544332",
                    VehicleId = 5,
                    VehicleRegNumber = "IKJ-520-YY",
                    DriverId = 5,
                    DriverName = "Kingsley Obi",
                    Legs = new List<TripLeg>
                    {
                        new() { Id = 3, CharterBookingId = 2, LegNumber = 1, OriginState = "Lagos", DestinationState = "Abuja (FCT Commercial)", FuelOption = FuelOption.WithoutFuel, LegCost = 1050000 }
                    },
                    DriverAllowance = 50000,
                    SecurityEscortFee = 0,
                    TollsAndRoadLevies = 25000,
                    GrossCharterFee = 1050000,
                    TripStatus = CharterTripStatus.InTransit,
                    DepartureDate = DateTime.UtcNow.AddDays(-1),
                    Notes = "Client providing own diesel voucher via TotalEnergies corporate card."
                },
                new CharterBooking
                {
                    Id = 3,
                    BookingRef = "LOG-20261005-03",
                    CustomerName = "Flour Mills Wholesale Agro Transport",
                    CustomerPhone = "+2348039988771",
                    VehicleId = 4,
                    VehicleRegNumber = "APP-714-DX",
                    DriverId = 4,
                    DriverName = "Ibrahim Yakubu",
                    Legs = new List<TripLeg>
                    {
                        new() { Id = 4, CharterBookingId = 3, LegNumber = 1, OriginState = "Lagos", DestinationState = "Kano (Dawanau Grain Corridor)", FuelOption = FuelOption.WithFuel, LegCost = 2200000 }
                    },
                    DriverAllowance = 90000,
                    SecurityEscortFee = 150000,
                    TollsAndRoadLevies = 60000,
                    GrossCharterFee = 2200000,
                    TripStatus = CharterTripStatus.Booked,
                    DepartureDate = DateTime.UtcNow.AddDays(1),
                    Notes = "Scheduled grain haulage run with armed escort over Niger/Kaduna corridor."
                }
            });

            // 4. Maintenance / Servicing Logs
            _maintenanceLogs.AddRange(new[]
            {
                new MaintenanceLog
                {
                    Id = 1,
                    VehicleId = 3,
                    VehicleRegNumber = "EKY-319-ZC",
                    ServiceDate = DateTime.UtcNow.AddDays(-3),
                    Description = "Brake pad replacement, clutch bleed, and steering linkage grease",
                    Cost = 85000,
                    WorkshopOrVendor = "Automotive Care Hub, Ikeja",
                    InvoiceNumber = "ACH-2026-991"
                },
                new MaintenanceLog
                {
                    Id = 2,
                    VehicleId = 4,
                    VehicleRegNumber = "APP-714-DX",
                    ServiceDate = DateTime.UtcNow.AddDays(-15),
                    Description = "Heavy duty engine oil flush (30L 15W40), diesel filter and air filter",
                    Cost = 165000,
                    WorkshopOrVendor = "TotalEnergies Commercial Workshop, Apapa",
                    InvoiceNumber = "TOT-SRV-418"
                },
                new MaintenanceLog
                {
                    Id = 3,
                    VehicleId = 1,
                    VehicleRegNumber = "KJA-892-XA",
                    ServiceDate = DateTime.UtcNow.AddDays(-20),
                    Description = "Front shock absorber replacement and wheel alignment",
                    Cost = 62000,
                    WorkshopOrVendor = "Mainland Auto Works",
                    InvoiceNumber = "MAW-7721"
                }
            });

            // 5. Fuel Purchase Logs
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
                },
                new FuelLog
                {
                    Id = 4,
                    VehicleId = 4,
                    VehicleRegNumber = "APP-714-DX",
                    DriverName = "Ibrahim Yakubu",
                    Date = DateTime.UtcNow.Date.AddDays(-4),
                    LitresDispensed = 320,
                    PricePerLitre = 1250,
                    OdometerReadingKm = 311600,
                    PetrolStation = "TotalEnergies Expressway Terminal, Sagamu",
                    ReceiptNumber = "TOT-99214",
                    RouteDescription = "Commercial Haulage Trip: Lagos - Benin - Port Harcourt"
                }
            });
        }

        // =============================================================
        // Vehicles Directory
        // =============================================================

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

        // =============================================================
        // Route Rates Matrix (State-to-State Haulage Rates)
        // =============================================================

        public List<RouteRate> GetAllRouteRates() => _routeRates.OrderBy(r => r.OriginState).ThenBy(r => r.DestinationState).ToList();
        public RouteRate? GetRouteRateById(int id) => _routeRates.FirstOrDefault(r => r.Id == id);

        public RouteRate? FindRate(string origin, string destination)
        {
            if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination)) return null;

            // 1. Exact match
            var exact = _routeRates.FirstOrDefault(r => 
                r.OriginState.Equals(origin, StringComparison.OrdinalIgnoreCase) && 
                r.DestinationState.Equals(destination, StringComparison.OrdinalIgnoreCase) &&
                r.IsActive);
            if (exact != null) return exact;

            // 2. Flexible corridor match (e.g., "Kano" matches "Kano (Dawanau Grain Corridor)")
            return _routeRates.FirstOrDefault(r => 
                (r.OriginState.Equals(origin, StringComparison.OrdinalIgnoreCase) || 
                 origin.Contains(r.OriginState, StringComparison.OrdinalIgnoreCase) || 
                 r.OriginState.Contains(origin, StringComparison.OrdinalIgnoreCase)) && 
                (r.DestinationState.Equals(destination, StringComparison.OrdinalIgnoreCase) || 
                 destination.Contains(r.DestinationState, StringComparison.OrdinalIgnoreCase) || 
                 r.DestinationState.Contains(destination, StringComparison.OrdinalIgnoreCase)) &&
                r.IsActive);
        }

        public RouteRate CreateRouteRate(RouteRateDto dto)
        {
            var rate = new RouteRate
            {
                Id = _routeRates.Any() ? _routeRates.Max(r => r.Id) + 1 : 1,
                OriginState = dto.OriginState,
                DestinationState = dto.DestinationState,
                VehicleTypeRequired = dto.VehicleTypeRequired,
                RateWithFuel = dto.RateWithFuel,
                RateWithoutFuel = dto.RateWithoutFuel,
                EstimatedHours = dto.EstimatedHours,
                IsActive = dto.IsActive
            };
            _routeRates.Add(rate);
            return rate;
        }

        public RouteRate? UpdateRouteRate(int id, RouteRateDto dto)
        {
            var rate = GetRouteRateById(id);
            if (rate == null) return null;

            rate.OriginState = dto.OriginState;
            rate.DestinationState = dto.DestinationState;
            rate.VehicleTypeRequired = dto.VehicleTypeRequired;
            rate.RateWithFuel = dto.RateWithFuel;
            rate.RateWithoutFuel = dto.RateWithoutFuel;
            rate.EstimatedHours = dto.EstimatedHours;
            rate.IsActive = dto.IsActive;

            return rate;
        }

        public bool DeleteRouteRate(int id)
        {
            var rate = GetRouteRateById(id);
            if (rate == null) return false;
            _routeRates.Remove(rate);
            return true;
        }

        // =============================================================
        // Commercial Haulage & Charter Bookings
        // =============================================================

        public List<CharterBooking> GetAllCharterBookings() => _charterBookings.OrderByDescending(b => b.DepartureDate).ThenByDescending(b => b.Id).ToList();
        public CharterBooking? GetCharterBookingById(int id) => _charterBookings.FirstOrDefault(b => b.Id == id);

        public decimal CalculateTripRate(IEnumerable<TripLegDto>? legs)
        {
            if (legs == null) return 0;
            decimal total = 0;
            foreach (var leg in legs)
            {
                var match = FindRate(leg.OriginState, leg.DestinationState);
                if (match != null)
                {
                    leg.LegCost = leg.FuelOption == FuelOption.WithFuel ? match.RateWithFuel : match.RateWithoutFuel;
                }
                total += leg.LegCost;
            }
            return total;
        }

        public decimal CalculateTripRate(IEnumerable<TripLeg>? legs)
        {
            if (legs == null) return 0;
            decimal total = 0;
            foreach (var leg in legs)
            {
                var match = FindRate(leg.OriginState, leg.DestinationState);
                if (match != null)
                {
                    leg.LegCost = leg.FuelOption == FuelOption.WithFuel ? match.RateWithFuel : match.RateWithoutFuel;
                }
                total += leg.LegCost;
            }
            return total;
        }

        public (decimal totalIncidentals, decimal netRevenue) CalculateTripProfitability(CharterBookingDto? booking)
        {
            if (booking == null) return (0, 0);
            var totalIncidentals = booking.DriverAllowance + booking.SecurityEscortFee + booking.TollsAndRoadLevies;
            var netRevenue = booking.GrossCharterFee - totalIncidentals;
            booking.TotalTripIncidentals = totalIncidentals;
            booking.NetCharterRevenue = netRevenue;
            return (totalIncidentals, netRevenue);
        }

        public (decimal totalIncidentals, decimal netRevenue) CalculateTripProfitability(CharterBooking? booking)
        {
            if (booking == null) return (0, 0);
            var totalIncidentals = booking.DriverAllowance + booking.SecurityEscortFee + booking.TollsAndRoadLevies;
            var netRevenue = booking.GrossCharterFee - totalIncidentals;
            return (totalIncidentals, netRevenue);
        }

        public CharterBooking CreateCharterBooking(CharterBookingDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            dto.Legs ??= new List<TripLegDto>();

            var vehicle = GetVehicleById(dto.VehicleId);
            var bookingId = _charterBookings.Any() ? _charterBookings.Max(b => b.Id) + 1 : 1;

            // Recalculate leg costs and gross charter fee
            var grossFee = CalculateTripRate(dto.Legs);
            if (grossFee > 0)
            {
                dto.GrossCharterFee = grossFee;
            }

            var (incidentals, netRev) = CalculateTripProfitability(dto);

            var booking = new CharterBooking
            {
                Id = bookingId,
                BookingRef = string.IsNullOrWhiteSpace(dto.BookingRef) 
                    ? $"LOG-{DateTime.UtcNow:yyyyMMdd}-{bookingId:00}" 
                    : dto.BookingRef,
                CustomerName = dto.CustomerName,
                CustomerPhone = dto.CustomerPhone,
                VehicleId = dto.VehicleId,
                VehicleRegNumber = vehicle?.RegistrationNumber ?? dto.VehicleRegNumber,
                DriverId = dto.DriverId,
                DriverName = vehicle?.AssignedDriverName ?? dto.DriverName,
                DriverAllowance = dto.DriverAllowance,
                SecurityEscortFee = dto.SecurityEscortFee,
                TollsAndRoadLevies = dto.TollsAndRoadLevies,
                GrossCharterFee = dto.GrossCharterFee > 0 ? dto.GrossCharterFee : grossFee,
                TripStatus = dto.TripStatus,
                DepartureDate = dto.DepartureDate,
                ReturnDate = dto.ReturnDate,
                CreatedAt = DateTime.UtcNow,
                Notes = dto.Notes,
                Legs = dto.Legs.Select((l, i) => new TripLeg
                {
                    Id = i + 1,
                    CharterBookingId = bookingId,
                    LegNumber = l.LegNumber > 0 ? l.LegNumber : i + 1,
                    OriginState = l.OriginState,
                    DestinationState = l.DestinationState,
                    FuelOption = l.FuelOption,
                    LegCost = l.LegCost
                }).ToList()
            };

            _charterBookings.Add(booking);
            return booking;
        }

        public bool UpdateBookingStatus(int id, CharterTripStatus newStatus)
        {
            var booking = GetCharterBookingById(id);
            if (booking == null) return false;
            booking.TripStatus = newStatus;
            return true;
        }

        // =============================================================
        // Weekly Fleet Running Expenses (Sunday-to-Saturday Operating Week)
        // =============================================================

        public static DateTime GetOperatingWeekStart(DateTime date)
        {
            // Lock to Sunday
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Sunday)) % 7;
            return date.Date.AddDays(-1 * diff);
        }

        public List<WeeklyFleetExpense> GetWeeklyFleetExpenses() => GetWeeklyFleetExpenses(DateTime.UtcNow);

        public List<WeeklyFleetExpense> GetWeeklyFleetExpenses(string? weekDate)
        {
            if (!string.IsNullOrWhiteSpace(weekDate) && DateTime.TryParse(weekDate, out var parsed))
            {
                return GetWeeklyFleetExpenses(parsed);
            }
            return GetWeeklyFleetExpenses(DateTime.UtcNow);
        }

        public List<WeeklyFleetExpense> GetWeeklyFleetExpenses(DateTime referenceDate)
        {
            var weekStart = GetOperatingWeekStart(referenceDate);
            var weekEnd = weekStart.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);

            var result = new List<WeeklyFleetExpense>();
            int expenseId = 1;

            foreach (var vehicle in _vehicles)
            {
                // Fuel logged for this vehicle in this operating week
                var fuelTotal = _fuelLogs
                    .Where(f => f.VehicleId == vehicle.Id && f.Date >= weekStart && f.Date <= weekEnd)
                    .Sum(f => f.TotalCost);

                // Servicing and maintenance logged
                var maintTotal = _maintenanceLogs
                    .Where(m => m.VehicleId == vehicle.Id && m.ServiceDate >= weekStart && m.ServiceDate <= weekEnd)
                    .Sum(m => m.Cost);

                // Incidentals for commercial trips operating in this week
                var incidentalsTotal = _charterBookings
                    .Where(b => b.VehicleId == vehicle.Id && b.DepartureDate >= weekStart && b.DepartureDate <= weekEnd)
                    .Sum(b => b.TotalTripIncidentals);

                result.Add(new WeeklyFleetExpense
                {
                    Id = expenseId++,
                    OperatingWeekStart = weekStart,
                    OperatingWeekEnd = weekEnd.Date,
                    VehicleId = vehicle.Id,
                    VehicleRegNumber = vehicle.RegistrationNumber,
                    DriverName = vehicle.AssignedDriverName,
                    FuelExpenseTotal = fuelTotal,
                    MaintenanceExpenseTotal = maintTotal,
                    IncidentalsTotal = incidentalsTotal
                });
            }

            return result;
        }

        // =============================================================
        // Maintenance & Fuel Logs
        // =============================================================

        public List<MaintenanceLog> GetAllMaintenanceLogs() => _maintenanceLogs.OrderByDescending(m => m.ServiceDate).ToList();
        
        public MaintenanceLog CreateMaintenanceLog(MaintenanceLogDto dto)
        {
            var vehicle = GetVehicleById(dto.VehicleId);
            var log = new MaintenanceLog
            {
                Id = _maintenanceLogs.Any() ? _maintenanceLogs.Max(m => m.Id) + 1 : 1,
                VehicleId = dto.VehicleId,
                VehicleRegNumber = vehicle?.RegistrationNumber ?? "Unknown",
                ServiceDate = dto.ServiceDate,
                Description = dto.Description,
                Cost = dto.Cost,
                WorkshopOrVendor = dto.WorkshopOrVendor,
                InvoiceNumber = dto.InvoiceNumber
            };
            _maintenanceLogs.Add(log);

            if (vehicle != null)
            {
                vehicle.LastServiceDate = dto.ServiceDate;
            }

            return log;
        }

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
