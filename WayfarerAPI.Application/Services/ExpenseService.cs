using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Extensions;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.QueryServices;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Application.Interfaces.Service;
using WayfarerAPI.Application.Interfaces.Utilities;
using WayfarerAPI.Application.Models;

namespace WayfarerAPI.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly IExpenseDetailRepository _expenseDetailRepository;
    private readonly IExpenseDetailSplitRepository _expenseDetailSplitRepository;
    private readonly IExpenseSplitRepository _expenseSplitRepository;
    private readonly ITravelMemberRepository _travelMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGoogleCloudStorageClient _gcsClient;
    private readonly IExpenseQueryService _expenseQueryService;
    private readonly string _bucketName;
    private const string ReceiptFolderPath = "receipts";

    public ExpenseService(
        IExpenseRepository expenseRepository,
        IExpenseDetailRepository expenseDetailRepository,
        IExpenseDetailSplitRepository expenseDetailSplitRepository,
        IExpenseSplitRepository expenseSplitRepository,
        ITravelMemberRepository travelMemberRepository,
        IUnitOfWork unitOfWork,
        IGoogleCloudStorageClient gcsClient,
        IExpenseQueryService expenseQueryService)
    {
        _expenseRepository = expenseRepository;
        _expenseDetailRepository = expenseDetailRepository;
        _expenseDetailSplitRepository = expenseDetailSplitRepository;
        _expenseSplitRepository = expenseSplitRepository;
        _travelMemberRepository = travelMemberRepository;
        _unitOfWork = unitOfWork;
        _gcsClient = gcsClient;
        _bucketName = gcsClient.BucketName;
        _expenseQueryService = expenseQueryService;
    }

    public async Task<Guid> CreateAsync(Guid travellerId, CreateExpenseRequestDto request)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var expense = new Domain.Entities.Expense
            {
                Id = Guid.CreateVersion7(),
                TravelId = request.TravelId,
                ItineraryDetailId = request.ItineraryDetailId,
                PayerMemberId = request.PayerMemberId,
                ItemName = request.ItemName,
                ConsumptionAmount = request.ConsumptionAmount,
                CurrencyCode = request.CurrencyCode,
                ExchangeRate = request.ExchangeRate,
                SettlementAmount = request.SettlementAmount,
                Category = request.Category,
                Note = request.Note,
                ExpenseTime = request.ExpenseTime ?? DateTime.UtcNow,
                CreatedBy = travellerId
            };

            var expenseId = await _expenseRepository.InsertAsync(expense);
            await InsertExpenseDetailsAsync(expenseId, request.Details);
            await _expenseSplitRepository.InsertRangeAsync(expenseId, request.Splits);

            await _unitOfWork.CommitAsync();
            return expenseId;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<Guid> CreateWithReceiptAsync(Guid travellerId, CreateExpenseRequestDto request, Stream? receiptFileStream)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var expense = new Domain.Entities.Expense
            {
                Id = Guid.CreateVersion7(),
                TravelId = request.TravelId,
                ItineraryDetailId = request.ItineraryDetailId,
                PayerMemberId = request.PayerMemberId,
                ItemName = request.ItemName,
                ConsumptionAmount = request.ConsumptionAmount,
                CurrencyCode = request.CurrencyCode,
                ExchangeRate = request.ExchangeRate,
                SettlementAmount = request.SettlementAmount,
                Category = request.Category,
                Note = request.Note,
                ExpenseTime = request.ExpenseTime ?? DateTime.UtcNow,
                CreatedBy = travellerId
            };

            var expenseId = await _expenseRepository.InsertAsync(expense);
            await InsertExpenseDetailsAsync(expenseId, request.Details);
            await _expenseSplitRepository.InsertRangeAsync(expenseId, request.Splits);

            if (receiptFileStream != null && receiptFileStream.Length > 0)
            {
                await UploadReceiptAsync(request.TravelId, expenseId, receiptFileStream, 1);
            }

            await _unitOfWork.CommitAsync();
            return expenseId;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<Guid> CreateWithReceiptsAsync(Guid travellerId, CreateExpenseRequestDto request, List<Stream>? receiptFileStreams)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var expense = new Domain.Entities.Expense
            {
                Id = Guid.CreateVersion7(),
                TravelId = request.TravelId,
                ItineraryDetailId = request.ItineraryDetailId,
                PayerMemberId = request.PayerMemberId,
                ItemName = request.ItemName,
                ConsumptionAmount = request.ConsumptionAmount,
                CurrencyCode = request.CurrencyCode,
                ExchangeRate = request.ExchangeRate,
                SettlementAmount = request.SettlementAmount,
                Category = request.Category,
                Note = request.Note,
                ExpenseTime = request.ExpenseTime ?? DateTime.UtcNow,
                CreatedBy = travellerId
            };

            var expenseId = await _expenseRepository.InsertAsync(expense);
            await InsertExpenseDetailsAsync(expenseId, request.Details);
            await _expenseSplitRepository.InsertRangeAsync(expenseId, request.Splits);

            if (receiptFileStreams != null && receiptFileStreams.Count > 0)
            {
                int idx = 1;
                foreach (var stream in receiptFileStreams)
                {
                    if (stream != null && stream.Length > 0)
                    {
                        await UploadReceiptAsync(request.TravelId, expenseId, stream, idx);
                        idx++;
                    }
                }
            }

            await _unitOfWork.CommitAsync();
            return expenseId;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private async Task<string> UploadReceiptAsync(Guid travelId, Guid expenseId, Stream receiptFile, int index)
    {
        if (receiptFile == null || receiptFile.Length == 0)
        {
            throw new ArgumentException("收據檔案不能為空", nameof(receiptFile));
        }

        var objectName = $"{ReceiptFolderPath}/{travelId}/{expenseId}/{index:D2}.jpg";

        var receiptUrl = await _gcsClient.UploadFileAsync(
            _bucketName,
            objectName,
            receiptFile,
            "image/jpeg");

        return receiptUrl;
    }

    private async Task InsertExpenseDetailsAsync(Guid expenseId, List<ExpenseDetailRequestDto> details)
    {
        var detailIds = await _expenseDetailRepository.InsertRangeAsync(expenseId, details);

        for (int i = 0; i < details.Count; i++)
        {
            var detailSplits = details[i].DetailSplits;
            if (detailSplits is { Count: > 0 })
            {
                await _expenseDetailSplitRepository.InsertRangeAsync(detailIds[i], detailSplits);
            }
        }
    }

    public async Task UpdateAsync(Guid expenseId, UpdateExpenseRequestDto request)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _expenseRepository.UpdateAsync(expenseId, request.PayerMemberId, request.ItemName, request.ConsumptionAmount, request.CurrencyCode, 
                request.ExchangeRate, request.SettlementAmount, request.Category, request.Note, request.ExpenseTime);

            var existingDetails = await _expenseDetailRepository.GetByExpenseIdAsync(expenseId);
            foreach (var detail in existingDetails)
            {
                await _expenseDetailSplitRepository.DeleteByExpenseDetailIdAsync(detail.Id);
            }

            await _expenseDetailRepository.DeleteByExpenseIdAsync(expenseId);
            await InsertExpenseDetailsAsync(expenseId, request.Details);

            await _expenseSplitRepository.DeleteByExpenseIdAsync(expenseId);
            await _expenseSplitRepository.InsertRangeAsync(expenseId, request.Splits);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<List<ExpenseSummaryDto>> GetByTravelIdAsync(Guid travelId)
    {
        var expenses = await _expenseRepository.GetByTravelIdAsync(travelId);
        var members = await _travelMemberRepository.GetByTravelIdAsync(travelId);
        var splits = await _expenseSplitRepository.GetByTravelIdAsync(travelId);

        return expenses.Select(e => new ExpenseSummaryDto
        {
            Id = e.Id,
            TravelId = e.TravelId,
            PayerMemberName = members.FirstOrDefault(x=>x.Id == e.PayerMemberId)?.Name ?? "",
            ItemName = e.ItemName,
            ConsumptionAmount = e.ConsumptionAmount,
            CurrencyCode = e.CurrencyCode,
            SettlementAmount = e.SettlementAmount,
            Category = e.Category,
            ExpenseTime = e.ExpenseTime,
            SplitMemberNumber = splits.Where(x=>x.ExpenseId == e.Id && x.SplitAmount > 0).Count()
        }).ToList();
    }

    public async Task<List<MemberExpenseDto>> GetMemberExpenseByTravelIdAsync(Guid travelId)
    {
        var expenses = await _expenseRepository.GetByTravelIdAsync(travelId);
        var splits = await _expenseSplitRepository.GetByTravelIdAsync(travelId);

        List<MemberExpenseDto> memberExpenses = new List<MemberExpenseDto>();
        foreach (var i in splits)
        {
            var expense = expenses.FirstOrDefault(x => x.Id == i.ExpenseId);
            memberExpenses.Add(new MemberExpenseDto()
            {
                MemberId = i.MemberId,
                ExpenseId = i.ExpenseId,
                ItemName = expense?.ItemName ?? "",
                Amount = i.SplitAmount,
                ExpenseTime = expense?.ExpenseTime,
                Category = expense?.Category ?? "",
            });
        }
        return memberExpenses.OrderBy(x=>x.ExpenseTime).ToList();
    }

    public async Task<ExpenseResponseDto?> GetByExpenseIdAsync(Guid expenseId)
    {
        var expense = await _expenseRepository.GetByIdAsync(expenseId);
        if (expense == null)
        {
            return null;
        }

        var members = await _travelMemberRepository.GetByTravelIdAsync(expense.TravelId);
        var payerName = members.FirstOrDefault(m => m.Id == expense.PayerMemberId)?.Name ?? string.Empty;
        var details = await _expenseDetailRepository.GetByExpenseIdAsync(expenseId);
        var detailSplitsMap = new Dictionary<int, List<ExpenseDetailSplitDto>>();
        foreach (var detail in details)
        {
            var detailSplits = await _expenseDetailSplitRepository.GetByExpenseDetailIdAsync(detail.Id);
            detailSplitsMap[detail.Id] = detailSplits.Select(s => new ExpenseDetailSplitDto
            {
                Id = s.Id,
                ExpenseDetailId = s.ExpenseDetailId,
                MemberId = s.MemberId,
                SplitAmount = s.SplitAmount
            }).ToList();
        }
        var splits = await _expenseSplitRepository.GetByExpenseIdAsync(expenseId);
        var receiptPrefix = $"{ReceiptFolderPath}/{expense.TravelId}/{expenseId}/";
        var receiptObjectNames = await _gcsClient.ListObjectNamesAsync(_bucketName, receiptPrefix);
        var receiptUrls = new List<string>();

        foreach (var objectName in receiptObjectNames)
        {
            var signedUrl = await _gcsClient.GenerateSignedReadUrlAsync(_bucketName, objectName);
            receiptUrls.Add(signedUrl);
        }

        return new ExpenseResponseDto
        {
            Id = expense.Id,
            TravelId = expense.TravelId,
            PayerMemberId = expense.PayerMemberId,
            PayerName = payerName,
            ItemName = expense.ItemName,
            ConsumptionAmount = expense.ConsumptionAmount,
            CurrencyCode = expense.CurrencyCode,
            ExchangeRate = expense.ExchangeRate,
            SettlementAmount = expense.SettlementAmount,
            Category = expense.Category ?? "",
            Note = expense.Note ?? "",
            ExpenseTime = expense.ExpenseTime,
            ReceiptUrls = receiptUrls,
            Details = details.Select(d => new ExpenseDetailResponseDto
            {
                Id = d.Id,
                ExpenseId = d.ExpenseId,
                Item = d.Item,
                UnitPrice = d.UnitPrice,
                Quantity = d.Quantity,
                Amount = d.Amount,
                Description = d.Description,
                DetailSplits = detailSplitsMap.TryGetValue(d.Id, out var detailSplits) && detailSplits.Count > 0
                    ? detailSplits
                    : null
            }).ToList(),
            Splits = splits.Select(s => new ExpenseSplitDto
            {
                Id = s.Id,
                ExpenseId = s.ExpenseId,
                MemberId = s.MemberId,
                SplitAmount = s.SplitAmount
            }).OrderBy(x=>x.MemberId).ToList()
        };
    }

    public async Task<List<SettlementTransactionResponseDto>> CalculateSettlementsAsync(Guid travelId)
    {
        var records = await _expenseSplitRepository.GetByTravelIdAsync(travelId);
        // 1. 計算每個人的淨餘額 (Net Balances)
        var balances = new Dictionary<Guid, decimal>();

        foreach (var record in records)
        {
            // 確保字典裡有這兩個人
            if (!balances.ContainsKey(record.MemberId)) balances[record.MemberId] = 0;
            if (!balances.ContainsKey(record.PayerMemberId)) balances[record.PayerMemberId] = 0;

            // 核心邏輯：分攤者(MemberId)欠錢扣餘額，代墊者(PayerMemberId)收錢加餘額
            // 如果自己墊錢自己分攤 (MemberId == PayerMemberId)，一加一減會變 +0，自然抵銷
            balances[record.MemberId] -= record.SplitAmount;
            balances[record.PayerMemberId] += record.SplitAmount;
        }

        // 2. 債務簡化 (分離出欠款人與收款人)
        var debtors = balances.Where(b => b.Value < 0)
                              .Select(b => new { MemberId = b.Key, Debt = -b.Value }) // 轉正數方便計算
                              .OrderByDescending(b => b.Debt)
                              .ToList();

        var creditors = balances.Where(b => b.Value > 0)
                                .Select(b => new { MemberId = b.Key, Credit = b.Value })
                                .OrderByDescending(b => b.Credit)
                                .ToList();

        var transactions = new List<SettlementTransactionResponseDto>();
        int i = 0, j = 0;

        var members = await _travelMemberRepository.GetByTravelIdAsync(travelId);
        // 3. 貪婪演算法進行債務抵銷
        while (i < debtors.Count && j < creditors.Count)
        {
            var debtor = debtors[i];
            var creditor = creditors[j];

            // 取兩人之間最小的金額進行結算
            decimal settleAmount = Math.Min(debtor.Debt, creditor.Credit);

            transactions.Add(new SettlementTransactionResponseDto
            {
                FromMemberId = debtor.MemberId,
                FromMemberName = members.FirstOrDefault(m => m.Id == debtor.MemberId)?.Name ?? "",
                ToMemberId = creditor.MemberId,
                ToMemberName = members.FirstOrDefault(m => m.Id == creditor.MemberId)?.Name ?? "",
                Amount = settleAmount
            });

            // 更新剩餘債務/債權
            debtors[i] = new { MemberId = debtor.MemberId, Debt = debtor.Debt - settleAmount };
            creditors[j] = new { MemberId = creditor.MemberId, Credit = creditor.Credit - settleAmount };

            // 欠款人還清了，換下一個欠款人
            if (debtors[i].Debt == 0) i++;

            // 收款人收齊了，換下一個收款人
            if (creditors[j].Credit == 0) j++;
        }

        return transactions;
    }

    public async Task DeleteAsync(Guid travellerId, Guid expenseId)
    {
        var expense = await _expenseRepository.GetByIdAsync(expenseId);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _expenseDetailRepository.DeleteByExpenseIdAsync(expenseId);
            await _expenseSplitRepository.DeleteByExpenseIdAsync(expenseId);
            await _expenseRepository.DeleteAsync(travellerId, expenseId);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }

        if (expense != null)
        {
            var receiptPrefix = $"{ReceiptFolderPath}/{expense.TravelId}/{expenseId}/";
            var objectNames = await _gcsClient.ListObjectNamesAsync(_bucketName, receiptPrefix);
            foreach (var name in objectNames)
            {
                try { await _gcsClient.DeleteFileAsync(_bucketName, name); } catch { /* 刪除失敗不影響主流程 */ }
            }
        }
    }

    public async Task<byte[]> ExportExpenseInfoToExcel(Guid memberId)
    {
        IEnumerable<ExpenseInfoModel> expenses  = await _expenseQueryService.GetExpenseInfoByMemberIdAsync(memberId);

        using var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet("花費細項");

        // ---------- 樣式 ----------
        var headerStyle = workbook.CreateCellStyle();
        var headerFont = workbook.CreateFont();
        headerFont.IsBold = true;
        headerStyle.SetFont(headerFont);
        headerStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index;
        headerStyle.FillPattern = FillPattern.SolidForeground;

        var dateStyle = workbook.CreateCellStyle();
        var dataFormat = workbook.CreateDataFormat();
        dateStyle.DataFormat = dataFormat.GetFormat("yyyy-mm-dd hh:mm");

        var moneyStyle = workbook.CreateCellStyle();
        moneyStyle.DataFormat = dataFormat.GetFormat("#,##0");

        // ---------- 標題列 ----------
        var headers = new[]
        {
            "消費時間", "類別", "項目名稱", "消費金額"
        };

        var headerRow = sheet.CreateRow(0);
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = headerRow.CreateCell(i);
            cell.SetCellValue(headers[i]);
            cell.CellStyle = headerStyle;
        }

        // ---------- 資料列 ----------
        int rowIndex = 1;
        foreach (var e in expenses)
        {
            var row = sheet.CreateRow(rowIndex);

            var timeCell = row.CreateCell(0);
            timeCell.SetCellValue(e.ExpenseTime);
            timeCell.CellStyle = dateStyle;

            row.CreateCell(1).SetCellValue(e.Category.GetDescription());
            row.CreateCell(2).SetCellValue(e.ItemName);

            var amountCell = row.CreateCell(3);
            amountCell.SetCellValue((double)e.SplitAmount);
            amountCell.CellStyle = moneyStyle;
            rowIndex++;
        }
        //總計
        var totalRow = sheet.CreateRow(rowIndex);

        totalRow.CreateCell(0).SetCellValue("總計");
        totalRow.CreateCell(1).SetCellValue("");

        var totalAmountCell = totalRow.CreateCell(3);
        totalAmountCell.SetCellValue((double)expenses.Sum(x=>x.SplitAmount));
        totalAmountCell.CellStyle = moneyStyle;

        // ---------- 欄寬自動調整 ----------
        var columnWidths = new[] { 16, 10, 60, 10 }; // 依實際欄位調整
        for (int i = 0; i < columnWidths.Length; i++)
        {
            sheet.SetColumnWidth(i, columnWidths[i] * 256);
        }

        // 凍結首列
        sheet.CreateFreezePane(0, 1);

        using var stream = new MemoryStream();
        workbook.Write(stream, leaveOpen: true);
        return stream.ToArray();
    }
}
