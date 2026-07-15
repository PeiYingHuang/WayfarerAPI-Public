using WayfarerAPI.Application.DTOs;

namespace WayfarerAPI.Application.Interfaces.Service;

public interface IConfigCurrencyService
{
    Task<List<ConfigCurrencyDto>> GetAllAsync();
    Task<List<ConfigCurrencyDto>> SyncAsync();
}