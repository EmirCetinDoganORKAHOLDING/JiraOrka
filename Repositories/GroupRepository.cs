using Dapper;
using Tezgah.Data;
using Tezgah.Models;

namespace Tezgah.Repositories;

public class GroupRepository
{
    private readonly DapperContext _context;

    public GroupRepository(DapperContext context) => _context = context;

    public async Task<IEnumerable<PermissionGroup>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<PermissionGroup>(@"
            SELECT g.*, COUNT(u.Id) AS MemberCount
            FROM OrkaJira_PermissionGroups g
            LEFT JOIN OrkaJira_Users u ON g.Id = u.GroupId AND u.IsActive = 1
            GROUP BY g.Id, g.Name, g.Description,
                     g.CanCreateProjects, g.CanManageTasks, g.CanViewReports, g.CreatedAt
            ORDER BY g.Name");
    }

    public async Task<PermissionGroup?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PermissionGroup>(@"
            SELECT g.*, COUNT(u.Id) AS MemberCount
            FROM OrkaJira_PermissionGroups g
            LEFT JOIN OrkaJira_Users u ON g.Id = u.GroupId AND u.IsActive = 1
            WHERE g.Id = @Id
            GROUP BY g.Id, g.Name, g.Description,
                     g.CanCreateProjects, g.CanManageTasks, g.CanViewReports, g.CreatedAt",
            new { Id = id });
    }

    public async Task<int> CreateAsync(PermissionGroup group)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO OrkaJira_PermissionGroups
                (Name, Description, CanCreateProjects, CanManageTasks, CanViewReports)
            VALUES
                (@Name, @Description, @CanCreateProjects, @CanManageTasks, @CanViewReports);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", group);
    }

    public async Task UpdateAsync(PermissionGroup group)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE OrkaJira_PermissionGroups SET
                Name               = @Name,
                Description        = @Description,
                CanCreateProjects  = @CanCreateProjects,
                CanManageTasks     = @CanManageTasks,
                CanViewReports     = @CanViewReports
            WHERE Id = @Id", group);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE OrkaJira_Users SET GroupId = NULL WHERE GroupId = @Id", new { Id = id });
        await conn.ExecuteAsync(
            "DELETE FROM OrkaJira_PermissionGroups WHERE Id = @Id", new { Id = id });
    }
}
