using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.QueryServices;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Application.Interfaces.Service;
using WayfarerAPI.Application.Mappings;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Services;

public sealed class TravelService : ITravelService
{
    private readonly ITravelRepository _travelRepository;
    private readonly ITravelFlightRepository _travelFlightRepository;
    private readonly ITravelMemberRepository _travelMemberRepository;
    private readonly ITravelQueryService _travelQueryService;
    private readonly IConfigCurrencyRepository _configCurrencyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TravelService(
        ITravelRepository travelRepository,
        ITravelFlightRepository travelFlightRepository,
        ITravelMemberRepository travelMemberRepository,
        ITravelQueryService travelQueryService,
        IConfigCurrencyRepository configCurrencyRepository,
        IUnitOfWork unitOfWork)
    {
        _travelRepository = travelRepository;
        _travelFlightRepository = travelFlightRepository;
        _travelMemberRepository = travelMemberRepository;
        _travelQueryService = travelQueryService;
        _configCurrencyRepository = configCurrencyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TravelResponseDto> CreateAsync(Guid travellerId, UpsertTravelRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("旅程名稱不可為空");

        var consumptionCurrencyCode = await ResolveCurrencyCodeAsync(request.ConsumptionCurrencyCode);
        var settlementCurrencyCode = await ResolveCurrencyCodeAsync(request.SettlementCurrencyCode);

        var travel = new Travel
        {
            Id = Guid.CreateVersion7(),
            CreatedBy = travellerId,
            Title = request.Title.Trim(),
            Destination = request.Destination?.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ConsumptionCurrencyCode = consumptionCurrencyCode,
            SettlementCurrencyCode = settlementCurrencyCode,
            CreatedAt = DateTime.UtcNow
        };

        var flights = request.Flights.Select(f => TravelMappings.ToTravelFlight(f, travel.Id, DateTime.UtcNow)).ToList();
        var members = request.Members.Select(m => TravelMappings.ToTravelMember(m, travel.Id, travellerId, DateTime.UtcNow)).ToList();

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _travelRepository.InsertAsync(travel);
            foreach (var flight in flights)
                await _travelFlightRepository.InsertAsync(flight);
            foreach (var member in members)
                await _travelMemberRepository.InsertAsync(member);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }

        return TravelMappings.ToResponseDto(travel, flights, members);
    }

