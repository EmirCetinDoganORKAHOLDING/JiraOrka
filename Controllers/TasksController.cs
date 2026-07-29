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
public class TasksController : Controller
{
    private readonly TaskRepository _taskRepo;
    private readonly ProjectRepository _projectRepo;
    private readonly UserRepository _userRepo;
    private readonly ProjectMemberRepository _memberRepo;
    private readonly EmailService _emailService;
    private readonly CurrentUserService _currentUser;
    private readonly IWebHostEnvironment _hostEnv;

    public TasksController(TaskRepository taskRepo, ProjectRepository projectRepo,
        UserRepository userRepo, ProjectMemberRepository memberRepo,
        EmailService emailService, CurrentUserService currentUser,
        IWebHostEnvironment hostEnv)
    {
        _taskRepo = taskRepo;
        _projectRepo = projectRepo;
        _userRepo = userRepo;
        _memberRepo = memberRepo;
        _emailService = emailService;
        _currentUser = currentUser;
        _hostEnv = hostEnv;
    }

    // ─── Helper: Görevle ilgili herkese mail gönder ───────────────────────────
    private async Task NotifyTaskParticipants(TaskItem task, string subject,
        string htmlBodyTemplate, int excludeUserId = 0)
    {
        var notified = new HashSet<int>();
        if (excludeUserId > 0) notified.Add(excludeUserId);

        async Task SendIfNew(int? userId, string? email, string? name)
        {
            if (userId == null || string.IsNullOrEmpty(email)) return;
            if (!notified.Add(userId.Value)) return;
            await _emailService.SendAsync(email, name ?? "", subject,
                htmlBodyTemplate.Replace("{{NAME}}", name ?? ""));
        }

        await SendIfNew(task.AssignedToId,  task.AssignedToEmail,  task.AssignedToName);
        await SendIfNew(task.RequesterId,   task.RequesterEmail,   task.RequesterName);

        var creator = await _userRepo.GetByIdAsync(task.CreatedById);
        if (creator != null)
            await SendIfNew(creator.Id, creator.Email, creator.FullName);
    }

    // ─── CREATE ───────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Create(int? projectId = null)
    {
        if (_currentUser.IsReadOnly) return Forbid();
        var model = new CreateTaskViewModel { ProjectId = projectId ?? 0 };
        await PopulateCreateViewModelAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTaskViewModel model)
    {
        if (_currentUser.IsReadOnly) return Forbid();
        if (!ModelState.IsValid)
        {
            await PopulateCreateViewModelAsync(model);
            return View(model);
        }

        var project = await _projectRepo.GetByIdAsync(model.ProjectId);
        if (project == null || (!_currentUser.IsSuperAdmin
            && project.GroupId != _currentUser.GroupId
            && !await _memberRepo.HasAccessAsync(model.ProjectId, _currentUser.UserId)))
            return Forbid();

        // Talep eden adını kullanıcıdan al
        string? requesterName = null;
        if (model.RequesterId.HasValue)
        {
            var req = await _userRepo.GetByIdAsync(model.RequesterId.Value);
            requesterName = req?.FullName;
        }

        var task = new TaskItem
        {
            Title            = model.Title,
            Description      = model.Description,
            ProjectId        = model.ProjectId,
            AssignedToId     = model.AssignedToId,
            RequesterId      = model.RequesterId,
            RequesterName    = requesterName,
            RequesterDetails = model.RequesterDetails,
            CreatedById      = _currentUser.UserId,
            Priority         = model.Priority,
            DueDate          = model.DueDate,
            Status           = TaskStatus.Todo
        };

        var taskId = await _taskRepo.CreateAsync(task);
        task.Id = taskId;

        // Oluşturulan task'ı tam bilgileriyle çek (email için)
        var fullTask = await _taskRepo.GetByIdAsync(taskId);
        if (fullTask != null)
        {
            var body = $@"
            <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
              <div style='background:#3b82f6;color:white;padding:20px;border-radius:8px 8px 0 0;'>
                <h2 style='margin:0;'>🎯 Yeni Görev Oluşturuldu</h2>
              </div>
              <div style='background:#f8fafc;padding:20px;border:1px solid #e2e8f0;'>
                <p>Merhaba <strong>{{{{NAME}}}}</strong>,</p>
                <p><strong>{_currentUser.FullName}</strong> yeni bir görev oluşturdu.</p>
                <div style='background:white;padding:15px;border-radius:8px;border-left:4px solid #3b82f6;margin:15px 0;'>
                  <p style='margin:0;'><strong>Görev:</strong> {fullTask.Title}</p>
                  <p style='margin:5px 0 0;'><strong>Proje:</strong> {project.Name}</p>
                  {(fullTask.AssignedToName != null ? $"<p style='margin:5px 0 0;'><strong>Atanan:</strong> {fullTask.AssignedToName}</p>" : "")}
                </div>
              </div>
              <div style='background:#e2e8f0;padding:10px;text-align:center;border-radius:0 0 8px 8px;font-size:12px;color:#64748b;'>Tezgah</div>
            </div>";

            await NotifyTaskParticipants(fullTask, $"[Tezgah] Yeni Görev: {fullTask.Title}",
                body, _currentUser.UserId);
        }

        TempData["Success"] = "Görev oluşturuldu.";
        return RedirectToAction("Index", "Board", new { projectId = model.ProjectId });
    }

