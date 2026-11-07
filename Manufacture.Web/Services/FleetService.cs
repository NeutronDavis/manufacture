using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Microsoft.EntityFrameworkCore;
using CharterBooking = Manufacture.Models.Entities.CharterBooking;
using Driver = Manufacture.Models.Entities.Driver;
using Employee = Manufacture.Models.Entities.Employee;
using FuelLog = Manufacture.Models.Entities.FuelLog;
using MaintenanceLog = Manufacture.Models.Entities.MaintenanceLog;
using RouteRate = Manufacture.Models.Entities.RouteRate;
using TripLeg = Manufacture.Models.Entities.TripLeg;
using Vehicle = Manufacture.Models.Entities.Vehicle;

namespace Manufacture.Services
{
    public class FleetService : IFleetService
    {
        private static readonly string[] AllowedFuelTypes = { "Petrol", "Diesel", "EV Charging" };

        private readonly ManufactureDbContext _db;

        public FleetService(ManufactureDbContext db)
        {
            _db = db;
        }

        public async Task<List<VehicleDto>> GetVehiclesAsync(CancellationToken cancellationToken = default)
        {
            var vehicles = await _db.Vehicles
                .AsNoTracking()
                .Include(v => v.AssignedEmployee)
                .OrderBy(v => v.RegistrationNumber)
                .ToListAsync(cancellationToken);

            return vehicles.Select(MapVehicle).ToList();
        }

        public async Task<VehicleDto?> GetVehicleAsync(int id, CancellationToken cancellationToken = default)
        {
            var vehicle = await _db.Vehicles
                .AsNoTracking()
                .Include(v => v.AssignedEmployee)
                .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

            return vehicle == null ? null : MapVehicle(vehicle);
        }

        public async Task<VehicleDto> CreateVehicleAsync(VehicleDto dto, CancellationToken cancellationToken = default)
        {
            var registration = dto.RegistrationNumber.Trim().ToUpperInvariant();
            if (await _db.Vehicles.AnyAsync(v => v.RegistrationNumber == registration, cancellationToken))
            {
                throw new InvalidOperationException("A vehicle with that registration number already exists.");
            }

            Employee? assignedEmployee = null;
            if (dto.AssignedEmployeeId.HasValue)
            {
                assignedEmployee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.Id == dto.AssignedEmployeeId.Value, cancellationToken);

                if (assignedEmployee == null || !IsLogisticsDepartment(assignedEmployee.Department))
                {
                    throw new InvalidOperationException("The assigned driver must be an employee in the Logistics department.");
                }
            }

            var fuelType = NormalizeFuelType(dto.FuelType);
            var vehicle = new Vehicle
            {
                RegistrationNumber = registration,
                MakeAndModel = dto.MakeAndModel.Trim(),
                VehicleType = string.IsNullOrWhiteSpace(dto.Type) ? "Van" : dto.Type.Trim(),
                VehicleFunction = string.IsNullOrWhiteSpace(dto.Function) ? "Deliveries" : dto.Function.Trim(),
                AssignedEmployeeId = assignedEmployee?.Id,
                FuelType = fuelType,
                TankCapacityLitres = dto.TankCapacityLitres <= 0 ? 60m : dto.TankCapacityLitres,
                CurrentOdometerKm = Math.Max(0m, dto.CurrentOdometerKm),
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status.Trim(),
                LastServiceDate = DateOnly.FromDateTime(DateTime.Today),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Vehicles.Add(vehicle);
            await _db.SaveChangesAsync(cancellationToken);

            vehicle.AssignedEmployee = assignedEmployee;
            return MapVehicle(vehicle);
        }

        public async Task<List<EmployeeDto>> GetLogisticsDriversAsync(CancellationToken cancellationToken = default)
        {
            var employees = await _db.Employees
                .AsNoTracking()
                .Where(e => e.IsActive && e.Department.Contains("Logistic"))
                .OrderBy(e => e.FullName)
                .ToListAsync(cancellationToken);

            return employees.Select(MapEmployee).ToList();
        }

