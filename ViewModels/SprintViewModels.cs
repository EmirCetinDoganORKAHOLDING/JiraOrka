using System.ComponentModel.DataAnnotations;
using Tezgah.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Tezgah.ViewModels;

public class SprintListViewModel
{
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public List<Project> Projects { get; set; } = new();
    public List<Sprint> Sprints { get; set; } = new();
}

public class CreateSprintViewModel
{
    [Required(ErrorMessage = "Sprint adı zorunludur")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Proje seçimi zorunludur")]
    public int ProjectId { get; set; }

    public string? Goal { get; set; }

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Bitiş tarihi zorunludur")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);

    public List<SelectListItem> Projects { get; set; } = new();
}

public class SprintBoardViewModel
{
    public Sprint Sprint { get; set; } = null!;
    public List<TaskItem> TodoTasks       { get; set; } = new();
    public List<TaskItem> InProgressTasks { get; set; } = new();
    public List<TaskItem> InReviewTasks   { get; set; } = new();
    public List<TaskItem> DoneTasks       { get; set; } = new();
    public List<TaskItem> BacklogTasks    { get; set; } = new();

    public bool CanManage { get; set; }

    public int TotalTasks => TodoTasks.Count + InProgressTasks.Count + InReviewTasks.Count + DoneTasks.Count;
}
