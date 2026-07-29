namespace Tezgah.Models;

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public bool IsSuperAdmin { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
