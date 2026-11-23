using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using MeasurementDensityEntity = Manufacture.Models.Entities.MeasurementDensity;

namespace Manufacture.Services
{
    public class MeasurementDensityService : IMeasurementDensityService
    {
        private readonly ManufactureDbContext _db;

        public MeasurementDensityService(ManufactureDbContext db)
        {
            _db = db;
        }

        public async Task<List<MeasurementDensityDto>> GetAllAsync(
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var query = _db.MeasurementDensities.AsNoTracking().AsQueryable();
            if (!includeInactive)
                query = query.Where(d => d.IsActive);

            var rows = await query
                .OrderBy(d => d.MaterialName)
                .ToListAsync(cancellationToken);

            return rows.Select(Map).ToList();
        }

        public async Task<MeasurementDensityDto?> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.MeasurementDensities
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            return entity == null ? null : Map(entity);
        }

        public async Task<MeasurementDensityDto> SaveAsync(
            MeasurementDensityDto dto,
            CancellationToken cancellationToken = default)
        {
            var materialName = dto.MaterialName?.Trim() ?? string.Empty;
            if (materialName.Length == 0)
                throw new InvalidOperationException("Material name is required.");
            if (dto.DensityKgPerLitre <= 0m)
                throw new InvalidOperationException("Density must be greater than zero.");

            if (await _db.MeasurementDensities.AnyAsync(
                    d => d.MaterialName == materialName && d.Id != dto.Id,
                    cancellationToken))
            {
                throw new InvalidOperationException("A density record with that raw material name already exists.");
            }

            MeasurementDensityEntity entity;
            if (dto.Id > 0)
            {
                entity = await _db.MeasurementDensities
                    .FirstOrDefaultAsync(d => d.Id == dto.Id, cancellationToken)
                    ?? throw new InvalidOperationException("Density record not found.");
            }
            else
            {
                entity = new MeasurementDensityEntity { CreatedAt = DateTime.UtcNow };
                _db.MeasurementDensities.Add(entity);
            }

            entity.MaterialName = materialName;
            entity.DensityKgPerLitre = dto.DensityKgPerLitre;
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            // Keep linked inventory items' mirrored density current.
            var linkedItems = await _db.InventoryItems
                .Where(i => i.MeasurementDensityId == entity.Id)
                .ToListAsync(cancellationToken);

            foreach (var item in linkedItems)
            {
                item.DensityKgPerLitre = entity.DensityKgPerLitre;
                item.UpdatedAt = DateTime.UtcNow;
            }

            if (linkedItems.Count > 0)
                await _db.SaveChangesAsync(cancellationToken);

            return Map(entity);
        }

        public async Task<bool> ToggleStatusAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.MeasurementDensities
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (entity == null) return false;

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static MeasurementDensityDto Map(MeasurementDensityEntity entity) => new()
        {
            Id = entity.Id,
            MaterialName = entity.MaterialName,
            DensityKgPerLitre = entity.DensityKgPerLitre,
            IsActive = entity.IsActive
        };
    }
}
