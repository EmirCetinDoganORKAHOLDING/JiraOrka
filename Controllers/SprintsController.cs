using Tezgah.Models;
using Tezgah.Repositories;
using Tezgah.Services;
using Tezgah.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskStatus = Tezgah.Models.TaskStatus;

namespace Tezgah.Controllers;

[Authorize]
public class SprintsController : Controller
{
    private readonly SprintRepository _sprintRepo;
    private readonly TaskRepository _taskRepo;
    private readonly ProjectRepository _projectRepo;
    private readonly ProjectMemberRepository _memberRepo;
    private readonly CurrentUserService _currentUser;

    public SprintsController(SprintRepository sprintRepo, TaskRepository taskRepo,
        ProjectRepository projectRepo, ProjectMemberRepository memberRepo,
        CurrentUserService currentUser)
    {
        _sprintRepo = sprintRepo;
        _taskRepo = taskRepo;
        _projectRepo = projectRepo;
        _memberRepo = memberRepo;
        _currentUser = currentUser;
    }

    // Kullanıcının erişebildiği projeler
    private async Task<List<Project>> GetAccessibleProjectsAsync()
    {
        IEnumerable<Project> projects = _currentUser.CanViewAll
            ? await _projectRepo.GetAllAsync()
            : await _projectRepo.GetAllAsync(_currentUser.GroupId, _currentUser.UserId);
        return projects.ToList();
    }

    private async Task<bool> HasProjectAccessAsync(int projectId, int? groupId)
    {
        return _currentUser.CanViewAll
            || groupId == _currentUser.GroupId
            || await _memberRepo.HasAccessAsync(projectId, _currentUser.UserId);
    }

    // ─── INDEX (sprint listesi) ────────────────────────────────────────────────
    public async Task<IActionResult> Index(int? projectId = null)
    {
        var projects = await GetAccessibleProjectsAsync();
        var accessibleIds = projects.Select(p => p.Id).ToList();

        List<Sprint> sprints;
        if (projectId.HasValue)
        {
            if (!accessibleIds.Contains(projectId.Value)) return Forbid();
            sprints = (await _sprintRepo.GetByProjectAsync(projectId.Value)).ToList();
        }
        else
        {
            sprints = (await _sprintRepo.GetByProjectsAsync(accessibleIds)).ToList();
        }

        return View(new SprintListViewModel
        {
            ProjectId = projectId,
            ProjectName = projectId.HasValue
                ? projects.FirstOrDefault(p => p.Id == projectId)?.Name
                : null,
            Projects = projects,
            Sprints = sprints
        });
    }

