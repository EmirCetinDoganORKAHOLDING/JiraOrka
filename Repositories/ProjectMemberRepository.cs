using Dapper;
using Tezgah.Data;
using Tezgah.Models;

namespace Tezgah.Repositories;

public class ProjectMemberRepository
{
    private readonly DapperContext _context;

    public ProjectMemberRepository(DapperContext context) => _context = context;

    /// Bir projenin tüm üyelerini döner (grup üyeliği + direkt atama)
    public async Task<IEnumerable<AppUser>> GetProjectMembersAsync(int projectId)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<AppUser>(@"
            SELECT DISTINCT u.*, g.Name AS GroupName
            FROM OrkaJira_Users u
            LEFT JOIN OrkaJira_PermissionGroups g ON u.GroupId = g.Id
            WHERE u.IsActive = 1
              AND (
                  -- Projenin grubuna üye
                  u.GroupId = (SELECT GroupId FROM OrkaJira_Projects WHERE Id = @ProjectId)
                  OR
                  -- Direkt projeye atanmış
                  u.Id IN (SELECT UserId FROM OrkaJira_ProjectMembers WHERE ProjectId = @ProjectId)
              )
            ORDER BY u.FullName", new { ProjectId = projectId });
    }

    /// Bir kullanıcının görebileceği proje ID'lerini döner (extra atamalar)
    public async Task<IEnumerable<int>> GetUserProjectIdsAsync(int userId)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<int>(@"
            SELECT ProjectId FROM OrkaJira_ProjectMembers WHERE UserId = @UserId",
            new { UserId = userId });
    }

    /// Projeye direkt atanmış kullanıcıları döner (grup üyeliği hariç)
    public async Task<IEnumerable<AppUser>> GetDirectMembersAsync(int projectId)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<AppUser>(@"
            SELECT u.*, g.Name AS GroupName
            FROM OrkaJira_ProjectMembers pm
            JOIN OrkaJira_Users u ON pm.UserId = u.Id
            LEFT JOIN OrkaJira_PermissionGroups g ON u.GroupId = g.Id
            WHERE pm.ProjectId = @ProjectId
            ORDER BY u.FullName", new { ProjectId = projectId });
    }

    /// Kullanıcıyı projeye ekle
    public async Task AddMemberAsync(int projectId, int userId)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(@"
            IF NOT EXISTS (
                SELECT 1 FROM OrkaJira_ProjectMembers
                WHERE ProjectId = @ProjectId AND UserId = @UserId
            )
            INSERT INTO OrkaJira_ProjectMembers (ProjectId, UserId)
            VALUES (@ProjectId, @UserId)",
            new { ProjectId = projectId, UserId = userId });
    }

    /// Kullanıcının ek proje atamalarını döner
    public async Task<IEnumerable<Project>> GetUserProjectsAsync(int userId)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Project>(@"
            SELECT p.*, g.Name AS GroupName
            FROM OrkaJira_ProjectMembers pm
            JOIN OrkaJira_Projects p ON pm.ProjectId = p.Id
            LEFT JOIN OrkaJira_PermissionGroups g ON p.GroupId = g.Id
            WHERE pm.UserId = @UserId
            ORDER BY p.Name", new { UserId = userId });
    }

    /// Kullanıcıyı projeden çıkar
    public async Task RemoveMemberAsync(int projectId, int userId)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(@"
            DELETE FROM OrkaJira_ProjectMembers
            WHERE ProjectId = @ProjectId AND UserId = @UserId",
            new { ProjectId = projectId, UserId = userId });
    }

    /// Kullanıcının bu projeye erişimi var mı?
    public async Task<bool> HasAccessAsync(int projectId, int userId)
    {
        using var conn = _context.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM OrkaJira_ProjectMembers
            WHERE ProjectId = @ProjectId AND UserId = @UserId",
            new { ProjectId = projectId, UserId = userId });
        return count > 0;
    }
}
