using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.DTOs;
using Manufacture.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Manufacture.Services
{
    public class CharterQuoteService : ICharterQuoteService
    {
        private readonly ManufactureDbContext _db;

        public CharterQuoteService(ManufactureDbContext db)
        {
            _db = db;
        }

        public async Task<List<CharterQuoteDto>> GetCharterQuotesAsync(
            string? status = null,
            string? search = null,
            CancellationToken cancellationToken = default)
        {
            await MarkExpiredAsync(cancellationToken);

            var query = _db.CharterQuoteRequests
                .AsNoTracking()
                .Include(q => q.CharterQuoteLegs)
                .Include(q => q.CharterBookings)
                .Include(q => q.RequestedByUser)
                .Include(q => q.ApprovedByUser)
                .Include(q => q.Vehicle)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status)
                && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase)
                && Enum.TryParse<CharterQuoteStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(q => q.Status == (int)parsedStatus);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(q =>
                    q.QuoteRef.ToLower().Contains(term) ||
                    q.CustomerName.ToLower().Contains(term) ||
                    q.CustomerPhone.ToLower().Contains(term) ||
                    q.VehicleTypeRequired.ToLower().Contains(term) ||
                    q.CharterQuoteLegs.Any(l =>
                        l.OriginAddress.ToLower().Contains(term) ||
                        l.DestinationAddress.ToLower().Contains(term) ||
                        l.OriginCityState.ToLower().Contains(term) ||
                        l.DestinationCityState.ToLower().Contains(term)));
            }

            var quotes = await query
                .OrderByDescending(q => q.RequestDate)
                .ThenByDescending(q => q.Id)
                .ToListAsync(cancellationToken);

            return quotes.Select(MapQuote).ToList();
        }

        public async Task<CharterQuoteDto?> GetCharterQuoteAsync(int id, CancellationToken cancellationToken = default)
        {
            await MarkExpiredAsync(cancellationToken);

            var quote = await _db.CharterQuoteRequests
                .AsNoTracking()
                .Include(q => q.CharterQuoteLegs)
                .Include(q => q.CharterBookings)
                .Include(q => q.RequestedByUser)
                .Include(q => q.ApprovedByUser)
                .Include(q => q.Vehicle)
                .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

            return quote == null ? null : MapQuote(quote);
        }

        public async Task<List<CharterQuoteDto>> GetApprovedCharterQuotesAsync(CancellationToken cancellationToken = default)
        {
            await MarkExpiredAsync(cancellationToken);

            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var quotes = await _db.CharterQuoteRequests
                .AsNoTracking()
                .Include(q => q.CharterQuoteLegs)
                .Include(q => q.CharterBookings)
                .Include(q => q.RequestedByUser)
                .Include(q => q.ApprovedByUser)
                .Include(q => q.Vehicle)
                .Where(q => q.Status == (int)CharterQuoteStatus.Approved
                    && (!q.ValidUntil.HasValue || q.ValidUntil.Value >= today))
                .OrderByDescending(q => q.ApprovedAt)
                .ThenByDescending(q => q.Id)
                .ToListAsync(cancellationToken);

            return quotes.Select(MapQuote).ToList();
        }

        public async Task<CharterQuoteDto> CreateCharterQuoteAsync(
            CharterQuoteDto dto,
            int requestedByUserId,
            CancellationToken cancellationToken = default)
        {
            NormalizeLegs(dto);
            var errors = CharterQuoteCalculator.Validate(dto);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join(" ", errors));
            }

            var totals = CharterQuoteCalculator.Calculate(dto.Legs, dto.DriverAllowance, dto.SecurityEscortFee, dto.TollsAndRoadLevies);
            var quote = new CharterQuoteRequest
            {
                QuoteRef = await GenerateQuoteRefAsync(cancellationToken),
                CustomerName = dto.CustomerName.Trim(),
                CustomerPhone = dto.CustomerPhone.Trim(),
                CustomerEmail = string.IsNullOrWhiteSpace(dto.CustomerEmail) ? null : dto.CustomerEmail.Trim(),
                RequestDate = DateTime.UtcNow,
                TripType = (int)dto.TripType,
                VehicleTypeRequired = dto.VehicleTypeRequired.Trim(),
                VehicleId = dto.VehicleId,
                TotalDistanceKm = totals.TotalDistanceKm,
                QuotedAmount = totals.QuotedAmount,
                DriverAllowance = dto.DriverAllowance,
                SecurityEscortFee = dto.SecurityEscortFee,
                TollsAndRoadLevies = dto.TollsAndRoadLevies,
                Status = (int)CharterQuoteStatus.Draft,
                ValidUntil = dto.ValidUntil.HasValue ? DateOnly.FromDateTime(dto.ValidUntil.Value) : null,
                Notes = dto.Notes?.Trim() ?? string.Empty,
                RequestedByUserId = requestedByUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            quote.CharterQuoteLegs = dto.Legs.Select((leg, index) => MapLeg(leg, index + 1)).ToList();
            _db.CharterQuoteRequests.Add(quote);
            await _db.SaveChangesAsync(cancellationToken);

            return (await GetCharterQuoteAsync(quote.Id, cancellationToken))!;
        }

        public async Task<CharterQuoteDto> UpdateCharterQuoteAsync(
            CharterQuoteDto dto,
            int requestedByUserId,
            CancellationToken cancellationToken = default)
        {
            var quote = await _db.CharterQuoteRequests
                .Include(q => q.CharterQuoteLegs)
                .FirstOrDefaultAsync(q => q.Id == dto.Id, cancellationToken)
                ?? throw new InvalidOperationException("Quote not found.");

            if (quote.Status is not ((int)CharterQuoteStatus.Draft) and not ((int)CharterQuoteStatus.Rejected))
            {
                throw new InvalidOperationException("Only draft or rejected quotes can be edited.");
            }

            NormalizeLegs(dto);
            var errors = CharterQuoteCalculator.Validate(dto);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join(" ", errors));
            }

            var totals = CharterQuoteCalculator.Calculate(dto.Legs, dto.DriverAllowance, dto.SecurityEscortFee, dto.TollsAndRoadLevies);
            quote.CustomerName = dto.CustomerName.Trim();
            quote.CustomerPhone = dto.CustomerPhone.Trim();
            quote.CustomerEmail = string.IsNullOrWhiteSpace(dto.CustomerEmail) ? null : dto.CustomerEmail.Trim();
            quote.TripType = (int)dto.TripType;
            quote.VehicleTypeRequired = dto.VehicleTypeRequired.Trim();
            quote.VehicleId = dto.VehicleId;
            quote.TotalDistanceKm = totals.TotalDistanceKm;
            quote.QuotedAmount = totals.QuotedAmount;
            quote.DriverAllowance = dto.DriverAllowance;
            quote.SecurityEscortFee = dto.SecurityEscortFee;
            quote.TollsAndRoadLevies = dto.TollsAndRoadLevies;
            quote.ValidUntil = dto.ValidUntil.HasValue ? DateOnly.FromDateTime(dto.ValidUntil.Value) : null;
            quote.Notes = dto.Notes?.Trim() ?? string.Empty;
            quote.RequestedByUserId = quote.RequestedByUserId ?? requestedByUserId;
            quote.Status = (int)CharterQuoteStatus.Draft;
            quote.RejectionReason = null;
            quote.SubmittedAt = null;
            quote.ApprovedAt = null;
            quote.ApprovedByUserId = null;
            quote.UpdatedAt = DateTime.UtcNow;

            _db.CharterQuoteLegs.RemoveRange(quote.CharterQuoteLegs);
            quote.CharterQuoteLegs = dto.Legs.Select((leg, index) => MapLeg(leg, index + 1)).ToList();
            await _db.SaveChangesAsync(cancellationToken);

            return (await GetCharterQuoteAsync(quote.Id, cancellationToken))!;
        }

        public async Task<CharterQuoteDto> SubmitCharterQuoteForApprovalAsync(int id, CancellationToken cancellationToken = default)
        {
            var quote = await _db.CharterQuoteRequests
                .Include(q => q.CharterQuoteLegs)
                .FirstOrDefaultAsync(q => q.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("Quote not found.");

            if (quote.Status is not ((int)CharterQuoteStatus.Draft) and not ((int)CharterQuoteStatus.Rejected))
            {
                throw new InvalidOperationException("Only draft or rejected quotes can be submitted for approval.");
            }

            var dto = MapQuote(quote);
            var errors = CharterQuoteCalculator.Validate(dto);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join(" ", errors));
            }

            quote.Status = (int)CharterQuoteStatus.PendingApproval;
            quote.SubmittedAt = DateTime.UtcNow;
            quote.RejectionReason = null;
            quote.UpdatedAt = DateTime.UtcNow;
            AddAudit("CharterQuoteSubmitted", quote.Id, $"Quote {quote.QuoteRef} submitted for approval.");
            await _db.SaveChangesAsync(cancellationToken);

            return (await GetCharterQuoteAsync(quote.Id, cancellationToken))!;
        }

        public async Task<CharterQuoteDto> ApproveCharterQuoteAsync(
            int id,
            int approvedByUserId,
            CancellationToken cancellationToken = default)
        {
            var quote = await _db.CharterQuoteRequests
                .Include(q => q.CharterQuoteLegs)
                .FirstOrDefaultAsync(q => q.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("Quote not found.");

            if (quote.Status != (int)CharterQuoteStatus.PendingApproval)
            {
                throw new InvalidOperationException("Only quotes pending approval can be approved.");
            }

            var dto = MapQuote(quote);
            var errors = CharterQuoteCalculator.Validate(dto);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join(" ", errors));
            }

            quote.Status = (int)CharterQuoteStatus.Approved;
            quote.ApprovedByUserId = approvedByUserId;
            quote.ApprovedAt = DateTime.UtcNow;
            quote.RejectionReason = null;
            quote.UpdatedAt = DateTime.UtcNow;
            AddAudit("CharterQuoteApproved", quote.Id, $"Quote {quote.QuoteRef} approved.");
            await _db.SaveChangesAsync(cancellationToken);

            return (await GetCharterQuoteAsync(quote.Id, cancellationToken))!;
        }

        public async Task<CharterQuoteDto> RejectCharterQuoteAsync(
            int id,
            string reason,
            int reviewedByUserId,
            CancellationToken cancellationToken = default)
        {
            var quote = await _db.CharterQuoteRequests.FirstOrDefaultAsync(q => q.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("Quote not found.");

            if (quote.Status != (int)CharterQuoteStatus.PendingApproval)
            {
                throw new InvalidOperationException("Only quotes pending approval can be rejected.");
            }

            quote.Status = (int)CharterQuoteStatus.Rejected;
            quote.ApprovedByUserId = reviewedByUserId;
            quote.ApprovedAt = DateTime.UtcNow;
            quote.RejectionReason = string.IsNullOrWhiteSpace(reason) ? "No reason supplied." : reason.Trim();
            quote.UpdatedAt = DateTime.UtcNow;
            AddAudit("CharterQuoteRejected", quote.Id, $"Quote {quote.QuoteRef} rejected: {quote.RejectionReason}");
            await _db.SaveChangesAsync(cancellationToken);

            return (await GetCharterQuoteAsync(quote.Id, cancellationToken))!;
        }

        public async Task<CharterQuoteDto> CloneCharterQuoteAsync(
            int id,
            int requestedByUserId,
            CancellationToken cancellationToken = default)
        {
            var source = await _db.CharterQuoteRequests
                .AsNoTracking()
                .Include(q => q.CharterQuoteLegs)
                .FirstOrDefaultAsync(q => q.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("Quote not found.");

            var dto = MapQuote(source);
            dto.Id = 0;
            dto.QuoteRef = string.Empty;
            dto.Status = CharterQuoteStatus.Draft;
            dto.RequestedByUserId = requestedByUserId;
            dto.ApprovedByUserId = null;
            dto.ApprovedAt = null;
            dto.SubmittedAt = null;
            dto.RejectionReason = null;
            dto.ValidUntil = null;
            return await CreateCharterQuoteAsync(dto, requestedByUserId, cancellationToken);
        }

        private async Task MarkExpiredAsync(CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var expired = await _db.CharterQuoteRequests
                .Where(q => q.ValidUntil.HasValue
                    && q.ValidUntil.Value < today
                    && (q.Status == (int)CharterQuoteStatus.PendingApproval
                        || q.Status == (int)CharterQuoteStatus.Approved))
                .ToListAsync(cancellationToken);

            if (expired.Count == 0) return;

            foreach (var quote in expired)
            {
                quote.Status = (int)CharterQuoteStatus.Expired;
                quote.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<string> GenerateQuoteRefAsync(CancellationToken cancellationToken)
        {
            var year = DateTime.UtcNow.Year;
            var next = await _db.CharterQuoteRequests.CountAsync(cancellationToken) + 1;
            string reference;
            do
            {
                reference = $"QTE-{year}-{next:000}";
                next++;
            } while (await _db.CharterQuoteRequests.AnyAsync(q => q.QuoteRef == reference, cancellationToken));

            return reference;
        }

        private void AddAudit(string action, int entityId, string description)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                Action = action,
                EntityName = "CharterQuoteRequest",
                EntityId = entityId.ToString(),
                NewValues = description,
                OccurredAt = DateTime.UtcNow
            });
        }

        private static void NormalizeLegs(CharterQuoteDto dto)
        {
            dto.Legs ??= new List<CharterQuoteLegDto>();
            for (var i = 0; i < dto.Legs.Count; i++)
            {
                dto.Legs[i].LegNumber = i + 1;
            }
        }

        private static CharterQuoteLeg MapLeg(CharterQuoteLegDto leg, int number) => new()
        {
            LegNumber = number,
            OriginAddress = leg.OriginAddress.Trim(),
            OriginCityState = leg.OriginCityState.Trim(),
            DestinationAddress = leg.DestinationAddress.Trim(),
            DestinationCityState = leg.DestinationCityState.Trim(),
            DistanceKm = leg.DistanceKm ?? 0m,
            FuelOption = (int)leg.FuelOption,
            QuotedLegAmount = leg.QuotedLegAmount,
            Notes = string.IsNullOrWhiteSpace(leg.Notes) ? null : leg.Notes.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        private static CharterQuoteDto MapQuote(CharterQuoteRequest quote)
        {
            var legs = quote.CharterQuoteLegs
                .OrderBy(l => l.LegNumber)
                .Select(l => new CharterQuoteLegDto
                {
                    Id = l.Id,
                    LegNumber = l.LegNumber,
                    OriginAddress = l.OriginAddress,
                    OriginCityState = l.OriginCityState,
                    DestinationAddress = l.DestinationAddress,
                    DestinationCityState = l.DestinationCityState,
                    DistanceKm = l.DistanceKm,
                    FuelOption = (Manufacture.Models.Domain.FuelOption)l.FuelOption,
                    QuotedLegAmount = l.QuotedLegAmount,
                    Notes = l.Notes
                }).ToList();

            return new CharterQuoteDto
            {
                Id = quote.Id,
                QuoteRef = quote.QuoteRef,
                CustomerName = quote.CustomerName,
                CustomerPhone = quote.CustomerPhone,
                CustomerEmail = quote.CustomerEmail,
                RequestDate = quote.RequestDate,
                TripType = (CharterQuoteTripType)quote.TripType,
                VehicleTypeRequired = quote.VehicleTypeRequired,
                VehicleId = quote.VehicleId,
                VehicleRegNumber = quote.Vehicle?.RegistrationNumber ?? string.Empty,
                TotalDistanceKm = quote.TotalDistanceKm,
                QuotedAmount = quote.QuotedAmount,
                DriverAllowance = quote.DriverAllowance,
                SecurityEscortFee = quote.SecurityEscortFee,
                TollsAndRoadLevies = quote.TollsAndRoadLevies,
                TotalTripIncidentals = quote.TotalTripIncidentals ?? 0m,
                NetEstimate = quote.NetEstimate ?? 0m,
                Status = (CharterQuoteStatus)quote.Status,
                ValidUntil = quote.ValidUntil?.ToDateTime(TimeOnly.MinValue),
                Notes = quote.Notes,
                RequestedByUserId = quote.RequestedByUserId,
                RequestedByName = quote.RequestedByUser?.Name ?? string.Empty,
                SubmittedAt = quote.SubmittedAt,
                ApprovedByUserId = quote.ApprovedByUserId,
                ApprovedByName = quote.ApprovedByUser?.Name ?? string.Empty,
                ApprovedAt = quote.ApprovedAt,
                RejectionReason = quote.RejectionReason,
                CreatedAt = quote.CreatedAt,
                BookingId = quote.CharterBookings.FirstOrDefault()?.Id,
                BookingRef = quote.CharterBookings.FirstOrDefault()?.BookingRef ?? string.Empty,
                Legs = legs
            };
        }
    }
}
