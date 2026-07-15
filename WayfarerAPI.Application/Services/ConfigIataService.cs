using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Application.Interfaces.Service;
using WayfarerAPI.Application.Interfaces.Utilities;

namespace WayfarerAPI.Application.Services;

public sealed class ConfigIataService : IConfigIataService
{
    private readonly IConfigIataRepository _configIataRepository;
    private readonly IIataGeoClient _iataGeoClient;
    private readonly IUnitOfWork _unitOfWork;

    public ConfigIataService(
        IConfigIataRepository configIataRepository,
        IIataGeoClient iataGeoClient,
        IUnitOfWork unitOfWork)
    {
        _configIataRepository = configIataRepository;
        _iataGeoClient = iataGeoClient;
        _unitOfWork = unitOfWork;
    }

    public async Task<ConfigIataDto?> GetAirportByIataCodeAsync(string iataCode)
    {
        if (string.IsNullOrWhiteSpace(iataCode) || iataCode.Length > 3)
            return null;

        var code = iataCode.Trim().ToUpperInvariant();

        var entity = await _configIataRepository.GetByIataCodeAsync(code);

        if (entity is null)
        {
            var remote = await _iataGeoClient.GetAirportAsync(code);
            if (remote is null)
                return null;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _configIataRepository.InsertAsync(remote);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            entity = remote;
        }

        return new ConfigIataDto
        {
            IataCode = entity.IataCode,
            IcaoCode = entity.IcaoCode,
            Name = entity.Name,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude
        };
    }
}
