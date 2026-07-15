using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Mappings;

public static class ConfigCurrencyMappings
{
    public static ConfigCurrencyDto ToDto(ConfigCurrency entity) => new()
    {
        Code = entity.Code,
        NumericCode = entity.NumericCode,
        Name = entity.Name,
        Symbol = entity.Symbol,
        DecimalPlaces = entity.DecimalPlaces,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt
    };
}
