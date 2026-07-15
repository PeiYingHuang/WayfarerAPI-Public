using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Service;

namespace WayfarerAPI.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class ExpenseController : ApiControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ExpenseController> _logger;

    public ExpenseController(
        IExpenseService expenseService,
        ILogger<ExpenseController> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateExpenseRequestDto request, [FromForm] List<IFormFile> receiptFiles)
    {
        try
        {
            var travellerId = GetCurrentUserId();
            _logger.LogInformation("開始建立花費 - TravelId: {TravelId}", request.TravelId);

            var receiptStreams = new List<Stream>();
            if (receiptFiles != null)
            {
                foreach (var file in receiptFiles)
                {
                    if (file != null && file.Length > 0)
                    {
                        receiptStreams.Add(file.OpenReadStream());
                    }
                }
            }

            var expenseId = await _expenseService.CreateWithReceiptsAsync(travellerId, request, receiptStreams);
            _logger.LogInformation("花費建立成功 - ExpenseId: {ExpenseId}", expenseId);

            return ApiResult(new { id = expenseId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立花費失敗 - TravelId: {TravelId}", request.TravelId);
            throw;
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetByTravelId([FromQuery] Guid travelId)
    {
        var expenses = await _expenseService.GetByTravelIdAsync(travelId);
        return ApiResult(expenses);
    }

    [HttpGet("member-expense")]
    public async Task<IActionResult> GetMemberExpenseByTravelIdAsync([FromQuery] Guid travelId)
    {
        var expenses = await _expenseService.GetMemberExpenseByTravelIdAsync(travelId);
        return ApiResult(expenses);
    }

    [HttpGet("{expenseId:guid}")]
    public async Task<IActionResult> GetByExpenseId(Guid expenseId)
    {
        var expense = await _expenseService.GetByExpenseIdAsync(expenseId);
        return ApiResult(expense);
    }

    [HttpGet("calculate-settlements")]
    public async Task<IActionResult> CalculateSettlements([FromQuery] Guid travelId)
    {
        return ApiResult(await _expenseService.CalculateSettlementsAsync(travelId));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExpenseRequestDto request)
    {
        await _expenseService.UpdateAsync(id, request);
        return ApiResult(true);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var travellerId = GetCurrentUserId();
        await _expenseService.DeleteAsync(travellerId, id);
        return ApiResult(true);
    }
}