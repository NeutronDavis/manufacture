using Manufacture.Models.Domain;
using Manufacture.Models.DTOs;

namespace Manufacture.Library.Interfaces
{
    public interface IFleetService
    {
        Task<List<VehicleDto>> GetVehiclesAsync(CancellationToken cancellationToken = default);

        Task<VehicleDto?> GetVehicleAsync(int id, CancellationToken cancellationToken = default);

        Task<VehicleDto> CreateVehicleAsync(VehicleDto dto, CancellationToken cancellationToken = default);

        Task<List<EmployeeDto>> GetLogisticsDriversAsync(CancellationToken cancellationToken = default);

        Task<List<FleetDriverDto>> GetDriversAsync(CancellationToken cancellationToken = default);

        Task<List<FuelLogDto>> GetFuelLogsAsync(CancellationToken cancellationToken = default);

        Task<List<FuelLogDto>> GetFuelLogsForCharterAsync(int charterBookingId, CancellationToken cancellationToken = default);

        Task<List<FuelLogDto>> GetFuelLogsForDispatchAsync(int fleetDispatchId, CancellationToken cancellationToken = default);

        Task<FuelLogDto> CreateFuelLogAsync(FuelLogDto dto, CancellationToken cancellationToken = default);

        Task<MaintenanceLogDto> CreateMaintenanceLogAsync(MaintenanceLogDto dto, CancellationToken cancellationToken = default);

        Task<List<MaintenanceLogDto>> GetMaintenanceLogsAsync(CancellationToken cancellationToken = default);

        Task<MaintenanceLogDto?> GetMaintenanceLogAsync(int id, CancellationToken cancellationToken = default);

        Task<List<CharterBookingDto>> GetCharterBookingsAsync(string? status = null, string? search = null, CancellationToken cancellationToken = default);

        Task<CharterBookingDto?> GetCharterBookingAsync(int id, CancellationToken cancellationToken = default);

        Task<CharterBookingDto> CreateCharterBookingAsync(CharterBookingDto dto, CancellationToken cancellationToken = default);

        Task<bool> UpdateCharterStatusAsync(int id, CharterTripStatus status, CancellationToken cancellationToken = default);

        Task<List<WeeklyFleetExpenseDto>> GetWeeklyFleetExpensesAsync(DateTime weekStart, DateTime weekEnd, CancellationToken cancellationToken = default);
    }
}
