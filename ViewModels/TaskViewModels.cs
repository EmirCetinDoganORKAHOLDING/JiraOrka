using System.ComponentModel.DataAnnotations;
using Tezgah.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using ModelTaskStatus = Tezgah.Models.TaskStatus;

namespace Tezgah.ViewModels;

public class CreateTaskViewModel
{
    [Required(ErrorMessage = "Başlık zorunludur")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Proje seçimi zorunludur")]
    public int ProjectId { get; set; }

    public int? AssignedToId { get; set; }

    public int?    RequesterId      { get; set; }
    public string? RequesterDetails { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateTime? DueDate { get; set; }

    public List<SelectListItem> Projects { get; set; } = new();
    public List<SelectListItem> Users    { get; set; } = new();
}

public class EditTaskViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Başlık zorunludur")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int  ProjectId    { get; set; }
    public int? AssignedToId { get; set; }

    public int?    RequesterId      { get; set; }
    public string? RequesterDetails { get; set; }

    public ModelTaskStatus Status   { get; set; }
    public TaskPriority    Priority { get; set; }
    public DateTime?       DueDate  { get; set; }

    public List<SelectListItem> Projects { get; set; } = new();
    public List<SelectListItem> Users    { get; set; } = new();
}

public class BoardViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectKey  { get; set; } = string.Empty;
    public List<Project> Projects { get; set; } = new();
    public List<TaskItem> TodoTasks       { get; set; } = new();
    public List<TaskItem> InProgressTasks { get; set; } = new();
    public List<TaskItem> InReviewTasks   { get; set; } = new();
    public List<TaskItem> DoneTasks       { get; set; } = new();
}

public class AdminDashboardViewModel
{
    public int TotalUsers    { get; set; }
    public int TotalGroups   { get; set; }
    public int TotalProjects { get; set; }
    public int TotalTasks    { get; set; }
    public int OpenTasks     { get; set; }
    public int DoneTasks     { get; set; }

    public IEnumerable<AppUser> Users { get; set; } = new List<AppUser>();
}

public class UserKanbanViewModel
{
    public AppUser        User            { get; set; } = null!;
    public List<TaskItem> TodoTasks       { get; set; } = new();
    public List<TaskItem> InProgressTasks { get; set; } = new();
    public List<TaskItem> InReviewTasks   { get; set; } = new();
    public List<TaskItem> DoneTasks       { get; set; } = new();
    public int TotalTasks => TodoTasks.Count + InProgressTasks.Count + InReviewTasks.Count + DoneTasks.Count;
}

public class TaskFilterViewModel
{
    public DateTime? StartDate    { get; set; }
    public DateTime? EndDate      { get; set; }
    public string?   RequesterName { get; set; }
    public int?      AssignedToId { get; set; }
    public int?      Status       { get; set; }
    public int?      Priority     { get; set; }
    public int?      ProjectId    { get; set; }

    public IEnumerable<TaskItem> Tasks    { get; set; } = new List<TaskItem>();
    public List<SelectListItem>  Users    { get; set; } = new();
    public List<SelectListItem>  Projects { get; set; } = new();
}

public class UserDetailViewModel
{
    public AppUser               User          { get; set; } = null!;
    public IEnumerable<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    public IEnumerable<TaskItem> CreatedTasks  { get; set; } = new List<TaskItem>();
    public IEnumerable<Project>  ExtraProjects { get; set; } = new List<Project>();
    public IEnumerable<Project>  AllProjects   { get; set; } = new List<Project>();

    public int TotalAssigned => AssignedTasks.Count();
    public int DoneAssigned  => AssignedTasks.Count(t => t.Status == ModelTaskStatus.Done);
    public int OpenAssigned  => AssignedTasks.Count(t => t.Status != ModelTaskStatus.Done);
}
