using Manufacture.Models.DTOs;

namespace Manufacture.Library.Interfaces
{
    public interface IMeasurementDensityService
    {
        Task<List<MeasurementDensityDto>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

        Task<MeasurementDensityDto?> GetAsync(int id, CancellationToken cancellationToken = default);

        Task<MeasurementDensityDto> SaveAsync(MeasurementDensityDto dto, CancellationToken cancellationToken = default);

        Task<bool> ToggleStatusAsync(int id, CancellationToken cancellationToken = default);
    }
}
