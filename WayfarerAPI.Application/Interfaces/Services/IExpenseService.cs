using System.Threading.Tasks;
using WayfarerAPI.Application.DTOs;
using static WayfarerAPI.Application.Services.ExpenseService;

namespace WayfarerAPI.Application.Interfaces.Service
{
    public interface IExpenseService
    {
        Task<Guid> CreateAsync(Guid travellerId, CreateExpenseRequestDto request);
        Task<Guid> CreateWithReceiptsAsync(Guid travellerId, CreateExpenseRequestDto request, List<Stream>? receiptFileStreams);
        Task UpdateAsync(Guid expenseId, UpdateExpenseRequestDto request);
        Task<List<ExpenseSummaryDto>> GetByTravelIdAsync(Guid travelId);
        Task<List<MemberExpenseDto>> GetMemberExpenseByTravelIdAsync(Guid travelId);
        Task<ExpenseResponseDto?> GetByExpenseIdAsync(Guid expenseId);
        Task<List<SettlementTransactionResponseDto>> CalculateSettlementsAsync(Guid travelId);
        Task DeleteAsync(Guid travellerId, Guid expenseId);
    }
}