        public async Task<List<FleetDriverDto>> GetDriversAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Drivers
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.FullName)
                .Select(d => new FleetDriverDto
                {
                    Id = d.Id,
                    FullName = d.FullName,
                    PhoneNumber = d.PhoneNumber,
                    LicenseNumber = d.LicenseNumber ?? string.Empty
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<FuelLogDto>> GetFuelLogsAsync(CancellationToken cancellationToken = default)
        {
            var logs = await _db.FuelLogs
                .AsNoTracking()
                .Include(l => l.Vehicle)
                .Include(l => l.Driver)
                .OrderByDescending(l => l.FuelDate)
                .ThenByDescending(l => l.Id)
                .ToListAsync(cancellationToken);

            return logs.Select(MapFuelLog).ToList();
        }

        public async Task<FuelLogDto> CreateFuelLogAsync(FuelLogDto dto, CancellationToken cancellationToken = default)
        {
            var vehicle = await _db.Vehicles
                .FirstOrDefaultAsync(v => v.Id == dto.VehicleId, cancellationToken)
                ?? throw new InvalidOperationException("Select a valid vehicle.");

            var fuelType = NormalizeFuelType(string.IsNullOrWhiteSpace(dto.FuelType) ? vehicle.FuelType : dto.FuelType);

            var log = new FuelLog
            {
                VehicleId = vehicle.Id,
                DriverId = vehicle.AssignedDriverId,
                FuelDate = dto.Date == default ? DateTime.UtcNow : dto.Date,
                FuelType = fuelType,
                LitresDispensed = dto.LitresDispensed,
                PricePerLitre = dto.PricePerLitre,
                PetrolStation = dto.PetrolStation.Trim(),
                RouteDescription = dto.RouteDescription.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.FuelLogs.Add(log);
            await _db.SaveChangesAsync(cancellationToken);

            log.Vehicle = vehicle;
            return MapFuelLog(log);
        }

        public async Task<MaintenanceLogDto> CreateMaintenanceLogAsync(
            MaintenanceLogDto dto,
            CancellationToken cancellationToken = default)
        {
            var vehicle = await _db.Vehicles
                .FirstOrDefaultAsync(v => v.Id == dto.VehicleId, cancellationToken)
                ?? throw new InvalidOperationException("Select a valid vehicle.");

            var log = new MaintenanceLog
            {
                VehicleId = vehicle.Id,
                ServiceDate = DateOnly.FromDateTime(dto.ServiceDate),
                Description = dto.Description.Trim(),
                Cost = dto.Cost,
                WorkshopOrVendor = dto.WorkshopOrVendor.Trim(),
                InvoiceNumber = dto.InvoiceNumber.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.MaintenanceLogs.Add(log);
            await _db.SaveChangesAsync(cancellationToken);

            return new MaintenanceLogDto
            {
                Id = log.Id,
                VehicleId = vehicle.Id,
                VehicleRegNumber = vehicle.RegistrationNumber,
                ServiceDate = log.ServiceDate.ToDateTime(TimeOnly.MinValue),
                Description = log.Description,
                Cost = log.Cost,
                WorkshopOrVendor = log.WorkshopOrVendor,
                InvoiceNumber = log.InvoiceNumber
            };
        }

        public async Task<List<RouteRateDto>> GetRouteRatesAsync(
            string? search = null,
            string? vehicleType = null,
            CancellationToken cancellationToken = default)
        {
            var query = _db.RouteRates.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(r =>
                    r.OriginState.ToLower().Contains(term) ||
                    r.DestinationState.ToLower().Contains(term) ||
                    r.VehicleTypeRequired.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(vehicleType))
            {
                query = query.Where(r => r.VehicleTypeRequired == vehicleType);
            }

            var rates = await query
                .OrderBy(r => r.OriginState)
                .ThenBy(r => r.DestinationState)
                .ToListAsync(cancellationToken);

            return rates.Select(MapRouteRate).ToList();
        }

        public async Task<RouteRateDto?> GetRouteRateAsync(int id, CancellationToken cancellationToken = default)
        {
            var rate = await _db.RouteRates.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            return rate == null ? null : MapRouteRate(rate);
        }

        public async Task<RouteRateDto> CreateRouteRateAsync(RouteRateDto dto, CancellationToken cancellationToken = default)
        {
            var rate = new RouteRate
            {
                OriginState = dto.OriginState.Trim(),
                DestinationState = dto.DestinationState.Trim(),
                VehicleTypeRequired = dto.VehicleTypeRequired.Trim(),
                RateWithFuel = dto.RateWithFuel,
                RateWithoutFuel = dto.RateWithoutFuel,
                EstimatedHours = dto.EstimatedHours,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.RouteRates.Add(rate);
            await _db.SaveChangesAsync(cancellationToken);
            return MapRouteRate(rate);
        }

        public async Task<bool> UpdateRouteRateAsync(RouteRateDto dto, CancellationToken cancellationToken = default)
        {
            var rate = await _db.RouteRates.FirstOrDefaultAsync(r => r.Id == dto.Id, cancellationToken);
            if (rate == null)
            {
                return false;
            }

            rate.OriginState = dto.OriginState.Trim();
            rate.DestinationState = dto.DestinationState.Trim();
            rate.VehicleTypeRequired = dto.VehicleTypeRequired.Trim();
            rate.RateWithFuel = dto.RateWithFuel;
            rate.RateWithoutFuel = dto.RateWithoutFuel;
            rate.EstimatedHours = dto.EstimatedHours;
            rate.IsActive = dto.IsActive;
            rate.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeleteRouteRateAsync(int id, CancellationToken cancellationToken = default)
        {
            var rate = await _db.RouteRates.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (rate == null)
            {
                return false;
            }

            _db.RouteRates.Remove(rate);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> SetRouteRateActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
        {
            var rate = await _db.RouteRates.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (rate == null)
            {
                return false;
            }

            rate.IsActive = isActive;
            rate.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<List<RouteRateDto>> GetActiveRouteRatesAsync(CancellationToken cancellationToken = default)
        {
            var rates = await _db.RouteRates
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.OriginState)
                .ThenBy(r => r.DestinationState)
                .ToListAsync(cancellationToken);

            return rates.Select(MapRouteRate).ToList();
        }

        public async Task<List<CharterBookingDto>> GetCharterBookingsAsync(
            string? status = null,
            string? search = null,
            CancellationToken cancellationToken = default)
        {
            var query = _db.CharterBookings
                .AsNoTracking()
                .Include(b => b.Vehicle)
                .Include(b => b.Driver)
                .Include(b => b.TripLegs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase)
                && Enum.TryParse<CharterTripStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(b => b.TripStatus == (int)parsedStatus);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(b =>
                    b.BookingRef.ToLower().Contains(term) ||
                    b.CustomerName.ToLower().Contains(term) ||
                    b.Vehicle.RegistrationNumber.ToLower().Contains(term) ||
                    b.DriverName.ToLower().Contains(term) ||
                    b.TripLegs.Any(l => l.OriginState.ToLower().Contains(term) || l.DestinationState.ToLower().Contains(term)));
            }

            var bookings = await query
                .OrderByDescending(b => b.DepartureDate)
                .ThenByDescending(b => b.Id)
                .ToListAsync(cancellationToken);

            return bookings.Select(MapCharterBooking).ToList();
        }

        public async Task<CharterBookingDto?> GetCharterBookingAsync(int id, CancellationToken cancellationToken = default)
        {
            var booking = await _db.CharterBookings
                .AsNoTracking()
                .Include(b => b.Vehicle)
                .Include(b => b.Driver)
                .Include(b => b.TripLegs)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

            return booking == null ? null : MapCharterBooking(booking);
        }

        public async Task<CharterBookingDto> CreateCharterBookingAsync(
            CharterBookingDto dto,
            CancellationToken cancellationToken = default)
        {
            var vehicle = await _db.Vehicles
                .Include(v => v.AssignedEmployee)
                .FirstOrDefaultAsync(v => v.Id == dto.VehicleId, cancellationToken)
                ?? throw new InvalidOperationException("Select a valid vehicle.");

            Driver? driver = null;
            if (dto.DriverId.HasValue)
            {
                driver = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == dto.DriverId.Value, cancellationToken);
            }

            if (dto.Legs.Count == 0)
            {
                throw new InvalidOperationException("At least one trip leg is required.");
            }

            var legs = new List<TripLeg>();
            var gross = 0m;
            for (var i = 0; i < dto.Legs.Count; i++)
            {
                var input = dto.Legs[i];
                var rate = await _db.RouteRates
                    .AsNoTracking()
                    .Where(r => r.IsActive
                        && r.OriginState == input.OriginState
                        && r.DestinationState == input.DestinationState)
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                var legCost = rate == null
                    ? input.LegCost
                    : input.FuelOption == FuelOption.WithFuel ? rate.RateWithFuel : rate.RateWithoutFuel;

                if (legCost <= 0)
                {
                    throw new InvalidOperationException(
                        $"No active route price was found for {input.OriginState} to {input.DestinationState}.");
                }

                gross += legCost;
                legs.Add(new TripLeg
                {
                    LegNumber = i + 1,
                    OriginState = input.OriginState.Trim(),
                    DestinationState = input.DestinationState.Trim(),
                    FuelOption = (int)input.FuelOption,
                    LegCost = legCost,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var year = dto.DepartureDate.Year;
            var nextNumber = await _db.CharterBookings.CountAsync(cancellationToken) + 1;
            string bookingRef;
            do
            {
                bookingRef = $"CHT-{year}-{nextNumber:000}";
                nextNumber++;
            } while (await _db.CharterBookings.AnyAsync(b => b.BookingRef == bookingRef, cancellationToken));

            var booking = new CharterBooking
            {
                BookingRef = bookingRef,
                CustomerName = dto.CustomerName.Trim(),
                CustomerPhone = dto.CustomerPhone.Trim(),
                VehicleId = vehicle.Id,
                DriverId = driver?.Id,
                DriverName = driver?.FullName ?? vehicle.AssignedEmployee?.FullName ?? string.Empty,
                DriverAllowance = dto.DriverAllowance,
                SecurityEscortFee = dto.SecurityEscortFee,
                TollsAndRoadLevies = dto.TollsAndRoadLevies,
                GrossCharterFee = gross,
                TripStatus = (int)dto.TripStatus,
                DepartureDate = DateOnly.FromDateTime(dto.DepartureDate),
                ReturnDate = dto.ReturnDate.HasValue ? DateOnly.FromDateTime(dto.ReturnDate.Value) : null,
                Notes = dto.Notes.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TripLegs = legs
            };

            _db.CharterBookings.Add(booking);
            await _db.SaveChangesAsync(cancellationToken);

            booking.Vehicle = vehicle;
            booking.Driver = driver;
            return MapCharterBooking(booking);
        }

        public async Task<bool> UpdateCharterStatusAsync(
            int id,
            CharterTripStatus status,
            CancellationToken cancellationToken = default)
        {
            var booking = await _db.CharterBookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
            if (booking == null)
            {
                return false;
            }

            booking.TripStatus = (int)status;
            booking.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<List<WeeklyFleetExpenseDto>> GetWeeklyFleetExpensesAsync(
            DateTime weekStart,
            DateTime weekEnd,
            CancellationToken cancellationToken = default)
        {
            var start = weekStart.Date;
            var endExclusive = weekEnd.Date.AddDays(1);
            var startDate = DateOnly.FromDateTime(start);
            var endDate = DateOnly.FromDateTime(weekEnd.Date);

            var vehicles = await _db.Vehicles
                .AsNoTracking()
                .Include(v => v.AssignedEmployee)
                .Where(v => v.IsActive)
                .OrderBy(v => v.RegistrationNumber)
                .ToListAsync(cancellationToken);

            var fuelByVehicle = await _db.FuelLogs
                .AsNoTracking()
                .Where(l => l.FuelDate >= start && l.FuelDate < endExclusive)
                .GroupBy(l => l.VehicleId)
                .Select(g => new { VehicleId = g.Key, Total = g.Sum(x => x.TotalCost ?? 0m) })
                .ToDictionaryAsync(x => x.VehicleId, x => x.Total, cancellationToken);

            var maintenanceByVehicle = await _db.MaintenanceLogs
                .AsNoTracking()
                .Where(m => m.ServiceDate >= startDate && m.ServiceDate <= endDate)
                .GroupBy(m => m.VehicleId)
                .Select(g => new { VehicleId = g.Key, Total = g.Sum(x => x.Cost) })
                .ToDictionaryAsync(x => x.VehicleId, x => x.Total, cancellationToken);

            var charterByVehicle = await _db.CharterBookings
                .AsNoTracking()
                .Where(b => b.DepartureDate >= startDate && b.DepartureDate <= endDate)
                .GroupBy(b => b.VehicleId)
                .Select(g => new
                {
                    VehicleId = g.Key,
                    Total = g.Sum(x => x.DriverAllowance + x.SecurityEscortFee + x.TollsAndRoadLevies)
                })
                .ToDictionaryAsync(x => x.VehicleId, x => x.Total, cancellationToken);

            return vehicles.Select(vehicle =>
            {
                var fuel = fuelByVehicle.GetValueOrDefault(vehicle.Id);
                var maintenance = maintenanceByVehicle.GetValueOrDefault(vehicle.Id);
                var incidentals = charterByVehicle.GetValueOrDefault(vehicle.Id);

                return new WeeklyFleetExpenseDto
                {
                    VehicleId = vehicle.Id,
                    VehicleRegNumber = vehicle.RegistrationNumber,
                    DriverName = vehicle.AssignedEmployee?.FullName ?? "Unassigned",
                    OperatingWeekStart = start,
                    OperatingWeekEnd = weekEnd.Date,
                    FuelExpenseTotal = fuel,
                    MaintenanceExpenseTotal = maintenance,
                    IncidentalsTotal = incidentals,
                    TotalWeeklyRunningCost = fuel + maintenance + incidentals
                };
            }).ToList();
        }

        private static VehicleDto MapVehicle(Vehicle vehicle)
        {
            return new VehicleDto
            {
                Id = vehicle.Id,
                RegistrationNumber = vehicle.RegistrationNumber,
                MakeAndModel = vehicle.MakeAndModel,
                Type = vehicle.VehicleType,
                Function = vehicle.VehicleFunction,
                AssignedEmployeeId = vehicle.AssignedEmployeeId,
                AssignedDriverName = vehicle.AssignedEmployee?.FullName,
                DriverPhoneNumber = vehicle.AssignedEmployee?.PhoneNumber,
                FuelType = vehicle.FuelType,
                TankCapacityLitres = vehicle.TankCapacityLitres,
                CurrentOdometerKm = vehicle.CurrentOdometerKm,
                Status = vehicle.Status,
                LastServiceDate = vehicle.LastServiceDate?.ToDateTime(TimeOnly.MinValue)
            };
        }

        private static EmployeeDto MapEmployee(Employee employee)
        {
            return new EmployeeDto
            {
                Id = employee.Id,
                FullName = employee.FullName,
                StaffCode = employee.StaffCode,
                Department = employee.Department,
                Designation = employee.Designation,
                PhoneNumber = employee.PhoneNumber,
                Email = employee.Email,
                Address = employee.Address,
                BankName = employee.BankName,
                BankAccountNumber = employee.BankAccountNumber,
                EmploymentStatus = employee.EmploymentStatus,
                BaseSalary = employee.BaseSalary,
                EmergencyContactName = employee.EmergencyContactName,
                EmergencyContactPhone = employee.EmergencyContactPhone,
                EmergencyRelationship = employee.EmergencyRelationship,
                GuarantorName = employee.GuarantorName,
                GuarantorPhone = employee.GuarantorPhone,
                GuarantorAddress = employee.GuarantorAddress,
                GuarantorOccupation = employee.GuarantorOccupation,
                EmploymentDate = employee.EmploymentDate.ToDateTime(TimeOnly.MinValue),
                IsActive = employee.IsActive
            };
        }

        private static FuelLogDto MapFuelLog(FuelLog log)
        {
            return new FuelLogDto
            {
                Id = log.Id,
                VehicleId = log.VehicleId,
                VehicleRegNumber = log.Vehicle?.RegistrationNumber ?? string.Empty,
                DriverName = log.Driver?.FullName ?? string.Empty,
                Date = log.FuelDate,
                FuelType = log.FuelType,
                LitresDispensed = log.LitresDispensed,
                PricePerLitre = log.PricePerLitre,
                TotalCost = log.TotalCost ?? log.LitresDispensed * log.PricePerLitre,
                PetrolStation = log.PetrolStation,
                RouteDescription = log.RouteDescription
            };
        }

        private static RouteRateDto MapRouteRate(RouteRate rate)
        {
            return new RouteRateDto
            {
                Id = rate.Id,
                OriginState = rate.OriginState,
                DestinationState = rate.DestinationState,
                VehicleTypeRequired = rate.VehicleTypeRequired,
                RateWithFuel = rate.RateWithFuel,
                RateWithoutFuel = rate.RateWithoutFuel,
                EstimatedHours = rate.EstimatedHours,
                IsActive = rate.IsActive
            };
        }

        private static CharterBookingDto MapCharterBooking(CharterBooking booking)
        {
            return new CharterBookingDto
            {
                Id = booking.Id,
                BookingRef = booking.BookingRef,
                CustomerName = booking.CustomerName,
                CustomerPhone = booking.CustomerPhone,
                VehicleId = booking.VehicleId,
                VehicleRegNumber = booking.Vehicle?.RegistrationNumber ?? string.Empty,
                DriverId = booking.DriverId,
                DriverName = booking.DriverName,
                IsMultiLeg = booking.TripLegs.Count > 1,
                Legs = booking.TripLegs
                    .OrderBy(l => l.LegNumber)
                    .Select(l => new TripLegDto
                    {
                        LegNumber = l.LegNumber,
                        OriginState = l.OriginState,
                        DestinationState = l.DestinationState,
                        FuelOption = (FuelOption)l.FuelOption,
                        LegCost = l.LegCost
                    })
                    .ToList(),
                DriverAllowance = booking.DriverAllowance,
                SecurityEscortFee = booking.SecurityEscortFee,
                TollsAndRoadLevies = booking.TollsAndRoadLevies,
                GrossCharterFee = booking.GrossCharterFee,
                TotalTripIncidentals = booking.TotalTripIncidentals ?? 0m,
                NetCharterRevenue = booking.NetCharterRevenue ?? 0m,
                TripStatus = (CharterTripStatus)booking.TripStatus,
                DepartureDate = booking.DepartureDate.ToDateTime(TimeOnly.MinValue),
                ReturnDate = booking.ReturnDate?.ToDateTime(TimeOnly.MinValue),
                Notes = booking.Notes,
                CreatedAt = booking.CreatedAt
            };
        }

        private static bool IsLogisticsDepartment(string? department) =>
            !string.IsNullOrWhiteSpace(department) &&
            department.Contains("Logistic", StringComparison.OrdinalIgnoreCase);

        private static string NormalizeFuelType(string? fuelType)
        {
            if (string.IsNullOrWhiteSpace(fuelType))
            {
                return "Petrol";
            }

            var normalized = fuelType.Trim();
            var match = AllowedFuelTypes.FirstOrDefault(f => string.Equals(f, normalized, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                throw new InvalidOperationException("Fuel type must be Petrol, Diesel, or EV Charging.");
            }

            return match;
        }
    }
}
