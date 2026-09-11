namespace Tezgah.Models;

public enum SprintStatus
{
    Planned = 0,
    Active = 1,
    Completed = 2
}

public class Sprint
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? ProjectKey { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);
    public SprintStatus Status { get; set; } = SprintStatus.Planned;
    public int CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Özet sayaçlar (sorgudan doldurulur)
    public int TaskCount { get; set; }
    public int DoneTaskCount { get; set; }

    public string StatusLabel => Status switch
    {
        SprintStatus.Planned => "Planlandı",
        SprintStatus.Active => "Aktif",
        SprintStatus.Completed => "Tamamlandı",
        _ => "Bilinmiyor"
    };

    public string StatusClass => Status switch
    {
        SprintStatus.Planned => "bg-secondary",
        SprintStatus.Active => "bg-success",
        SprintStatus.Completed => "bg-primary",
        _ => "bg-secondary"
    };

    public int ProgressPercent => TaskCount == 0 ? 0 : (int)Math.Round(DoneTaskCount * 100.0 / TaskCount);

    // Kalan gün (aktif sprint için anlamlı)
    public int DaysRemaining => (int)Math.Ceiling((EndDate.Date - DateTime.Today).TotalDays);

    public int TotalDays => Math.Max(1, (int)Math.Ceiling((EndDate.Date - StartDate.Date).TotalDays));
}