    // ─── CREATE ─────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Create(int? projectId = null)
    {
        if (_currentUser.IsReadOnly) return Forbid();
        var projects = await GetAccessibleProjectsAsync();

        var model = new CreateSprintViewModel
        {
            ProjectId = projectId ?? 0,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(7),
            Projects = projects.Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSprintViewModel model)
    {
        if (_currentUser.IsReadOnly) return Forbid();

        var projects = await GetAccessibleProjectsAsync();
        var project = projects.FirstOrDefault(p => p.Id == model.ProjectId);
        if (project == null)
            ModelState.AddModelError(nameof(model.ProjectId), "Geçersiz proje.");

        if (model.EndDate.Date <= model.StartDate.Date)
            ModelState.AddModelError(nameof(model.EndDate), "Bitiş tarihi başlangıçtan sonra olmalı.");

        if (!ModelState.IsValid)
        {
            model.Projects = projects.Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
            return View(model);
        }

        var sprint = new Sprint
        {
            ProjectId = model.ProjectId,
            Name = model.Name,
            Goal = model.Goal,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Status = SprintStatus.Planned,
            CreatedById = _currentUser.UserId
        };

        var id = await _sprintRepo.CreateAsync(sprint);
        TempData["Success"] = "Sprint oluşturuldu. Backlog'tan görev ekleyip başlatabilirsiniz.";
        return RedirectToAction(nameof(Board), new { id });
    }

    // ─── BOARD (sprint panosu + backlog) ─────────────────────────────────────────
    public async Task<IActionResult> Board(int id)
    {
        var sprint = await _sprintRepo.GetByIdAsync(id);
        if (sprint == null) return NotFound();

        var project = await _projectRepo.GetByIdAsync(sprint.ProjectId);
        if (project == null) return NotFound();

        if (!await HasProjectAccessAsync(sprint.ProjectId, project.GroupId)) return Forbid();

        var sprintTasks = (await _taskRepo.GetBySprintAsync(id)).ToList();

        // Backlog: erişilebilen TÜM projelerin sprint'siz görevleri
        var accessibleIds = (await GetAccessibleProjectsAsync()).Select(p => p.Id).ToList();
        var backlog = (await _taskRepo.GetBacklogByProjectsAsync(accessibleIds)).ToList();

        var vm = new SprintBoardViewModel
        {
            Sprint = sprint,
            CanManage = !_currentUser.IsReadOnly,
            TodoTasks       = sprintTasks.Where(t => t.Status == TaskStatus.Todo).ToList(),
            InProgressTasks = sprintTasks.Where(t => t.Status == TaskStatus.InProgress).ToList(),
            InReviewTasks   = sprintTasks.Where(t => t.Status == TaskStatus.InReview).ToList(),
            DoneTasks       = sprintTasks.Where(t => t.Status == TaskStatus.Done).ToList(),
            BacklogTasks    = backlog
        };

        return View(vm);
    }

    // ─── SPRINT'İ BAŞLAT ─────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id)
    {
        var sprint = await _sprintRepo.GetByIdAsync(id);
        if (sprint == null) return NotFound();
        if (!await CanManageSprintAsync(sprint)) return Forbid();

        if (sprint.Status != SprintStatus.Planned)
        {
            TempData["Error"] = "Sadece planlanmış sprintler başlatılabilir.";
            return RedirectToAction(nameof(Board), new { id });
        }

        // Aynı projede zaten aktif bir sprint var mı?
        var active = await _sprintRepo.GetActiveByProjectAsync(sprint.ProjectId);
        if (active != null)
        {
            TempData["Error"] = $"Bu projede zaten aktif bir sprint var: {active.Name}. Önce onu tamamlayın.";
            return RedirectToAction(nameof(Board), new { id });
        }

        await _sprintRepo.UpdateStatusAsync(id, SprintStatus.Active);
        TempData["Success"] = "Sprint başlatıldı. Koşuyor! 🏃";
        return RedirectToAction(nameof(Board), new { id });
    }

    // ─── SPRINT'İ TAMAMLA ────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var sprint = await _sprintRepo.GetByIdAsync(id);
        if (sprint == null) return NotFound();
        if (!await CanManageSprintAsync(sprint)) return Forbid();

        if (sprint.Status != SprintStatus.Active)
        {
            TempData["Error"] = "Sadece aktif sprintler tamamlanabilir.";
            return RedirectToAction(nameof(Board), new { id });
        }

        // Bitmemiş görevler backlog'a döner, biten görevler sprint'te kalır (rapor için)
        await _taskRepo.ReturnIncompleteToBacklogAsync(id);
        await _sprintRepo.UpdateStatusAsync(id, SprintStatus.Completed);
        TempData["Success"] = "Sprint tamamlandı. Biten görevler korundu, bitmeyenler backlog'a döndü.";
        return RedirectToAction(nameof(Index), new { projectId = sprint.ProjectId });
    }

    // ─── GÖREV EKLE / ÇIKAR ──────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTask(int sprintId, int taskId)
    {
        var sprint = await _sprintRepo.GetByIdAsync(sprintId);
        if (sprint == null) return NotFound();
        if (!await CanManageSprintAsync(sprint)) return Forbid();

        var task = await _taskRepo.GetByIdAsync(taskId);
        if (task == null)
            return NotFound();

        // Görevin ait olduğu projeye erişim var mı? (farklı proje olabilir — sprint tüm projeleri kapsar)
        if (!await HasProjectAccessAsync(task.ProjectId, task.GroupId))
        {
            TempData["Error"] = "Bu göreve erişim yetkiniz yok.";
            return RedirectToAction(nameof(Board), new { id = sprintId });
        }

        await _taskRepo.AssignToSprintAsync(taskId, sprintId);
        TempData["Success"] = "Görev sprint'e eklendi.";
        return RedirectToAction(nameof(Board), new { id = sprintId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveTask(int sprintId, int taskId)
    {
        var sprint = await _sprintRepo.GetByIdAsync(sprintId);
        if (sprint == null) return NotFound();
        if (!await CanManageSprintAsync(sprint)) return Forbid();

        await _taskRepo.RemoveFromSprintAsync(taskId);
        TempData["Success"] = "Görev backlog'a geri alındı.";
        return RedirectToAction(nameof(Board), new { id = sprintId });
    }

    // ─── SİL ─────────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var sprint = await _sprintRepo.GetByIdAsync(id);
        if (sprint == null) return NotFound();
        if (!await CanManageSprintAsync(sprint)) return Forbid();

        var projectId = sprint.ProjectId;
        await _sprintRepo.DeleteAsync(id);
        TempData["Success"] = "Sprint silindi. Görevleri backlog'a döndü.";
        return RedirectToAction(nameof(Index), new { projectId });
    }

    private async Task<bool> CanManageSprintAsync(Sprint sprint)
    {
        if (_currentUser.IsReadOnly) return false;
        var project = await _projectRepo.GetByIdAsync(sprint.ProjectId);
        if (project == null) return false;
        return await HasProjectAccessAsync(sprint.ProjectId, project.GroupId);
    }
}
