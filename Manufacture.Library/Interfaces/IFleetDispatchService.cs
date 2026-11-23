using Manufacture.Models.DTOs;

namespace Manufacture.Library.Interfaces
{
    public interface IFleetDispatchService
    {
        Task<List<FleetDispatchDto>> GetDispatchesAsync(
            string? status = null,
            string? search = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? vehicleId = null,
            int? driverId = null,
            CancellationToken cancellationToken = default);

        Task<FleetDispatchDto?> GetDispatchAsync(int id, CancellationToken cancellationToken = default);

        Task<List<FleetDispatchDto>> GetAvailableForSalesRouteAsync(CancellationToken cancellationToken = default);

        Task<FleetDispatchDto> CreateDispatchAsync(
            FleetDispatchDto dto,
            int createdByUserId,
            CancellationToken cancellationToken = default);

        Task<FleetDispatchDto> UpdateDispatchAsync(
            FleetDispatchDto dto,
            CancellationToken cancellationToken = default);

        Task<FleetDispatchDto> StartDispatchAsync(int id, CancellationToken cancellationToken = default);

        Task<FleetDispatchDto> UpdateStopStatusAsync(
            int dispatchId,
            int stopId,
            FleetDispatchStopStatus status,
            CancellationToken cancellationToken = default);

        Task<FleetDispatchDto> CompleteDispatchAsync(int id, CancellationToken cancellationToken = default);

        Task<FleetDispatchDto> CancelDispatchAsync(int id, CancellationToken cancellationToken = default);

        Task UpdateVehicleStatusAsync(int vehicleId, string status, CancellationToken cancellationToken = default);
    }
}
