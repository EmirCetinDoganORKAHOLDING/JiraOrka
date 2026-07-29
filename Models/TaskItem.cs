namespace Tezgah.Models;

public enum TaskStatus
{
    Todo = 0,
    InProgress = 1,
    InReview = 2,
    Done = 3
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public string? AssignedToEmail { get; set; }
    public int CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public int?    RequesterId      { get; set; }
    public string? RequesterName    { get; set; }
    public string? RequesterEmail   { get; set; }
    public string? RequesterDetails { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public List<Comment> Comments { get; set; } = new();

    public string StatusLabel => Status switch
    {
        TaskStatus.Todo => "Yapılacak",
        TaskStatus.InProgress => "Devam Ediyor",
        TaskStatus.InReview => "İncelemede",
        TaskStatus.Done => "Tamamlandı",
        _ => "Bilinmiyor"
    };

    public string PriorityLabel => Priority switch
    {
        TaskPriority.Low => "Düşük",
        TaskPriority.Medium => "Orta",
        TaskPriority.High => "Yüksek",
        TaskPriority.Critical => "Kritik",
        _ => "Bilinmiyor"
    };

    public string StatusClass => Status switch
    {
        TaskStatus.Todo => "status-todo",
        TaskStatus.InProgress => "status-inprogress",
        TaskStatus.InReview => "status-inreview",
        TaskStatus.Done => "status-done",
        _ => ""
    };

    public string PriorityClass => Priority switch
    {
        TaskPriority.Low => "priority-low",
        TaskPriority.Medium => "priority-medium",
        TaskPriority.High => "priority-high",
        TaskPriority.Critical => "priority-critical",
        _ => ""
    };
}
