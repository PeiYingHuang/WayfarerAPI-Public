using Dapper;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Infrastructure.Repositories;

public sealed class TravelMemberRepository : ITravelMemberRepository
{
    private readonly IDbSession _session;

    public TravelMemberRepository(IDbSession session) => _session = session;

    public async Task<IEnumerable<TravelMember>> GetByTravelIdAsync(Guid travelId)
    {
        const string sql = """
            SELECT Id, TravelId, Name, TravellerId, MemberType, Age, IsPayer, CreatedBy, CreatedAt
            FROM TravelMember 
            WHERE TravelId = @TravelId 
            ORDER BY MemberType, CreatedAt;
            """;
        return await _session.Connection.QueryAsync<TravelMember>(
            new CommandDefinition(sql, new { TravelId = travelId }, transaction: _session.Transaction));
    }

    public async Task<IEnumerable<TravelMember>> GetByTravelIdsAsync(IEnumerable<Guid> travelIds)
    {
        const string sql = """
            SELECT Id, TravelId, Name, TravellerId, MemberType, Age, IsPayer, CreatedBy, CreatedAt
            FROM TravelMember 
            WHERE TravelId IN @TravelIds;
            """;
        return await _session.Connection.QueryAsync<TravelMember>(
            new CommandDefinition(sql, new { TravelIds = travelIds }, transaction: _session.Transaction));
    }

    public async Task<IEnumerable<TravelMember>> GetByTravellerIdAsync(Guid travellerId)
    {
        const string sql = """
            SELECT Id, TravelId, Name, TravellerId, MemberType, Age, IsPayer, CreatedBy, CreatedAt
            FROM TravelMember 
            WHERE TravellerId = @TravellerId;
            """;
        return await _session.Connection.QueryAsync<TravelMember>(
            new CommandDefinition(sql, new { TravellerId = travellerId }, transaction: _session.Transaction));
    }

    public async Task InsertAsync(TravelMember member)
    {
        const string sql = """
            INSERT INTO TravelMember (Id, TravelId, Name, TravellerId, MemberType, Age, IsPayer, CreatedBy, CreatedAt)
            VALUES (@Id, @TravelId, @Name, @TravellerId, @MemberType, @Age, @IsPayer, @CreatedBy, @CreatedAt);
            """;
        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            member.Id, member.TravelId, member.Name, member.TravellerId,
            member.MemberType, member.Age, IsPayer = member.IsPayer ? 1 : 0, member.CreatedBy, member.CreatedAt
        }, transaction: _session.Transaction));
    }

    public async Task UpdateAsync(TravelMember member)
    {
        const string sql = """
            UPDATE TravelMember
            SET Name = @Name,
                TravellerId = @TravellerId,
                MemberType = @MemberType,
                Age = @Age,
                IsPayer = @IsPayer
            WHERE Id = @Id AND TravelId = @TravelId;
            """;

        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            member.Id,
            member.TravelId,
            member.Name,
            member.TravellerId,
            member.MemberType,
            member.Age,
            IsPayer = member.IsPayer ? 1 : 0
        }, transaction: _session.Transaction));
    }

    public async Task DeleteByIdsAsync(Guid travelId, IEnumerable<Guid> memberIds)
    {
        var ids = memberIds?.Distinct().ToList() ?? [];
        if (ids.Count == 0)
            return;

        const string sql = "DELETE FROM TravelMember WHERE TravelId = @TravelId AND Id IN @Ids;";
        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new { TravelId = travelId, Ids = ids }, transaction: _session.Transaction));
    }

    public async Task DeleteByTravelIdAsync(Guid travelId)
    {
        const string sql = "DELETE FROM TravelMember WHERE TravelId = @TravelId;";
        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new { TravelId = travelId }, transaction: _session.Transaction));
    }
}
