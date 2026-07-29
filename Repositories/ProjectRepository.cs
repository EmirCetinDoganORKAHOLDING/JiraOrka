using Dapper;
using Tezgah.Data;
using Tezgah.Models;

namespace Tezgah.Repositories;

public class ProjectRepository
{
    private readonly DapperContext _context;

    public ProjectRepository(DapperContext context) => _context = context;

    private const string ProjectSelectSql = @"
        SELECT p.*,
               g.Name     AS GroupName,
               u.FullName AS CreatedByName,
               COUNT(t.Id) AS TaskCount,
               SUM(CASE WHEN t.Status <> 3 THEN 1 ELSE 0 END) AS OpenTaskCount
        FROM OrkaJira_Projects p
        LEFT JOIN OrkaJira_PermissionGroups g ON p.GroupId = g.Id
        LEFT JOIN OrkaJira_Users            u ON p.CreatedById = u.Id
        LEFT JOIN OrkaJira_Tasks            t ON p.Id = t.ProjectId";

    private const string ProjectGroupBy = @"
        GROUP BY p.Id, p.Name, p.Description, p.[Key],
                 p.GroupId, p.CreatedById, p.IsActive, p.CreatedAt,
                 g.Name, u.FullName
        ORDER BY p.CreatedAt DESC";

    /// groupId verilirse: gruba ait projeler + (mümkünse) kullanıcıya direkt atanmış projeler
    public async Task<IEnumerable<Project>> GetAllAsync(int? groupId = null, int? userId = null)
    {
        using var conn = _context.CreateConnection();

        // 1) Grup projeleri (temel sorgu - her zaman çalışır)
        var sql = ProjectSelectSql + " WHERE p.IsActive = 1";
        if (groupId.HasValue)
            sql += " AND p.GroupId = @GroupId";
        sql += ProjectGroupBy;

        var result = (await conn.QueryAsync<Project>(sql, new { GroupId = groupId })).ToList();

        // 2) Direkt atanan projeler — OrkaJira_ProjectMembers tablosu yoksa sessizce atla
        if (userId.HasValue)
        {
            try
            {
                var memberIds = (await conn.QueryAsync<int>(
                    "SELECT DISTINCT ProjectId FROM OrkaJira_ProjectMembers WHERE UserId = @UserId",
                    new { UserId = userId })).ToList();

                if (memberIds.Any())
                {
                    var existing = result.Select(p => p.Id).ToHashSet();
                    var newIds   = memberIds.Where(id => !existing.Contains(id)).ToList();

                    if (newIds.Any())
                    {
                        var extra = await conn.QueryAsync<Project>(
                            ProjectSelectSql +
                            " WHERE p.IsActive = 1 AND p.Id IN @Ids" +
                            ProjectGroupBy,
                            new { Ids = newIds });
                        result.AddRange(extra);
                    }
                }
            }
            catch
            {
                // OrkaJira_ProjectMembers tablosu henüz oluşturulmamış — grup projeleriyle devam et
            }
        }

        return result;
    }

    public async Task<Project?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Project>(@"
            SELECT p.*,
                   g.Name  AS GroupName,
                   u.FullName AS CreatedByName,
                   COUNT(t.Id) AS TaskCount,
                   SUM(CASE WHEN t.Status <> 3 THEN 1 ELSE 0 END) AS OpenTaskCount
            FROM OrkaJira_Projects p
            LEFT JOIN OrkaJira_PermissionGroups g ON p.GroupId = g.Id
            LEFT JOIN OrkaJira_Users            u ON p.CreatedById = u.Id
            LEFT JOIN OrkaJira_Tasks            t ON p.Id = t.ProjectId
            WHERE p.Id = @Id
            GROUP BY p.Id, p.Name, p.Description, p.[Key],
                     p.GroupId, p.CreatedById, p.IsActive, p.CreatedAt,
                     g.Name, u.FullName", new { Id = id });
    }

    public async Task<int> CreateAsync(Project project)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO OrkaJira_Projects
                (Name, Description, [Key], GroupId, CreatedById, IsActive)
            VALUES
                (@Name, @Description, @Key, @GroupId, @CreatedById, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", project);
    }

    public async Task UpdateAsync(Project project)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE OrkaJira_Projects SET
                Name      = @Name,
                Description = @Description,
                [Key]     = @Key,
                GroupId   = @GroupId,
                IsActive  = @IsActive
            WHERE Id = @Id", project);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE OrkaJira_Projects SET IsActive = 0 WHERE Id = @Id", new { Id = id });
    }
}
