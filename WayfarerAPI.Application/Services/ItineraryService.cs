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
        var draft = new AiItineraryDraftModel
        {
            Days =
    [
        new AiItineraryDayDraftModel
        {
            DayNumber = 1,
            Date = new DateTime(2026, 8, 22),
            DayTitle = "抵達釜山與初探市區",
            Details =
            [
                new AiItineraryDetailDraftModel
                {
                    Title = "抵達釜山金海國際機場",
                    Description = "搭乘下午班機從台北出發，約18:00抵達釜山，辦理入境手續。",
                    StartTime = "18:00",
                    EndTime = "18:30",
                    LocationName = "釜山金海國際機場",
                    Category = "airplane"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "機場至飯店交通",
                    Description = "搭乘計程車約1小時抵達Avani Central Busan，方便攜帶行李和小孩。",
                    StartTime = "18:30",
                    EndTime = "19:30",
                    LocationName = "Avani Central Busan",
                    Category = "taxi"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "晚餐：釜山巨人炸雞",
                    Description = "人氣炸雞連鎖，酥脆口感適合全家共享，距飯店不遠，方便第一天簡單用餐。",
                    StartTime = "20:00",
                    EndTime = "21:00",
                    LocationName = "釜山巨人炸雞 (附近分店)",
                    Category = "food"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "飯店休息與自由活動",
                    Description = "讓小孩和大人調整時差，休息為隔日旅程備戰。",
                    StartTime = "21:00",
                    EndTime = null,
                    LocationName = "Avani Central Busan",
                    Category = "accommodation"
                }
            ]
        },
        new AiItineraryDayDraftModel
        {
            DayNumber = 2,
            Date = new DateTime(2026, 8, 23),
            DayTitle = "釜山市區文化巡禮與國際市場購物",
            Details =
            [
                new AiItineraryDetailDraftModel
                {
                    Title = "早餐及出發",
                    Description = "飯店附近咖啡店輕食早餐，準備當天行程。",
                    StartTime = "08:00",
                    EndTime = "08:45",
                    LocationName = "Avani Central Busan 附近咖啡廳",
                    Category = "coffee"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "釜山國際市場購物",
                    Description = "著名的傳統市場，選購棉被及其他韓國特色商品，適合全家輕鬆逛街。",
                    StartTime = "09:30",
                    EndTime = "11:30",
                    LocationName = "釜山國際市場",
                    Category = "shopping"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "午餐：市場附近韓式料理",
                    Description = "品嚐市場周邊經典海鮮煎餅或韓式小吃，口味道地且方便用餐。",
                    StartTime = "11:30",
                    EndTime = "12:30",
                    LocationName = "釜山市場附近韓式餐廳",
                    Category = "food"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "甘川文化村輕鬆散步",
                    Description = "欣賞彩色街道與藝術裝置，調整步行節奏適合帶小孩，並有休憩空間。",
                    StartTime = "13:00",
                    EndTime = "15:00",
                    LocationName = "甘川文化村",
                    Category = "attraction"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "咖啡休息：甘川文化村附近咖啡廳",
                    Description = "享用甜點與飲品，稍作休息，適合親子時光。",
                    StartTime = "15:00",
                    EndTime = "15:45",
                    LocationName = "甘川文化村咖啡廳",
                    Category = "coffee"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "晚餐：釜山當地燒烤餐廳",
                    Description = "體驗韓式燒肉美食，方便搭乘交通回飯店。",
                    StartTime = "18:00",
                    EndTime = "19:30",
                    LocationName = "市區著名韓式燒烤店",
                    Category = "food"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "返回飯店休息",
                    Description = "準備隔日行程，飯店休息。",
                    StartTime = "20:00",
                    EndTime = null,
                    LocationName = "Avani Central Busan",
                    Category = "accommodation"
                }
            ]
        },
        new AiItineraryDayDraftModel
        {
            DayNumber = 3,
            Date = new DateTime(2026, 8, 24),
            DayTitle = "海岸列車與天空膠囊列車體驗",
            Details =
            [
                new AiItineraryDetailDraftModel
                {
                    Title = "早餐",
                    Description = "飯店附近簡單早餐，為一日行程補充能量。",
                    StartTime = "07:30",
                    EndTime = "08:00",
                    LocationName = "Avani Central Busan附近咖啡廳",
                    Category = "coffee"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "入住新飯店：紐CZ海雲台雷西登斯",
                    Description = "上午辦理退房並前往新飯店，放置行李。",
                    StartTime = "08:30",
                    EndTime = "09:30",
                    LocationName = "紐CZ海雲台雷西登斯",
                    Category = "accommodation"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "乘坐海岸列車（海雲台-松亭線）",
                    Description = "欣賞美麗海景，車程輕鬆適合攜帶小孩的家庭旅遊。",
                    StartTime = "10:00",
                    EndTime = "11:30",
                    LocationName = "海岸列車路線",
                    Category = "train"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "午餐：靠海海鮮餐廳",
                    Description = "海雲台附近新鮮海鮮料理，環境舒適適合親子用餐。",
                    StartTime = "12:00",
                    EndTime = "13:30",
                    LocationName = "海雲台海鮮餐廳",
                    Category = "food"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "乘坐天空膠囊列車",
                    Description = "體驗釜山天空膠囊列車，欣賞市區與海景，適合全家大小體驗樂趣。",
                    StartTime = "14:00",
                    EndTime = "15:30",
                    LocationName = "釜山天空膠囊列車站",
                    Category = "train"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "下午茶：海雲台沿海咖啡廳",
                    Description = "輕鬆享用甜點與咖啡，散步海邊親子好選擇。",
                    StartTime = "15:30",
                    EndTime = "16:30",
                    LocationName = "海雲台咖啡廳",
                    Category = "coffee"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "晚餐：海雲台當地餐廳",
                    Description = "簡約韓式餐點，方便飯店交通，適合全家享用。",
                    StartTime = "18:30",
                    EndTime = "20:00",
                    LocationName = "海雲台區韓式餐廳",
                    Category = "food"
                }
            ]
        },
        new AiItineraryDayDraftModel
        {
            DayNumber = 4,
            Date = new DateTime(2026, 8, 25),
            DayTitle = "海雲台周邊親子輕鬆遊",
            Details =
            [
                new AiItineraryDetailDraftModel
                {
                    Title = "早餐",
                    Description = "飯店內或附近咖啡廳輕食早餐。",
                    StartTime = "08:00",
                    EndTime = "08:45",
                    LocationName = "紐CZ海雲台雷西登斯附近咖啡廳",
                    Category = "coffee"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "海雲台海水浴場",
                    Description = "親子海灘玩水，風景優美且安全設施完善，適合學齡前小孩。",
                    StartTime = "09:00",
                    EndTime = "11:30",
                    LocationName = "海雲台海水浴場",
                    Category = "attraction"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "午餐：海雲台街邊小吃",
                    Description = "品嘗韓式煎餅、炸雞等，方便親子享用。",
                    StartTime = "12:00",
                    EndTime = "13:00",
                    LocationName = "海雲台街頭小吃攤",
                    Category = "food"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "釜山水族館",
                    Description = "適合帶小孩參觀，體驗水底隧道和多樣海洋生物展示。",
                    StartTime = "13:30",
                    EndTime = "15:30",
                    LocationName = "釜山水族館",
                    Category = "attraction"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "咖啡與休息",
                    Description = "水族館附近咖啡廳休息，提供舒適空間和兒童友善環境。",
                    StartTime = "15:30",
                    EndTime = "16:15",
                    LocationName = "海雲台水族館附近咖啡廳",
                    Category = "coffee"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "晚餐：韓國傳統韓定食",
                    Description = "餐廳提供多樣精緻料理，適合一家大小一起品嘗。",
                    StartTime = "18:00",
                    EndTime = "19:30",
                    LocationName = "海雲台韓定食餐廳",
                    Category = "food"
                }
            ]
        },
        new AiItineraryDayDraftModel
        {
            DayNumber = 5,
            Date = new DateTime(2026, 8, 26),
            DayTitle = "出發前輕鬆半日遊與回程",
            Details =
            [
                new AiItineraryDetailDraftModel
                {
                    Title = "早餐",
                    Description = "飯店附近輕食，預留時間準備退房及前往機場。",
                    StartTime = "07:30",
                    EndTime = "08:00",
                    LocationName = "紐CZ海雲台雷西登斯附近咖啡廳",
                    Category = "coffee"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "松島天空公園短程散步",
                    Description = "海雲台附近公園，風景優美且輕鬆，適合小孩活動放鬆。",
                    StartTime = "08:30",
                    EndTime = "10:00",
                    LocationName = "松島天空公園",
                    Category = "attraction"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "午餐：海雲台區輕食餐廳",
                    Description = "提供簡單韓式料理或西餐，方便快速用餐。",
                    StartTime = "10:30",
                    EndTime = "11:30",
                    LocationName = "海雲台輕食餐廳",
                    Category = "food"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "返回飯店準備退房",
                    Description = "返回飯店整理行李，預備前往機場。",
                    StartTime = "11:30",
                    EndTime = "12:30",
                    LocationName = "紐CZ海雲台雷西登斯",
                    Category = "accommodation"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "飯店至機場交通",
                    Description = "搭計程車約1小時前往釜山金海國際機場，預留充裕時間。",
                    StartTime = "12:30",
                    EndTime = "13:30",
                    LocationName = "釜山金海國際機場",
                    Category = "taxi"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "機場休息與出境準備",
                    Description = "預留不少於3小時的時間進行退稅及安檢手續。",
                    StartTime = "13:30",
                    EndTime = "16:10",
                    LocationName = "釜山金海國際機場",
                    Category = "other"
                },
                new AiItineraryDetailDraftModel
                {
                    Title = "回程班機起飛",
                    Description = "搭乘晚間19:10班機返回台北。",
                    StartTime = "19:10",
                    EndTime = null,
                    LocationName = "釜山金海國際機場",
                    Category = "airplane"
                }
            ]
        }
    ]
        };
        return draft;
    }
}
