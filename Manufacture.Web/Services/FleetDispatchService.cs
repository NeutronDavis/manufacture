using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Manufacture.Services
{
    public class FleetDispatchService : IFleetDispatchService
    {
        private static readonly string[] AllowedVehicleStatuses = { "Active", "InMaintenance", "Grounded", "InTransit" };

        private readonly ManufactureDbContext _db;

        public FleetDispatchService(ManufactureDbContext db)
        {
            _db = db;
        }

        public async Task<List<FleetDispatchDto>> GetDispatchesAsync(
            string? status = null,
            string? search = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? vehicleId = null,
            int? driverId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _db.FleetDispatches
                .AsNoTracking()
                .Include(d => d.Vehicle)
                .Include(d => d.DriverEmployee)
                .Include(d => d.CreatedByUser)
                .Include(d => d.FleetDispatchStops)
                .Include(d => d.FuelLogs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status)
                && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase)
                && Enum.TryParse<FleetDispatchStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(d => d.Status == (int)parsedStatus);
            }

            if (fromDate.HasValue)
            {
                var from = DateOnly.FromDateTime(fromDate.Value.Date);
                query = query.Where(d => d.DispatchDate >= from);
            }

            if (toDate.HasValue)
            {
                var to = DateOnly.FromDateTime(toDate.Value.Date);
                query = query.Where(d => d.DispatchDate <= to);
            }

            if (vehicleId.HasValue) query = query.Where(d => d.VehicleId == vehicleId.Value);
            if (driverId.HasValue) query = query.Where(d => d.DriverEmployeeId == driverId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(d =>
                    d.DispatchRef.ToLower().Contains(term) ||
                    d.Vehicle.RegistrationNumber.ToLower().Contains(term) ||
                    d.DriverName.ToLower().Contains(term) ||
                    d.FleetDispatchStops.Any(s =>
                        s.DeliveryPointName.ToLower().Contains(term) ||
                        s.Address.ToLower().Contains(term) ||
                        s.CityState.ToLower().Contains(term)));
            }

            var dispatches = await query
                .OrderByDescending(d => d.DispatchDate)
                .ThenByDescending(d => d.Id)
                .ToListAsync(cancellationToken);

            return dispatches.Select(MapDispatch).ToList();
        }

        public async Task<FleetDispatchDto?> GetDispatchAsync(int id, CancellationToken cancellationToken = default)
        {
            var dispatch = await _db.FleetDispatches
                .AsNoTracking()
                .Include(d => d.Vehicle)
                .Include(d => d.DriverEmployee)
                .Include(d => d.CreatedByUser)
                .Include(d => d.FleetDispatchStops)
                .Include(d => d.FuelLogs)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            return dispatch == null ? null : MapDispatch(dispatch);
        }

        public async Task<FleetDispatchDto> CreateDispatchAsync(
            FleetDispatchDto dto,
            int createdByUserId,
            CancellationToken cancellationToken = default)
        {
            ValidateStops(dto);

            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == dto.VehicleId, cancellationToken)
                ?? throw new InvalidOperationException("Select a valid vehicle.");

            Employee? driver = null;
            if (dto.DriverEmployeeId.HasValue)
            {
                driver = await _db.Employees.FirstOrDefaultAsync(e => e.Id == dto.DriverEmployeeId.Value && !e.IsDeleted, cancellationToken);
            }
            driver ??= await _db.Employees.FirstOrDefaultAsync(e => e.Id == vehicle.AssignedEmployeeId, cancellationToken);

            var dispatch = new FleetDispatch
            {
                DispatchRef = await GenerateRefAsync(cancellationToken),
                DispatchDate = DateOnly.FromDateTime(dto.DispatchDate),
                VehicleId = vehicle.Id,
                DriverEmployeeId = driver?.Id,
                DriverName = driver?.FullName ?? string.Empty,
                Purpose = dto.Purpose?.Trim() ?? string.Empty,
                Status = (int)FleetDispatchStatus.Planned,
                Notes = dto.Notes?.Trim() ?? string.Empty,
                CreatedByUserId = createdByUserId > 0 ? createdByUserId : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                FleetDispatchStops = dto.Stops.Select((stop, index) => MapStop(stop, index + 1)).ToList()
            };

            _db.FleetDispatches.Add(dispatch);
            await _db.SaveChangesAsync(cancellationToken);

            return (await GetDispatchAsync(dispatch.Id, cancellationToken))!;
        }

        public async Task<FleetDispatchDto> UpdateDispatchAsync(
            FleetDispatchDto dto,
            CancellationToken cancellationToken = default)
        {
            var dispatch = await _db.FleetDispatches
                .Include(d => d.FleetDispatchStops)
                .FirstOrDefaultAsync(d => d.Id == dto.Id, cancellationToken)
                ?? throw new InvalidOperationException("Dispatch not found.");

            if (dispatch.Status is (int)FleetDispatchStatus.Completed or (int)FleetDispatchStatus.Cancelled)
            {
                throw new InvalidOperationException("Completed or cancelled dispatches cannot be edited.");
            }

            ValidateStops(dto);
            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == dto.VehicleId, cancellationToken)
                ?? throw new InvalidOperationException("Select a valid vehicle.");

            Employee? driver = null;
            if (dto.DriverEmployeeId.HasValue)
            {
                driver = await _db.Employees.FirstOrDefaultAsync(e => e.Id == dto.DriverEmployeeId.Value && !e.IsDeleted, cancellationToken);
            }

            dispatch.DispatchDate = DateOnly.FromDateTime(dto.DispatchDate);
            dispatch.VehicleId = vehicle.Id;
            dispatch.DriverEmployeeId = driver?.Id;
            dispatch.DriverName = driver?.FullName ?? string.Empty;
            dispatch.Purpose = dto.Purpose?.Trim() ?? string.Empty;
            dispatch.Notes = dto.Notes?.Trim() ?? string.Empty;
            dispatch.UpdatedAt = DateTime.UtcNow;

            _db.FleetDispatchStops.RemoveRange(dispatch.FleetDispatchStops);
            dispatch.FleetDispatchStops = dto.Stops.Select((stop, index) => MapStop(stop, index + 1)).ToList();
            await _db.SaveChangesAsync(cancellationToken);

            return (await GetDispatchAsync(dispatch.Id, cancellationToken))!;
        }

        public async Task<FleetDispatchDto> StartDispatchAsync(int id, CancellationToken cancellationToken = default)
        {
            var dispatch = await _db.FleetDispatches.Include(d => d.Vehicle).FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("Dispatch not found.");

            if (dispatch.Status is (int)FleetDispatchStatus.Completed or (int)FleetDispatchStatus.Cancelled)
            {
                throw new InvalidOperationException("Completed or cancelled dispatches cannot be started.");
            }

            if (dispatch.Vehicle.Status is "InMaintenance" or "Grounded")
            {
                throw new InvalidOperationException("This vehicle is not available for dispatch. Change its status first.");
            }

            dispatch.Status = (int)FleetDispatchStatus.InTransit;
            dispatch.UpdatedAt = DateTime.UtcNow;
            dispatch.Vehicle.Status = "InTransit";
            dispatch.Vehicle.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            return (await GetDispatchAsync(id, cancellationToken))!;
        }

        public async Task<FleetDispatchDto> UpdateStopStatusAsync(
            int dispatchId,
            int stopId,
            FleetDispatchStopStatus status,
            CancellationToken cancellationToken = default)
        {
            var stop = await _db.FleetDispatchStops
                .FirstOrDefaultAsync(s => s.Id == stopId && s.FleetDispatchId == dispatchId, cancellationToken)
                ?? throw new InvalidOperationException("Delivery point not found.");

            stop.Status = (int)status;
            stop.DeliveredAt = status == FleetDispatchStopStatus.Delivered ? DateTime.UtcNow : null;
            stop.UpdatedAt = DateTime.UtcNow;

            var dispatch = await _db.FleetDispatches
                .Include(d => d.FleetDispatchStops)
                .FirstOrDefaultAsync(d => d.Id == dispatchId, cancellationToken);

            if (dispatch is not null)
            {
                var allDelivered = dispatch.FleetDispatchStops.Count > 0
                    && dispatch.FleetDispatchStops.All(s => s.Status == (int)FleetDispatchStopStatus.Delivered);
                if (dispatch.Status is not (int)FleetDispatchStatus.Completed and not (int)FleetDispatchStatus.Cancelled)
                {
                    dispatch.Status = (int)(allDelivered ? FleetDispatchStatus.Delivered : FleetDispatchStatus.InTransit);
                    dispatch.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetDispatchAsync(dispatchId, cancellationToken))!;
        }

        public async Task<FleetDispatchDto> CompleteDispatchAsync(int id, CancellationToken cancellationToken = default)
        {
            var dispatch = await _db.FleetDispatches.Include(d => d.Vehicle).FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("Dispatch not found.");

            dispatch.Status = (int)FleetDispatchStatus.Completed;
            dispatch.UpdatedAt = DateTime.UtcNow;
            if (dispatch.Vehicle.Status == "InTransit")
            {
                dispatch.Vehicle.Status = "Active";
                dispatch.Vehicle.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetDispatchAsync(id, cancellationToken))!;
        }

        public async Task<FleetDispatchDto> CancelDispatchAsync(int id, CancellationToken cancellationToken = default)
        {
            var dispatch = await _db.FleetDispatches.Include(d => d.Vehicle).FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("Dispatch not found.");

            dispatch.Status = (int)FleetDispatchStatus.Cancelled;
            dispatch.UpdatedAt = DateTime.UtcNow;
            if (dispatch.Vehicle.Status == "InTransit")
            {
                dispatch.Vehicle.Status = "Active";
                dispatch.Vehicle.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetDispatchAsync(id, cancellationToken))!;
        }

        public async Task UpdateVehicleStatusAsync(int vehicleId, string status, CancellationToken cancellationToken = default)
        {
            var normalized = AllowedVehicleStatuses.FirstOrDefault(value =>
                string.Equals(value, status, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Invalid vehicle status.");

            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId, cancellationToken)
                ?? throw new InvalidOperationException("Vehicle not found.");

            vehicle.Status = normalized;
            vehicle.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        private static void ValidateStops(FleetDispatchDto dto)
        {
            dto.Stops ??= new List<FleetDispatchStopDto>();
            if (dto.Stops.Count == 0)
            {
                throw new InvalidOperationException("At least one delivery point is required.");
            }

            for (var i = 0; i < dto.Stops.Count; i++)
            {
                var stop = dto.Stops[i];
                if (string.IsNullOrWhiteSpace(stop.DeliveryPointName))
                {
                    throw new InvalidOperationException($"Delivery point {i + 1}: name is required.");
                }

                if (string.IsNullOrWhiteSpace(stop.Address))
                {
                    throw new InvalidOperationException($"Delivery point {i + 1}: address is required.");
                }
            }
        }

        private async Task<string> GenerateRefAsync(CancellationToken cancellationToken)
        {
            var year = DateTime.UtcNow.Year;
            var next = await _db.FleetDispatches.CountAsync(cancellationToken) + 1;
            string reference;
            do
            {
                reference = $"DSP-{year}-{next:000}";
                next++;
            } while (await _db.FleetDispatches.AnyAsync(d => d.DispatchRef == reference, cancellationToken));

            return reference;
        }

        private static FleetDispatchStop MapStop(FleetDispatchStopDto stop, int number) => new()
        {
            StopNumber = number,
            DeliveryPointName = stop.DeliveryPointName.Trim(),
            Address = stop.Address.Trim(),
            CityState = stop.CityState?.Trim() ?? string.Empty,
            ContactName = string.IsNullOrWhiteSpace(stop.ContactName) ? null : stop.ContactName.Trim(),
            ContactPhone = string.IsNullOrWhiteSpace(stop.ContactPhone) ? null : stop.ContactPhone.Trim(),
            Status = (int)stop.Status,
            DeliveredAt = stop.DeliveredAt,
            Notes = string.IsNullOrWhiteSpace(stop.Notes) ? null : stop.Notes.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        private static FleetDispatchDto MapDispatch(FleetDispatch dispatch) => new()
        {
            Id = dispatch.Id,
            DispatchRef = dispatch.DispatchRef,
            DispatchDate = dispatch.DispatchDate.ToDateTime(TimeOnly.MinValue),
            VehicleId = dispatch.VehicleId,
            VehicleRegNumber = dispatch.Vehicle?.RegistrationNumber ?? string.Empty,
            DriverEmployeeId = dispatch.DriverEmployeeId,
            DriverName = dispatch.DriverName,
            Purpose = dispatch.Purpose,
            Status = (FleetDispatchStatus)dispatch.Status,
            OdometerEnd = dispatch.OdometerEnd,
            Notes = dispatch.Notes,
            CreatedByUserId = dispatch.CreatedByUserId,
            CreatedByName = dispatch.CreatedByUser?.Name ?? string.Empty,
            CreatedAt = dispatch.CreatedAt,
            Stops = dispatch.FleetDispatchStops
                .OrderBy(s => s.StopNumber)
                .Select(s => new FleetDispatchStopDto
                {
                    Id = s.Id,
                    FleetDispatchId = s.FleetDispatchId,
                    StopNumber = s.StopNumber,
                    DeliveryPointName = s.DeliveryPointName,
                    Address = s.Address,
                    CityState = s.CityState,
                    ContactName = s.ContactName,
                    ContactPhone = s.ContactPhone,
                    Status = (FleetDispatchStopStatus)s.Status,
                    DeliveredAt = s.DeliveredAt,
                    Notes = s.Notes
                }).ToList(),
            FuelLogs = dispatch.FuelLogs.Select(log => new FuelLogDto
            {
                Id = log.Id,
                VehicleId = log.VehicleId,
                VehicleRegNumber = dispatch.Vehicle?.RegistrationNumber ?? string.Empty,
                FleetDispatchId = log.FleetDispatchId,
                FleetDispatchRef = dispatch.DispatchRef,
                EmployeeId = log.EmployeeId,
                DriverName = dispatch.DriverName,
                Date = log.FuelDate,
                FuelType = log.FuelType,
                LitresDispensed = log.LitresDispensed,
                PricePerLitre = log.PricePerLitre,
                TotalCost = log.TotalCost ?? 0m,
                PetrolStation = log.PetrolStation,
                RouteDescription = log.RouteDescription
            }).ToList()
        };
    }
}