    public async Task<TravelResponseDto> UpdateAsync(Guid travelId, Guid travellerId, UpsertTravelRequestDto request)
    {
        var travel = await _travelRepository.GetByIdAsync(travelId)
            ?? throw new ArgumentException("找不到此旅程");

        if (travel.CreatedBy != travellerId)
            throw new UnauthorizedAccessException("無權限修改此旅程");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("旅程名稱不可為空");

        var consumptionCurrencyCode = await ResolveCurrencyCodeAsync(request.ConsumptionCurrencyCode);
        var settlementCurrencyCode = await ResolveCurrencyCodeAsync(request.SettlementCurrencyCode);

        travel.Title = request.Title.Trim();
        travel.Destination = request.Destination?.Trim();
        travel.StartDate = request.StartDate;
        travel.EndDate = request.EndDate;
        travel.ConsumptionCurrencyCode = consumptionCurrencyCode;
        travel.SettlementCurrencyCode = settlementCurrencyCode;

        var newFlights = request.Flights.Select(f => TravelMappings.ToTravelFlight(f, travelId, DateTime.UtcNow)).ToList();

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _travelRepository.UpdateAsync(travel);

            await _travelFlightRepository.DeleteByTravelIdAsync(travelId);
            foreach (var flight in newFlights)
                await _travelFlightRepository.InsertAsync(flight);

            var existingMembers = (await _travelMemberRepository.GetByTravelIdAsync(travelId)).ToList();
            var existingMap = existingMembers.ToDictionary(m => m.Id, m => m);

            var requestMemberIds = request.Members
                .Where(m => m.Id.HasValue && m.Id.Value != Guid.Empty)
                .Select(m => m.Id!.Value)
                .ToHashSet();

            var toDeleteIds = existingMembers
                .Where(m => !requestMemberIds.Contains(m.Id))
                .Select(m => m.Id)
                .ToList();

            await _travelMemberRepository.DeleteByIdsAsync(travelId, toDeleteIds);

            foreach (var memberDto in request.Members)
            {
                if (memberDto.Id.HasValue && memberDto.Id.Value != Guid.Empty && existingMap.TryGetValue(memberDto.Id.Value, out var existing))
                {
                    existing.Name = memberDto.Name.Trim();
                    existing.MemberType = memberDto.MemberType;
                    existing.Age = memberDto.Age;
                    existing.IsPayer = memberDto.IsPayer;
                    await _travelMemberRepository.UpdateAsync(existing);
                }
                else
                {
                    var newMember = TravelMappings.ToTravelMember(memberDto, travelId, travellerId, DateTime.UtcNow);
                    await _travelMemberRepository.InsertAsync(newMember);
                }
            }

            await _unitOfWork.CommitAsync();

            var updatedMembers = await _travelMemberRepository.GetByTravelIdAsync(travelId);
            return TravelMappings.ToResponseDto(travel, newFlights, updatedMembers);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<List<TravelResponseDto>> GetAllByTravellerAsync(Guid travellerId)
    {
        var travels = await _travelRepository.GetByTravellerIdAsync(travellerId);
        var result = new List<TravelResponseDto>(travels.Count());

        foreach (var travel in travels)
        {
            var flights = await _travelFlightRepository.GetByTravelIdAsync(travel.Id);
            var members = await _travelMemberRepository.GetByTravelIdAsync(travel.Id);
            result.Add(TravelMappings.ToResponseDto(travel, flights, members));
        }

        return result;
    }

    public async Task<List<TravelSummaryDto>> GetAllSummaryByTravellerAsync(Guid travellerId)
    {
        var summaries = await _travelQueryService.GetAllSummaryByTravellerIdAsync(travellerId);

        return summaries.Select(summary => new TravelSummaryDto
        {
            Id = summary.Id,
            Title = summary.Title ?? string.Empty,
            Destination = summary.Destination,
            StartDate = summary.StartDate?.ToString("yyyy-MM-dd"),
            EndDate = summary.EndDate?.ToString("yyyy-MM-dd"),
            CreatedAt = summary.CreatedAt,
            AdultCount = summary.AdultCount,
            ChildCount = summary.ChildCount,
            FlightCount = summary.FlightCount
        }).ToList();
    }

    public async Task<TravelResponseDto?> GetByIdAsync(Guid travelId, Guid travellerId)
    {
        var travel = await _travelRepository.GetByIdAsync(travelId);
        if (travel is null || travel.CreatedBy != travellerId) return null;

        var flights = await _travelFlightRepository.GetByTravelIdAsync(travelId);
        var members = await _travelMemberRepository.GetByTravelIdAsync(travelId);
        return TravelMappings.ToResponseDto(travel, flights, members);
    }

    public async Task DeleteAsync(Guid travelId, Guid travellerId)
    {
        var travel = await _travelRepository.GetByIdAsync(travelId)
            ?? throw new ArgumentException("找不到此旅程");

        if (travel.CreatedBy != travellerId)
            throw new UnauthorizedAccessException("無權限刪除此旅程");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _travelMemberRepository.DeleteByTravelIdAsync(travelId);
            await _travelFlightRepository.DeleteByTravelIdAsync(travelId);
            await _travelRepository.DeleteAsync(travelId);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private async Task<string> ResolveCurrencyCodeAsync(string? currencyCode)
    {
        var normalized = string.IsNullOrWhiteSpace(currencyCode)
            ? "TWD"
            : currencyCode.Trim().ToUpperInvariant();

        if (await _configCurrencyRepository.ExistsByCodeAsync(normalized))
            return normalized;

        if (await _configCurrencyRepository.ExistsByCodeAsync("TWD"))
            return "TWD";

        throw new InvalidOperationException("找不到對應的貨幣設定，請確認 ConfigCurrency 資料表已有 TWD 資料");
    }

    public async Task<IEnumerable<TravellerResponseDto>> GetTravellerInfoByIdAsync(Guid travellerId)
    {
        var travelIds = (await _travelMemberRepository.GetByTravellerIdAsync(travellerId)).Select(x => x.TravelId).Distinct();
        var friends = await _travelQueryService.GetFriendInfoByTravellerIdAsync(travellerId);
        return friends.Select(info => new TravellerResponseDto
        {
            Id = info.Id,
            Name = info.Name,
            Email = info.Email
        });
    }

    public async Task<IEnumerable<TravellerResponseDto>> GetFriendsByTravellerIdAsync(Guid travellerId)
    {
        var travelIds = (await _travelMemberRepository.GetByTravellerIdAsync(travellerId)).Select(x => x.TravelId).Distinct();
        var friends = await _travelQueryService.GetFriendInfoByTravellerIdAsync(travellerId);
        return friends.Select(info => new TravellerResponseDto
        {
            Id = info.Id,
            Name = info.Name,
            Email = info.Email
        });
    }
}