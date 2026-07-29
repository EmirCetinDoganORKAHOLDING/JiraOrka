namespace Tezgah.Models;

public class PermissionGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool CanCreateProjects { get; set; } = true;
    public bool CanManageTasks { get; set; } = true;
    public bool CanViewReports { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int MemberCount { get; set; }
}
