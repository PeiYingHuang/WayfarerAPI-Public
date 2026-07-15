using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Models;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Mappings;

public static class TravelMappings
{
    public static TravelFlight ToTravelFlight(UpsertTravelFlightDto dto, Guid travelId, DateTime createdAtUtc) => new()
    {
        Id = Guid.CreateVersion7(),
        TravelId = travelId,
        FlightNumber = string.IsNullOrWhiteSpace(dto.FlightNumber) ? null : dto.FlightNumber.Trim(),
        DepartureAirport = string.IsNullOrWhiteSpace(dto.DepartureAirport) ? null : dto.DepartureAirport.Trim().ToUpperInvariant(),
        ArrivalAirport = string.IsNullOrWhiteSpace(dto.ArrivalAirport) ? null : dto.ArrivalAirport.Trim().ToUpperInvariant(),
        DepartureAt = dto.DepartureAt,
        ArrivalAt = dto.ArrivalAt,
        Direction = dto.Direction,
        CreatedAt = createdAtUtc
    };

    public static TravelMember ToTravelMember(UpsertTravelMemberDto dto, Guid travelId, Guid createdBy, DateTime createdAtUtc) => new()
    {
        Id = Guid.CreateVersion7(),
        TravelId = travelId,
        Name = dto.Name.Trim(),
        TravellerId = dto.TravellerId,
        MemberType = dto.MemberType,
        Age = dto.Age,
        IsPayer = dto.IsPayer,
        CreatedBy = createdBy,
        CreatedAt = createdAtUtc
    };

    public static TravelResponseDto ToResponseDto(Travel travel, IEnumerable<TravelFlight> flights, IEnumerable<TravelMember> members) => new()
    {
        Id = travel.Id,
        Title = travel.Title,
        Destination = travel.Destination,
        StartDate = travel.StartDate?.ToString("yyyy-MM-dd"),
        EndDate = travel.EndDate?.ToString("yyyy-MM-dd"),
        ConsumptionCurrencyCode = travel.ConsumptionCurrencyCode,
        SettlementCurrencyCode = travel.SettlementCurrencyCode,
        CreatedBy = travel.CreatedBy,
        CreatedAt = travel.CreatedAt,
        Flights = flights.Select(f => new TravelFlightDto
        {
            Id = f.Id,
            FlightNumber = f.FlightNumber,
            DepartureAirport = f.DepartureAirport,
            ArrivalAirport = f.ArrivalAirport,
            DepartureAt = f.DepartureAt?.ToString("yyyy-MM-dd HH:mm"),
            ArrivalAt = f.ArrivalAt?.ToString("yyyy-MM-dd HH:mm"),
            Direction = f.Direction
        }).ToList(),
        Members = members.Select(m => new TravelMemberDto
        {
            Id = m.Id,
            Name = m.Name,
            TravellerId = m.TravellerId,
            MemberType = m.MemberType,
            Age = m.Age,
            IsPayer = m.IsPayer
        }).ToList()
    };

    public static TravellerResponseDto? ToTravellerResponseDto(TravellerInfoModel? travellerInfo) => travellerInfo == null ? null : new TravellerResponseDto
    {
        Id = travellerInfo.Id,
        Name = travellerInfo.Name,
        Email = travellerInfo.Email,
    };
}
