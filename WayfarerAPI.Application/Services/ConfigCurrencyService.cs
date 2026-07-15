using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Application.Interfaces.Service;
using WayfarerAPI.Application.Interfaces.Utilities;
using WayfarerAPI.Application.Mappings;

namespace WayfarerAPI.Application.Services;

public sealed class ConfigCurrencyService : IConfigCurrencyService
{
    private readonly IConfigCurrencyRepository _configCurrencyRepository;
    private readonly IFrankfurterClient _frankfurterClient;
    private readonly IUnitOfWork _unitOfWork;

    public ConfigCurrencyService(
        IConfigCurrencyRepository configCurrencyRepository,
        IFrankfurterClient frankfurterClient,
        IUnitOfWork unitOfWork)
    {
        _configCurrencyRepository = configCurrencyRepository;
        _frankfurterClient = frankfurterClient;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ConfigCurrencyDto>> GetAllAsync()
    {
        var entities = await _configCurrencyRepository.GetAllAsync();
        return entities.Select(ConfigCurrencyMappings.ToDto).ToList();
    }

    public async Task<List<ConfigCurrencyDto>> SyncAsync()
    {
        var currencies = await _frankfurterClient.GetCurrenciesAsync();
        if (currencies.Count == 0)
            return [];

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _configCurrencyRepository.UpsertAsync(currencies);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }

        var entities = await _configCurrencyRepository.GetAllAsync();
        return entities.Select(ConfigCurrencyMappings.ToDto).ToList();
    }
}