using Dapper;
using Tezgah.Data;
using Tezgah.Models;

namespace Tezgah.Repositories;

public class SprintRepository
{
    private readonly DapperContext _context;

    public SprintRepository(DapperContext context) => _context = context;

    private const string BaseSelect = @"
        SELECT s.*,
               p.Name  AS ProjectName,
               p.[Key] AS ProjectKey,
               u.FullName AS CreatedByName,
               (SELECT COUNT(*) FROM OrkaJira_Tasks t WHERE t.SprintId = s.Id) AS TaskCount,
               (SELECT COUNT(*) FROM OrkaJira_Tasks t WHERE t.SprintId = s.Id AND t.Status = 3) AS DoneTaskCount
        FROM OrkaJira_Sprints s
        LEFT JOIN OrkaJira_Projects p ON s.ProjectId = p.Id
        LEFT JOIN OrkaJira_Users    u ON s.CreatedById = u.Id";

    public async Task<IEnumerable<Sprint>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Sprint>(
            BaseSelect + " ORDER BY s.Status ASC, s.StartDate DESC");
    }

    public async Task<IEnumerable<Sprint>> GetByProjectAsync(int projectId)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Sprint>(
            BaseSelect + " WHERE s.ProjectId = @ProjectId ORDER BY s.Status ASC, s.StartDate DESC",
            new { ProjectId = projectId });
    }

    /// Verilen projelere ait sprintler (kullanıcının erişebildiği projeler için)
    public async Task<IEnumerable<Sprint>> GetByProjectsAsync(IEnumerable<int> projectIds)
    {
        var ids = projectIds.ToList();
        if (ids.Count == 0) return Enumerable.Empty<Sprint>();
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Sprint>(
            BaseSelect + " WHERE s.ProjectId IN @Ids ORDER BY s.Status ASC, s.StartDate DESC",
            new { Ids = ids });
    }

    public async Task<Sprint?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Sprint>(
            BaseSelect + " WHERE s.Id = @Id", new { Id = id });
    }

    /// Projede aktif sprint (varsa)
    public async Task<Sprint?> GetActiveByProjectAsync(int projectId)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Sprint>(
            BaseSelect + " WHERE s.ProjectId = @ProjectId AND s.Status = 1",
            new { ProjectId = projectId });
    }

    public async Task<int> CreateAsync(Sprint sprint)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO OrkaJira_Sprints
                (ProjectId, Name, Goal, StartDate, EndDate, Status, CreatedById, CreatedAt)
            VALUES
                (@ProjectId, @Name, @Goal, @StartDate, @EndDate, @Status, @CreatedById, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new
            {
                sprint.ProjectId,
                sprint.Name,
                sprint.Goal,
                sprint.StartDate,
                sprint.EndDate,
                Status = (int)sprint.Status,
                sprint.CreatedById
            });
    }

    public async Task UpdateAsync(Sprint sprint)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE OrkaJira_Sprints SET
                Name      = @Name,
                Goal      = @Goal,
                StartDate = @StartDate,
                EndDate   = @EndDate
            WHERE Id = @Id",
            new
            {
                sprint.Name,
                sprint.Goal,
                sprint.StartDate,
                sprint.EndDate,
                sprint.Id
            });
    }

    public async Task UpdateStatusAsync(int sprintId, SprintStatus status)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE OrkaJira_Sprints SET Status = @Status WHERE Id = @Id",
            new { Status = (int)status, Id = sprintId });
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        // Görevleri backlog'a geri bırak, sonra sprint'i sil
        await conn.ExecuteAsync(
            "UPDATE OrkaJira_Tasks SET SprintId = NULL WHERE SprintId = @Id", new { Id = id });
        await conn.ExecuteAsync(
            "DELETE FROM OrkaJira_Sprints WHERE Id = @Id", new { Id = id });
    }
}
