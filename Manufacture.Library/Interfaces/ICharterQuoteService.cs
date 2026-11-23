using Manufacture.Models.DTOs;

namespace Manufacture.Library.Interfaces
{
    public interface ICharterQuoteService
    {
        Task<List<CharterQuoteDto>> GetCharterQuotesAsync(
            string? status = null,
            string? search = null,
            CancellationToken cancellationToken = default);

        Task<CharterQuoteDto?> GetCharterQuoteAsync(int id, CancellationToken cancellationToken = default);

        Task<List<CharterQuoteDto>> GetApprovedCharterQuotesAsync(CancellationToken cancellationToken = default);

        Task<CharterQuoteDto> CreateCharterQuoteAsync(
            CharterQuoteDto dto,
            int requestedByUserId,
            CancellationToken cancellationToken = default);

        Task<CharterQuoteDto> UpdateCharterQuoteAsync(
            CharterQuoteDto dto,
            int requestedByUserId,
            CancellationToken cancellationToken = default);

        Task<CharterQuoteDto> SubmitCharterQuoteForApprovalAsync(int id, CancellationToken cancellationToken = default);

        Task<CharterQuoteDto> ApproveCharterQuoteAsync(
            int id,
            int approvedByUserId,
            CancellationToken cancellationToken = default);

        Task<CharterQuoteDto> RejectCharterQuoteAsync(
            int id,
            string reason,
            int reviewedByUserId,
            CancellationToken cancellationToken = default);

        Task<CharterQuoteDto> CloneCharterQuoteAsync(
            int id,
            int requestedByUserId,
            CancellationToken cancellationToken = default);
    }
}
