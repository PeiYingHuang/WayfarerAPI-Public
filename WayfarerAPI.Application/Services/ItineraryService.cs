using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Application.Interfaces.Service;
using WayfarerAPI.Application.Interfaces.Utilities;
using WayfarerAPI.Application.Mappings;
using WayfarerAPI.Application.Models;
using WayfarerAPI.Domain.Entities;
using WayfarerAPI.Domain.Enumerations;

namespace WayfarerAPI.Application.Services;

public sealed class ItineraryService : IItineraryService
{
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IItineraryDetailRepository _itineraryDetailRepository;
    private readonly ITravelRepository _travelRepository;
    private readonly IOpenAiVisionClient _openAiVisionClient;
    private readonly ITravelFlightRepository _travelFlightRepository;
    private readonly ITravelMemberRepository _travelMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ItineraryService> _logger;

    public ItineraryService(
        IItineraryRepository itineraryRepository,
        IItineraryDetailRepository itineraryDetailRepository,
        ITravelRepository travelRepository,
        IOpenAiVisionClient openAiVisionClient,
        ITravelFlightRepository travelFlightRepository,
        ITravelMemberRepository travelMemberRepository,
        IUnitOfWork unitOfWork,
        ILogger<ItineraryService> logger)
    {
        _itineraryRepository = itineraryRepository;
        _itineraryDetailRepository = itineraryDetailRepository;
        _travelRepository = travelRepository;
        _openAiVisionClient = openAiVisionClient;
        _travelFlightRepository = travelFlightRepository;
        _travelMemberRepository = travelMemberRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    //public async Task<List<ItineraryDayDto>> GenerateItineraryAsyc(Guid travelId)
    //{
    //    var travel = _travelRepository.GetByIdAsync(travelId);
    //}

    public async Task<List<ItineraryDayDto>> GetByTravelIdAsync(Guid travelId, Guid travellerId)
    {
        //await VerifyTravelOwnershipAsync(travelId, travellerId);

        var days = await _itineraryRepository.GetByTravelIdAsync(travelId);
        var result = new List<ItineraryDayDto>(days.Count());

        foreach (var day in days.OrderBy(d => d.TravelDate))
        {
            var details = await _itineraryDetailRepository.GetByItineraryIdAsync(day.Id);
            result.Add(ItineraryMappings.ToDto(day, details));
        }

        return result;
    }

    public async Task<List<ItineraryDayDto>> UpsertDaysAsync(Guid travelId, Guid travellerId, UpsertItineraryBatchRequestDto request)
    {
        await VerifyTravelOwnershipAsync(travelId, travellerId);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var result = new List<ItineraryDayDto>(request.Days.Count);

            foreach (var dayReq in request.Days)
            {
                if (!DateTime.TryParseExact(dayReq.Date, "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var parsedDate))
                    throw new ArgumentException($"日期格式錯誤：{dayReq.Date}");

                var targetDate = parsedDate.Date;
                var existing = await _itineraryRepository.GetByTravelIdAndDateAsync(travelId, targetDate);

                Guid itineraryId;

                if (existing is null)
                {
                    itineraryId = Guid.CreateVersion7();
                    var itinerary = new Itinerary
                    {
                        Id = itineraryId,
                        TravelId = travelId,
                        DayNumber = dayReq.DayNumber,
                        TravelDate = targetDate,
                        DayTitle = string.IsNullOrWhiteSpace(dayReq.DayTitle) ? null : dayReq.DayTitle.Trim(),
                    };
                    await _itineraryRepository.InsertAsync(itinerary);
                }
                else
                {
                    itineraryId = existing.Id;
                    await _itineraryRepository.UpdateDayTitleAsync(
                        itineraryId,
                        string.IsNullOrWhiteSpace(dayReq.DayTitle) ? null : dayReq.DayTitle.Trim());
                }

                await _itineraryDetailRepository.DeleteByItineraryIdAsync(itineraryId);

                var newDetails = new List<ItineraryDetail>(dayReq.Details.Count);
                foreach (var dto in dayReq.Details)
                {
                    if (string.IsNullOrWhiteSpace(dto.Title)) continue;

                    var detail = ItineraryMappings.ToItineraryDetail(dto, itineraryId);
                    await _itineraryDetailRepository.InsertAsync(detail);
                    newDetails.Add(detail);
                }

                var resolvedTitle = string.IsNullOrWhiteSpace(dayReq.DayTitle) ? null : dayReq.DayTitle.Trim();
                var savedDay = existing is null
                    ? new Itinerary
                    {
                        Id = itineraryId,
                        TravelId = travelId,
                        DayNumber = dayReq.DayNumber,
                        TravelDate = targetDate,
                        DayTitle = resolvedTitle,
                    }
                    : new Itinerary
                    {
                        Id = existing.Id,
                        TravelId = existing.TravelId,
                        DayNumber = existing.DayNumber,
                        TravelDate = existing.TravelDate,
                        DayTitle = resolvedTitle,
                    };

                result.Add(ItineraryMappings.ToDto(savedDay, newDetails));
            }

            await _unitOfWork.CommitAsync();
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private async Task VerifyTravelOwnershipAsync(Guid travelId, Guid travellerId)
    {
        var travel = await _travelRepository.GetByIdAsync(travelId)
            ?? throw new ArgumentException("找不到此旅程");

        if (travel.CreatedBy != travellerId)
            throw new UnauthorizedAccessException("無權限存取此旅程");
    }

    public async Task<List<ItineraryDayDto>> GenerateItineraryByAI(Guid travelId, string userPreferences, CancellationToken ct)
    {
        var travel = await _travelRepository.GetByIdAsync(travelId) 
            ?? throw new ArgumentException("找不到此旅程");
        var travelMembers = await _travelMemberRepository.GetByTravelIdAsync(travelId);
        if(string.IsNullOrEmpty(travel.Destination) || travel.StartDate.HasValue == false || travel.EndDate.HasValue == false)
        {
            throw new ArgumentException("旅程的目的地或開始/結束日期未設定");
        }
        var travelFlight = await _travelFlightRepository.GetByTravelIdAsync(travelId);

        var OutboundFlight = travelFlight.Where(tf => tf.Direction == "Outbound").FirstOrDefault();
        var ReturnFlight = travelFlight.Where(tf => tf.Direction == "Return").FirstOrDefault();

        ItineraryAiModel itineraryAi = new ItineraryAiModel()
        {
            Destination = travel.Destination,
            StartDate = travel.StartDate.Value,
            EndDate = travel.EndDate.Value,
            AdultCount = travelMembers.Count(tm => tm.MemberType == "Adult"),
            Children = travelMembers
                .Where(tm => tm.MemberType == "Child")
                .GroupBy(tm => tm.Age)
                .Select(g => (Age: g.Key, Count: g.Count()))
                .ToList(),
            OutboundFlight = OutboundFlight == null ? null : new FlightInfoModel
            {
                FlightNumber = OutboundFlight.FlightNumber ?? "",
                DepartureAt = OutboundFlight.DepartureAt,
                ArrivalAt = OutboundFlight.ArrivalAt,
                DepartureAirport = OutboundFlight.DepartureAirport ?? "",
                ArrivalAirport = OutboundFlight.ArrivalAirport ?? ""
            },
            ReturnFlight = ReturnFlight == null ? null : new FlightInfoModel
            {
                FlightNumber = ReturnFlight.FlightNumber ?? "",
                DepartureAt = ReturnFlight.DepartureAt,
                ArrivalAt = ReturnFlight.ArrivalAt,
                DepartureAirport = ReturnFlight.DepartureAirport ?? "",
                ArrivalAirport = ReturnFlight.ArrivalAirport ?? ""
            },
            UserPreferences = userPreferences
        };
        //var draft = await _openAiVisionClient.GenerateItineraryAsync(itineraryAi, ct);
        //_logger.LogInformation("draft result: {draft}", JsonConvert.SerializeObject(draft));
        var draft = TestDraftData();

        List<ItineraryDayDto> result = new List<ItineraryDayDto>();
        int sort;
        foreach (var day in draft.Days)
        {
            sort = 1;
            var details = new List<ItineraryDetailDto>();
            foreach (var detail in day.Details)
            {
                details.Add(new ItineraryDetailDto
                {
                    Title = detail.Title,
                    Description = detail.Description,
                    LocationName = detail.LocationName,
                    StartTime = detail.StartTime,
                    EndTime = detail.EndTime,
                    Category = await _openAiVisionClient.ParseCategory(detail.Category),
                    SortOrder = sort++
                });
            }
            result.Add(new ItineraryDayDto
            {
                DayNumber = day.DayNumber,
                TravelDate = day.Date.ToString("yyyy-MM-dd"),
                DayTitle = day.DayTitle,
                Details = details
            });
        }
        _logger.LogInformation("AI行程生成完成，旅程ID: {TravelId}, 結果: {Result}", travelId, JsonConvert.SerializeObject(result));
        return result;
    }

    public AiItineraryDraftModel TestDraftData()
    {
        var model = new AiItineraryDraftModel
        {
            Days =
    [
        new AiItineraryDayDraftModel
        {
            DayNumber = 1,
            Date = new DateTime(2026,08,22),
            DayTitle = "抵達釜山與輕鬆探索",
            Details =
            [
                new AiItineraryDetailDraftModel
                {
                    Title = "班機抵達釜山金海國際機場",
                    Description = "從台北桃園機場出發，抵達釜山金海國際機場",
                    StartTime = "18:00",
                    EndTime = "18:30",
                    LocationName = "釜山金海國際機場",
                    Category = "airplane"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "機場至Avani Central Busan飯店交通",
                    Description = "搭乘計程車約1小時前往Avani Central Busan酒店",
                    StartTime = "18:30",
                    EndTime = "19:30",
                    LocationName = "機場到Avani Central Busan",
                    Category = "taxi"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "晚餐：釜山巨人炸雞品嘗",
                    Description = "享用著名的釜山巨人炸雞，適合全家大小",
                    StartTime = "20:00",
                    EndTime = "21:00",
                    LocationName = "Avani Central Busan附近餐廳",
                    Category = "food"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "飯店休息",
                    Description = "適合小朋友休息調整時差，準備接下來的行程",
                    StartTime = "21:00",
                    EndTime = "22:00",
                    LocationName = "Avani Central Busan",
                    Category = "accommodation"
                }
            ]
        },

        new AiItineraryDayDraftModel
        {
            DayNumber = 2,
            Date = new DateTime(2026,08,23),
            DayTitle = "釜山國際市場與海岸列車體驗",
            Details =
            [
                new AiItineraryDetailDraftModel
                {
                    Title = "早餐",
                    Description = "於飯店附近享用早餐，準備一天行程",
                    StartTime = "08:00",
                    EndTime = "09:00",
                    LocationName = null,
                    Category = "food"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "釜山國際市場購物",
                    Description = "前往釜山國際市場採購棉被及當地特色商品",
                    StartTime = "09:30",
                    EndTime = "11:30",
                    LocationName = "釜山國際市場",
                    Category = "shopping"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "午餐休息",
                    Description = "在市場附近餐廳享用午餐，休息調整",
                    StartTime = "11:30",
                    EndTime = "12:30",
                    LocationName = "釜山國際市場附近",
                    Category = "food"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "海岸列車體驗",
                    Description = "搭乘海岸列車欣賞釜山美麗海岸風光，輕鬆適合家庭出行",
                    StartTime = "13:00",
                    EndTime = "15:00",
                    LocationName = "海岸列車路線",
                    Category = "train"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "下午茶時間",
                    Description = "於咖啡廳休息並享用下午茶",
                    StartTime = "15:30",
                    EndTime = "16:30",
                    LocationName = "釜山市區咖啡廳",
                    Category = "coffee"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "晚餐",
                    Description = "前往人氣餐廳享用本地料理",
                    StartTime = "18:00",
                    EndTime = "19:30",
                    LocationName = "市區餐廳",
                    Category = "food"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "回飯店休息",
                    Description = null,
                    StartTime = "20:00",
                    EndTime = "21:00",
                    LocationName = "Avani Central Busan",
                    Category = "accommodation"
                }
            ]
        },

        new AiItineraryDayDraftModel
        {
            DayNumber = 3,
            Date = new DateTime(2026,08,24),
            DayTitle = "天空膠囊列車與釜山市區輕鬆遊",
            Details =
            [
                new AiItineraryDetailDraftModel
                {
                    Title = "早餐",
                    Description = "在飯店附近享用當地早餐",
                    StartTime = "08:00",
                    EndTime = "09:00",
                    LocationName = null,
                    Category = "food"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "天空膠囊列車搭乘",
                    Description = "體驗天空膠囊列車，享受俯瞰城市與海景樂趣",
                    StartTime = "09:30",
                    EndTime = "11:00",
                    LocationName = "天空膠囊列車站",
                    Category = "train"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "午餐",
                    Description = "在當地餐廳品嘗當地美食",
                    StartTime = "11:30",
                    EndTime = "12:30",
                    LocationName = "市區餐廳",
                    Category = "food"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "釜山市區輕鬆觀光",
                    Description = "安排交通工具輕鬆遊覽附近景點，避免過大體力消耗",
                    StartTime = "13:00",
                    EndTime = "16:00",
                    LocationName = "釜山市區",
                    Category = "taxi"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "下午茶",
                    Description = "於咖啡店享受下午茶時間",
                    StartTime = "16:00",
                    EndTime = "17:00",
                    LocationName = "市區咖啡廳",
                    Category = "coffee"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "晚餐",
                    Description = "嘗試釜山當地特色料理",
                    StartTime = "18:00",
                    EndTime = "19:30",
                    LocationName = "市區推薦餐廳",
                    Category = "food"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "回飯店休息",
                    Description = null,
                    StartTime = "20:00",
                    EndTime = "21:00",
                    LocationName = "Avani Central Busan",
                    Category = "accommodation"
                }
            ]
        },

        new AiItineraryDayDraftModel
        {
            DayNumber = 4,
            Date = new DateTime(2026,08,25),
            DayTitle = "飯店遷移與海雲台探索",
            Details =
            [
                new AiItineraryDetailDraftModel
                {
                    Title = "退房及前往海雲台",
                    Description = "早上辦理退房手續並搭乘計程車或地鐵前往紐CZ海雲台雷西登斯",
                    StartTime = "08:00",
                    EndTime = "09:00",
                    LocationName = "Avani Central Busan至紐CZ海雲台雷西登斯",
                    Category = "taxi"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "入住紐CZ海雲台雷西登斯",
                    Description = "辦理入住手續",
                    StartTime = "09:00",
                    EndTime = "09:30",
                    LocationName = "紐CZ海雲台雷西登斯",
                    Category = "accommodation"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "早餐或輕食",
                    Description = "附近咖啡廳進食早餐或輕食",
                    StartTime = "09:30",
                    EndTime = "10:00",
                    LocationName = "海雲台附近咖啡廳",
                    Category = "coffee"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "海雲台海灘輕鬆旅遊",
                    Description = "帶小孩到海雲台海灘遊玩及稍作休息",
                    StartTime = "10:30",
                    EndTime = "13:00",
                    LocationName = "海雲台海灘",
                    Category = "attraction"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "午餐",
                    Description = "享用海雲台海鮮或當地餐點",
                    StartTime = "13:00",
                    EndTime = "14:00",
                    LocationName = "海雲台區餐廳",
                    Category = "food"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "海雲台市場購物及休憩",
                    Description = "逛海雲台市場，購買手信及享受輕鬆時間",
                    StartTime = "14:30",
                    EndTime = "16:00",
                    LocationName = "海雲台市場",
                    Category = "shopping"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "晚餐",
                    Description = "海雲台附近餐廳享用晚餐",
                    StartTime = "18:00",
                    EndTime = "19:30",
                    LocationName = "海雲台區餐廳",
                    Category = "food"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "飯店休息",
                    Description = null,
                    StartTime = "20:00",
                    EndTime = "21:30",
                    LocationName = "紐CZ海雲台雷西登斯",
                    Category = "accommodation"
                }
            ]
        },

        new AiItineraryDayDraftModel
        {
            DayNumber = 5,
            Date = new DateTime(2026,08,26),
            DayTitle = "海雲台輕鬆行與返程",
            Details =
            [
                new AiItineraryDetailDraftModel
                {
                    Title = "早餐及輕鬆早晨散步",
                    Description = "飯店附近享用早餐後，海雲台周邊輕鬆散步",
                    StartTime = "08:00",
                    EndTime = "09:30",
                    LocationName = "海雲台區域",
                    Category = "food"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "輕鬆市區觀光",
                    Description = "搭乘地鐵或計程車輕鬆遊覽附近短途景點",
                    StartTime = "10:00",
                    EndTime = "12:00",
                    LocationName = "海雲台周邊",
                    Category = "taxi"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "午餐",
                    Description = "享用當地餐廳午餐",
                    StartTime = "12:00",
                    EndTime = "13:00",
                    LocationName = "海雲台區",
                    Category = "food"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "返回飯店整理行李",
                    Description = "回飯店準備退房",
                    StartTime = "13:30",
                    EndTime = "14:30",
                    LocationName = "紐CZ海雲台雷西登斯",
                    Category = "accommodation"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "前往釜山金海國際機場",
                    Description = "搭乘計程車抵達機場，預留足夠時間辦理登機及安檢",
                    StartTime = "15:00",
                    EndTime = "17:00",
                    LocationName = "紐CZ海雲台雷西登斯至釜山金海國際機場",
                    Category = "taxi"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "候機及返台班機",
                    Description = "搭乘班機返回台北",
                    StartTime = "19:10",
                    EndTime = "20:35",
                    LocationName = "釜山金海國際機場至台北桃園機場",
                    Category = "airplane"
                }
            ]
        }
    ]
        };
        return model;
    }
}
