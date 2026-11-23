using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Microsoft.EntityFrameworkCore;
using CharterBooking = Manufacture.Models.Entities.CharterBooking;
using Employee = Manufacture.Models.Entities.Employee;
using FuelLog = Manufacture.Models.Entities.FuelLog;
using MaintenanceLog = Manufacture.Models.Entities.MaintenanceLog;
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
                    .FirstOrDefaultAsync(e => e.Id == dto.AssignedEmployeeId.Value && !e.IsDeleted, cancellationToken);

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
                ImageUrl = dto.ImageUrl,
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
                .Where(e => !e.IsDeleted && e.IsActive && e.Department.Contains("Logistic"))
                .OrderBy(e => e.FullName)
                .ToListAsync(cancellationToken);

            return employees.Select(MapEmployee).ToList();
        }

        public async Task<List<FleetDriverDto>> GetDriversAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Employees
                .AsNoTracking()
                .Where(e => !e.IsDeleted && e.IsActive && e.Department.Contains("Logistic"))
                .OrderBy(e => e.FullName)
                .Select(e => new FleetDriverDto
                {
                    Id = e.Id,
                    FullName = e.FullName,
                    PhoneNumber = e.PhoneNumber,
                    LicenseNumber = string.Empty
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<FuelLogDto>> GetFuelLogsAsync(CancellationToken cancellationToken = default)
        {
            var logs = await _db.FuelLogs
                .AsNoTracking()
                .Include(l => l.Vehicle)
                .Include(l => l.Employee)
                .Include(l => l.CharterBooking)
                .Include(l => l.FleetDispatch)
                .OrderByDescending(l => l.FuelDate)
                .ThenByDescending(l => l.Id)
                .ToListAsync(cancellationToken);

            return logs.Select(MapFuelLog).ToList();
        }

        public async Task<List<FuelLogDto>> GetFuelLogsForCharterAsync(int charterBookingId, CancellationToken cancellationToken = default)
        {
            var logs = await _db.FuelLogs
                .AsNoTracking()
                .Include(l => l.Vehicle)
                .Include(l => l.Employee)
                .Include(l => l.CharterBooking)
                .Include(l => l.FleetDispatch)
                .Where(l => l.CharterBookingId == charterBookingId)
                .OrderByDescending(l => l.FuelDate)
                .ThenByDescending(l => l.Id)
                .ToListAsync(cancellationToken);

            return logs.Select(MapFuelLog).ToList();
        }

        public async Task<List<FuelLogDto>> GetFuelLogsForDispatchAsync(int fleetDispatchId, CancellationToken cancellationToken = default)
        {
            var logs = await _db.FuelLogs
                .AsNoTracking()
                .Include(l => l.Vehicle)
                .Include(l => l.Employee)
                .Include(l => l.CharterBooking)
                .Include(l => l.FleetDispatch)
                .Where(l => l.FleetDispatchId == fleetDispatchId)
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

            if (dto.CharterBookingId.HasValue && dto.FleetDispatchId.HasValue)
            {
                throw new InvalidOperationException("A fuel record cannot belong to both a charter and a dispatch.");
            }

            CharterBooking? charter = null;
            if (dto.CharterBookingId.HasValue)
            {
                charter = await _db.CharterBookings.FirstOrDefaultAsync(b => b.Id == dto.CharterBookingId.Value, cancellationToken)
                    ?? throw new InvalidOperationException("Charter booking not found.");
                if (charter.VehicleId != vehicle.Id)
                {
                    throw new InvalidOperationException("The fuel vehicle must match the charter vehicle.");
                }
            }

            FleetDispatch? dispatch = null;
            if (dto.FleetDispatchId.HasValue)
            {
                dispatch = await _db.FleetDispatches.FirstOrDefaultAsync(d => d.Id == dto.FleetDispatchId.Value, cancellationToken)
                    ?? throw new InvalidOperationException("Fleet dispatch not found.");
                if (dispatch.VehicleId != vehicle.Id)
                {
                    throw new InvalidOperationException("The fuel vehicle must match the dispatch vehicle.");
                }
            }

            var fuelType = NormalizeFuelType(string.IsNullOrWhiteSpace(dto.FuelType) ? vehicle.FuelType : dto.FuelType);

            var log = new FuelLog
            {
                VehicleId = vehicle.Id,
                CharterBookingId = charter?.Id,
                FleetDispatchId = dispatch?.Id,
                EmployeeId = dto.EmployeeId ?? vehicle.AssignedEmployeeId,
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
            log.CharterBooking = charter;
            log.FleetDispatch = dispatch;
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
                BeforeImageUrl = dto.BeforeImageUrl,
                AfterImageUrl = dto.AfterImageUrl,
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
                InvoiceNumber = log.InvoiceNumber,
                BeforeImageUrl = log.BeforeImageUrl,
                AfterImageUrl = log.AfterImageUrl
            };
        }

        public async Task<List<MaintenanceLogDto>> GetMaintenanceLogsAsync(CancellationToken cancellationToken = default)
        {
            var logs = await _db.MaintenanceLogs
                .AsNoTracking()
                .Include(m => m.Vehicle)
                .OrderByDescending(m => m.ServiceDate)
                .ThenByDescending(m => m.Id)
                .ToListAsync(cancellationToken);

            return logs.Select(log => MapMaintenance(log, log.Vehicle?.RegistrationNumber ?? string.Empty)).ToList();
        }

        public async Task<MaintenanceLogDto?> GetMaintenanceLogAsync(int id, CancellationToken cancellationToken = default)
        {
            var log = await _db.MaintenanceLogs
                .AsNoTracking()
                .Include(m => m.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

            return log == null ? null : MapMaintenance(log, log.Vehicle?.RegistrationNumber ?? string.Empty);
        }

        public async Task<List<CharterBookingDto>> GetCharterBookingsAsync(
            string? status = null,
            string? search = null,
            CancellationToken cancellationToken = default)
        {
            var query = _db.CharterBookings
                .AsNoTracking()
                .Include(b => b.Vehicle)
                .Include(b => b.QuoteRequest)
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
                .Include(b => b.QuoteRequest)
                .Include(b => b.TripLegs)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

            return booking == null ? null : MapCharterBooking(booking);
        }

        public async Task<CharterBookingDto> CreateCharterBookingAsync(
            CharterBookingDto dto,
            CancellationToken cancellationToken = default)
        {
            if (!dto.QuoteRequestId.HasValue)
            {
                throw new InvalidOperationException("An approved client quote is required before booking a charter.");
            }

            var vehicle = await _db.Vehicles
                .Include(v => v.AssignedEmployee)
                .FirstOrDefaultAsync(v => v.Id == dto.VehicleId, cancellationToken)
                ?? throw new InvalidOperationException("Select a valid vehicle.");

            Employee? driverEmployee = null;
            if (dto.DriverEmployeeId.HasValue)
            {
                driverEmployee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == dto.DriverEmployeeId.Value && !e.IsDeleted, cancellationToken);
            }

            var assignedDriver = driverEmployee ?? vehicle.AssignedEmployee;

            var quote = await _db.CharterQuoteRequests
                .Include(q => q.CharterQuoteLegs)
                .FirstOrDefaultAsync(q => q.Id == dto.QuoteRequestId.Value, cancellationToken)
                ?? throw new InvalidOperationException("The selected quote was not found.");

            if (quote.Status != (int)CharterQuoteStatus.Approved)
            {
                throw new InvalidOperationException("Only approved quotes can be converted to a booking.");
            }

            if (quote.ValidUntil.HasValue && quote.ValidUntil.Value < DateOnly.FromDateTime(DateTime.UtcNow.Date))
            {
                throw new InvalidOperationException("This quote has expired. Create a new quote or clone the expired quote.");
            }

            if (await _db.CharterBookings.AnyAsync(b => b.QuoteRequestId == quote.Id, cancellationToken))
            {
                throw new InvalidOperationException("This quote has already been converted to a booking.");
            }

            if (quote.CharterQuoteLegs.Count == 0)
            {
                throw new InvalidOperationException("The selected quote has no trip legs.");
            }

            var gross = quote.QuotedAmount;
            if (gross <= 0)
            {
                throw new InvalidOperationException("The selected quote has no billable amount.");
            }

            var legs = quote.CharterQuoteLegs
                .OrderBy(l => l.LegNumber)
                .Select(l => new TripLeg
                {
                    LegNumber = l.LegNumber,
                    OriginState = l.OriginCityState.Trim(),
                    OriginAddress = l.OriginAddress.Trim(),
                    DestinationState = l.DestinationCityState.Trim(),
                    DestinationAddress = l.DestinationAddress.Trim(),
                    DistanceKm = l.DistanceKm,
                    FuelOption = l.FuelOption,
                    LegCost = l.QuotedLegAmount,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

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
                CustomerName = quote.CustomerName,
                CustomerPhone = quote.CustomerPhone,
                VehicleId = vehicle.Id,
                QuoteRequestId = quote.Id,
                DriverEmployeeId = assignedDriver?.Id,
                DriverName = assignedDriver?.FullName ?? string.Empty,
                DriverAllowance = quote.DriverAllowance,
                SecurityEscortFee = quote.SecurityEscortFee,
                TollsAndRoadLevies = quote.TollsAndRoadLevies,
                GrossCharterFee = gross,
                TripStatus = (int)CharterTripStatus.Booked,
                DepartureDate = DateOnly.FromDateTime(dto.DepartureDate),
                ReturnDate = dto.ReturnDate.HasValue ? DateOnly.FromDateTime(dto.ReturnDate.Value) : null,
                Notes = dto.Notes?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TripLegs = legs
            };

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            _db.CharterBookings.Add(booking);
            quote.Status = (int)CharterQuoteStatus.Converted;
            quote.UpdatedAt = DateTime.UtcNow;
            _db.AuditLogs.Add(new Manufacture.Models.Entities.AuditLog
            {
                Action = "CharterQuoteConverted",
                EntityName = "CharterQuoteRequest",
                EntityId = quote.Id.ToString(),
                NewValues = $"Quote {quote.QuoteRef} converted to booking {bookingRef}.",
                OccurredAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            booking.Vehicle = vehicle;
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
                ImageUrl = vehicle.ImageUrl,
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
                PassportUrl = employee.PassportUrl ?? string.Empty,
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
                CharterBookingId = log.CharterBookingId,
                CharterBookingRef = log.CharterBooking?.BookingRef ?? string.Empty,
                FleetDispatchId = log.FleetDispatchId,
                FleetDispatchRef = log.FleetDispatch?.DispatchRef ?? string.Empty,
                EmployeeId = log.EmployeeId,
                DriverName = log.Employee?.FullName ?? string.Empty,
                Date = log.FuelDate,
                FuelType = log.FuelType,
                LitresDispensed = log.LitresDispensed,
                PricePerLitre = log.PricePerLitre,
                TotalCost = log.TotalCost ?? log.LitresDispensed * log.PricePerLitre,
                PetrolStation = log.PetrolStation,
                RouteDescription = log.RouteDescription
            };
        }

        private static MaintenanceLogDto MapMaintenance(MaintenanceLog log, string vehicleRegNumber)
        {
            return new MaintenanceLogDto
            {
                Id = log.Id,
                VehicleId = log.VehicleId,
                VehicleRegNumber = vehicleRegNumber,
                ServiceDate = log.ServiceDate.ToDateTime(TimeOnly.MinValue),
                Description = log.Description,
                Cost = log.Cost,
                WorkshopOrVendor = log.WorkshopOrVendor,
                InvoiceNumber = log.InvoiceNumber,
                BeforeImageUrl = log.BeforeImageUrl,
                AfterImageUrl = log.AfterImageUrl
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
                QuoteRequestId = booking.QuoteRequestId,
                QuoteRef = booking.QuoteRequest?.QuoteRef ?? string.Empty,
                TotalDistanceKm = booking.TripLegs.Where(l => l.DistanceKm.HasValue).Sum(l => l.DistanceKm!.Value),
                DriverEmployeeId = booking.DriverEmployeeId,
                DriverName = booking.DriverName,
                IsMultiLeg = booking.TripLegs.Count > 1,
                Legs = booking.TripLegs
                    .OrderBy(l => l.LegNumber)
                    .Select(l => new TripLegDto
                    {
                        LegNumber = l.LegNumber,
                        OriginState = l.OriginState,
                        OriginAddress = l.OriginAddress,
                        DestinationState = l.DestinationState,
                        DestinationAddress = l.DestinationAddress,
                        DistanceKm = l.DistanceKm,
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
