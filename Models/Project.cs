namespace Tezgah.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Key { get; set; }
    public int? GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int TaskCount { get; set; }
    public int OpenTaskCount { get; set; }
}
