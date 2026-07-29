using Dapper;
using Tezgah.Data;
using Tezgah.Models;

namespace Tezgah.Repositories;

public class UserRepository
{
    private readonly DapperContext _context;

    public UserRepository(DapperContext context) => _context = context;

    public async Task<AppUser?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AppUser>(@"
            SELECT u.*, g.Name AS GroupName
            FROM OrkaJira_Users u
            LEFT JOIN OrkaJira_PermissionGroups g ON u.GroupId = g.Id
            WHERE u.Id = @Id", new { Id = id });
    }

    public async Task<AppUser?> GetByEmailAsync(string email)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AppUser>(@"
            SELECT u.*, g.Name AS GroupName
            FROM OrkaJira_Users u
            LEFT JOIN OrkaJira_PermissionGroups g ON u.GroupId = g.Id
            WHERE LOWER(u.Email) = LOWER(@Email)", new { Email = email });
    }

    public async Task<AppUser?> GetByUsernameAsync(string username)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AppUser>(@"
            SELECT u.*, g.Name AS GroupName
            FROM OrkaJira_Users u
            LEFT JOIN OrkaJira_PermissionGroups g ON u.GroupId = g.Id
            WHERE LOWER(u.Username) = LOWER(@Username)", new { Username = username });
    }

    public async Task<IEnumerable<AppUser>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<AppUser>(@"
            SELECT u.*, g.Name AS GroupName
            FROM OrkaJira_Users u
            LEFT JOIN OrkaJira_PermissionGroups g ON u.GroupId = g.Id
            ORDER BY u.FullName");
    }

    public async Task<IEnumerable<AppUser>> GetByGroupAsync(int groupId)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<AppUser>(@"
            SELECT u.*, g.Name AS GroupName
            FROM OrkaJira_Users u
            LEFT JOIN OrkaJira_PermissionGroups g ON u.GroupId = g.Id
            WHERE u.GroupId = @GroupId AND u.IsActive = 1
            ORDER BY u.FullName", new { GroupId = groupId });
    }

    public async Task<int> CreateAsync(AppUser user)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO OrkaJira_Users
                (Username, Email, PasswordHash, FullName, GroupId, IsSuperAdmin, IsReadOnly, IsActive)
            VALUES
                (@Username, @Email, @PasswordHash, @FullName, @GroupId, @IsSuperAdmin, @IsReadOnly, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", user);
    }

    public async Task UpdateAsync(AppUser user)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE OrkaJira_Users SET
                Username     = @Username,
                Email        = @Email,
                FullName     = @FullName,
                GroupId      = @GroupId,
                IsActive     = @IsActive,
                IsSuperAdmin = @IsSuperAdmin,
                IsReadOnly   = @IsReadOnly
            WHERE Id = @Id", user);
    }

    public async Task UpdatePasswordAsync(int userId, string passwordHash)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE OrkaJira_Users SET PasswordHash = @Hash WHERE Id = @Id",
            new { Hash = passwordHash, Id = userId });
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM OrkaJira_Users WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
    {
        using var conn = _context.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM OrkaJira_Users
            WHERE LOWER(Email) = LOWER(@Email)
              AND (@ExcludeId IS NULL OR Id <> @ExcludeId)",
            new { Email = email, ExcludeId = excludeId });
        return count > 0;
    }

    public async Task<bool> UsernameExistsAsync(string username, int? excludeId = null)
    {
        using var conn = _context.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM OrkaJira_Users
            WHERE LOWER(Username) = LOWER(@Username)
              AND (@ExcludeId IS NULL OR Id <> @ExcludeId)",
            new { Username = username, ExcludeId = excludeId });
        return count > 0;
    }
}