    // ─── DETAILS ──────────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(int id)
    {
        var task = await _taskRepo.GetByIdAsync(id);
        if (task == null) return NotFound();

        var hasAccess = _currentUser.CanViewAll
            || task.GroupId == _currentUser.GroupId
            || await _memberRepo.HasAccessAsync(task.ProjectId, _currentUser.UserId);
        if (!hasAccess) return Forbid();

        return View(task);
    }

    // ─── ADD COMMENT ──────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> AddComment(int taskId, string content,
        IFormFileCollection? attachments)
    {
        if (_currentUser.IsReadOnly) return Forbid();
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Yorum boş olamaz.";
            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        var task = await _taskRepo.GetByIdAsync(taskId);
        if (task == null) return NotFound();

        var hasAccess = _currentUser.CanViewAll
            || task.GroupId == _currentUser.GroupId
            || await _memberRepo.HasAccessAsync(task.ProjectId, _currentUser.UserId);
        if (!hasAccess) return Forbid();

        var comment = new Comment
        {
            TaskId  = taskId,
            UserId  = _currentUser.UserId,
            Content = content
        };

        var commentId = await _taskRepo.AddCommentAsync(comment);

        if (attachments != null && attachments.Count > 0)
        {
            var uploadDir = Path.Combine(_hostEnv.WebRootPath, "uploads", "attachments");
            Directory.CreateDirectory(uploadDir);
            foreach (var file in attachments)
            {
                if (file.Length == 0) continue;
                var storedName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                using var stream = new FileStream(Path.Combine(uploadDir, storedName), FileMode.Create);
                await file.CopyToAsync(stream);
                await _taskRepo.AddAttachmentAsync(new CommentAttachment
                {
                    CommentId      = commentId,
                    FileName       = file.FileName,
                    StoredFileName = storedName,
                    ContentType    = file.ContentType,
                    FileSize       = file.Length
                });
            }
        }

        var fileCount  = attachments?.Count(f => f.Length > 0) ?? 0;
        var attachNote = fileCount > 0 ? $" ({fileCount} dosya eki)" : "";
        var body = $@"
        <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
          <div style='background:#8b5cf6;color:white;padding:20px;border-radius:8px 8px 0 0;'>
            <h2 style='margin:0;'>💬 Yeni Yorum</h2>
          </div>
          <div style='background:#f8fafc;padding:20px;border:1px solid #e2e8f0;'>
            <p>Merhaba <strong>{{{{NAME}}}}</strong>,</p>
            <p><strong>{_currentUser.FullName}</strong> göreve yorum ekledi.</p>
            <div style='background:white;padding:15px;border-radius:8px;border-left:4px solid #8b5cf6;margin:15px 0;'>
              <p style='margin:0;'><strong>Görev:</strong> {task.Title}</p>
              <p style='margin:5px 0 0;'><strong>Yorum:</strong> {content}{attachNote}</p>
            </div>
          </div>
          <div style='background:#e2e8f0;padding:10px;text-align:center;border-radius:0 0 8px 8px;font-size:12px;color:#64748b;'>Tezgah</div>
        </div>";

        await NotifyTaskParticipants(task, $"[Tezgah] Yeni Yorum: {task.Title}",
            body, _currentUser.UserId);

        TempData["Success"] = "Yorum eklendi.";
        return RedirectToAction(nameof(Details), new { id = taskId });
    }

    // ─── EDIT ─────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (_currentUser.IsReadOnly) return Forbid();
        var task = await _taskRepo.GetByIdAsync(id);
        if (task == null) return NotFound();

        var hasAccess = _currentUser.CanViewAll
            || task.GroupId == _currentUser.GroupId
            || await _memberRepo.HasAccessAsync(task.ProjectId, _currentUser.UserId);
        if (!hasAccess) return Forbid();

        var model = new EditTaskViewModel
        {
            Id               = task.Id,
            Title            = task.Title,
            Description      = task.Description,
            ProjectId        = task.ProjectId,
            AssignedToId     = task.AssignedToId,
            RequesterId      = task.RequesterId,
            RequesterDetails = task.RequesterDetails,
            Status           = task.Status,
            Priority         = task.Priority,
            DueDate          = task.DueDate,
        };
        await PopulateEditViewModelAsync(model, task.ProjectId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditTaskViewModel model)
    {
        if (_currentUser.IsReadOnly) return Forbid();
        var task = await _taskRepo.GetByIdAsync(model.Id);
        if (task == null) return NotFound();

        var hasAccess = _currentUser.CanViewAll
            || task.GroupId == _currentUser.GroupId
            || await _memberRepo.HasAccessAsync(task.ProjectId, _currentUser.UserId);
        if (!hasAccess) return Forbid();

        if (!ModelState.IsValid)
        {
            await PopulateEditViewModelAsync(model, task.ProjectId);
            return View(model);
        }

        var oldStatus   = task.Status;
        var oldAssignee = task.AssignedToId;

        // Talep eden adını kullanıcıdan al
        string? requesterName = null;
        if (model.RequesterId.HasValue)
        {
            var req = await _userRepo.GetByIdAsync(model.RequesterId.Value);
            requesterName = req?.FullName;
        }

        task.Title            = model.Title;
        task.Description      = model.Description;
        task.ProjectId        = model.ProjectId > 0 ? model.ProjectId : task.ProjectId;
        task.AssignedToId     = model.AssignedToId;
        task.RequesterId      = model.RequesterId;
        task.RequesterName    = requesterName;
        task.RequesterDetails = model.RequesterDetails;
        task.Status           = model.Status;
        task.Priority         = model.Priority;
        task.DueDate          = model.DueDate;

        await _taskRepo.UpdateAsync(task);

        // Mail: yeni atama
        if (model.AssignedToId.HasValue && model.AssignedToId != oldAssignee)
        {
            var newAssignee = await _userRepo.GetByIdAsync(model.AssignedToId.Value);
            var project     = await _projectRepo.GetByIdAsync(task.ProjectId);
            if (newAssignee != null && !string.IsNullOrEmpty(newAssignee.Email))
                await _emailService.SendTaskAssignedAsync(
                    newAssignee.Email, newAssignee.FullName,
                    task.Title, project?.Name ?? "",
                    _currentUser.FullName, task.Id);
        }

        // Mail: durum değişikliği → herkese
        if (oldStatus != model.Status)
        {
            var updatedTask = await _taskRepo.GetByIdAsync(task.Id);
            if (updatedTask != null)
            {
                var statusLabels = new[] { "Yapılacak", "Devam Ediyor", "İncelemede", "Tamamlandı" };
                var oldLabel = statusLabels.ElementAtOrDefault((int)oldStatus) ?? oldStatus.ToString();
                var newLabel = statusLabels.ElementAtOrDefault((int)model.Status) ?? model.Status.ToString();

                var body = $@"
                <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
                  <div style='background:#10b981;color:white;padding:20px;border-radius:8px 8px 0 0;'>
                    <h2 style='margin:0;'>✅ Görev Durumu Güncellendi</h2>
                  </div>
                  <div style='background:#f8fafc;padding:20px;border:1px solid #e2e8f0;'>
                    <p>Merhaba <strong>{{{{NAME}}}}</strong>,</p>
                    <p><strong>{_currentUser.FullName}</strong> görev durumunu güncelledi.</p>
                    <div style='background:white;padding:15px;border-radius:8px;border-left:4px solid #10b981;margin:15px 0;'>
                      <p style='margin:0;'><strong>Görev:</strong> {updatedTask.Title}</p>
                      <p style='margin:5px 0 0;'><strong>Eski Durum:</strong> {oldLabel}</p>
                      <p style='margin:5px 0 0;'><strong>Yeni Durum:</strong> {newLabel}</p>
                    </div>
                  </div>
                  <div style='background:#e2e8f0;padding:10px;text-align:center;border-radius:0 0 8px 8px;font-size:12px;color:#64748b;'>Tezgah</div>
                </div>";

                await NotifyTaskParticipants(updatedTask,
                    $"[Tezgah] Durum Değişti: {updatedTask.Title}", body, _currentUser.UserId);
            }
        }

        TempData["Success"] = "Görev güncellendi.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    // ─── DELETE ───────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (_currentUser.IsReadOnly) return Forbid();
        var task = await _taskRepo.GetByIdAsync(id);
        if (task == null) return NotFound();

        var hasAccess = _currentUser.CanViewAll
            || task.GroupId == _currentUser.GroupId
            || await _memberRepo.HasAccessAsync(task.ProjectId, _currentUser.UserId);
        if (!hasAccess) return Forbid();

        var projectId = task.ProjectId;
        await _taskRepo.DeleteAsync(id);
        TempData["Success"] = "Görev silindi.";
        return RedirectToAction("Index", "Board", new { projectId });
    }

    // ─── UPDATE STATUS (drag & drop) ──────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
    {
        if (_currentUser.IsReadOnly)
            return Json(new { success = false, message = "Salt-okunur erişim: durum değiştirilemez." });
        var task = await _taskRepo.GetByIdAsync(request.TaskId);
        if (task == null)
            return Json(new { success = false, message = "Görev bulunamadı." });

        var hasAccess = _currentUser.CanViewAll
            || task.GroupId == _currentUser.GroupId
            || await _memberRepo.HasAccessAsync(task.ProjectId, _currentUser.UserId);
        if (!hasAccess)
            return Json(new { success = false, message = "Yetki hatası." });

        if (!Enum.IsDefined(typeof(TaskStatus), request.Status))
            return Json(new { success = false, message = "Geçersiz durum." });

        var oldStatus = task.Status;
        var newStatus = (TaskStatus)request.Status;
        await _taskRepo.UpdateStatusAsync(request.TaskId, newStatus);

        if (oldStatus != newStatus)
        {
            var statusLabels = new[] { "Yapılacak", "Devam Ediyor", "İncelemede", "Tamamlandı" };
            var oldLabel = statusLabels.ElementAtOrDefault((int)oldStatus) ?? oldStatus.ToString();
            var newLabel = statusLabels.ElementAtOrDefault((int)newStatus) ?? newStatus.ToString();

            var body = $@"
            <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
              <div style='background:#10b981;color:white;padding:20px;border-radius:8px 8px 0 0;'>
                <h2 style='margin:0;'>✅ Görev Durumu Güncellendi</h2>
              </div>
              <div style='background:#f8fafc;padding:20px;border:1px solid #e2e8f0;'>
                <p>Merhaba <strong>{{{{NAME}}}}</strong>,</p>
                <p><strong>{_currentUser.FullName}</strong> görev durumunu güncelledi.</p>
                <div style='background:white;padding:15px;border-radius:8px;border-left:4px solid #10b981;margin:15px 0;'>
                  <p style='margin:0;'><strong>Görev:</strong> {task.Title}</p>
                  <p style='margin:5px 0 0;'><strong>Yeni Durum:</strong> {newLabel}</p>
                </div>
              </div>
              <div style='background:#e2e8f0;padding:10px;text-align:center;border-radius:0 0 8px 8px;font-size:12px;color:#64748b;'>Tezgah</div>
            </div>";

            await NotifyTaskParticipants(task,
                $"[Tezgah] Durum Değişti: {task.Title}", body, _currentUser.UserId);
        }

        return Json(new { success = true });
    }

    // ─── HELPERS ──────────────────────────────────────────────────────────────
    private async Task PopulateCreateViewModelAsync(CreateTaskViewModel model)
    {
        IEnumerable<Project> projects = _currentUser.IsSuperAdmin
            ? await _projectRepo.GetAllAsync()
            : await _projectRepo.GetAllAsync(_currentUser.GroupId, _currentUser.UserId);

        IEnumerable<AppUser> users;
        if (model.ProjectId > 0)
            users = await _memberRepo.GetProjectMembersAsync(model.ProjectId);
        else if (_currentUser.IsSuperAdmin)
            users = await _userRepo.GetAllAsync();
        else if (_currentUser.GroupId.HasValue)
            users = await _userRepo.GetByGroupAsync(_currentUser.GroupId.Value);
        else
            users = [];

        model.Projects = projects.Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
        model.Users    = users.Select(u => new SelectListItem(u.FullName, u.Id.ToString())).ToList();
    }

    private async Task PopulateEditViewModelAsync(EditTaskViewModel model, int? projectId = null)
    {
        IEnumerable<Project> projects = _currentUser.IsSuperAdmin
            ? await _projectRepo.GetAllAsync()
            : await _projectRepo.GetAllAsync(_currentUser.GroupId, _currentUser.UserId);

        // Edit formunda atanan seçiminde TÜM kullanıcılar gösterilsin
        IEnumerable<AppUser> users = await _userRepo.GetAllAsync();

        model.Projects = projects.Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
        model.Users    = users.Select(u => new SelectListItem(u.FullName, u.Id.ToString())).ToList();
    }
}

public class UpdateStatusRequest
{
    public int TaskId { get; set; }
    public int Status { get; set; }
}
