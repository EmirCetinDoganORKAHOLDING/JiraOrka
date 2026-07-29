using Tezgah.Models;
using Tezgah.Repositories;
using Tezgah.Services;
using Tezgah.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Tezgah.Controllers;

[Authorize]
public class ProjectsController : Controller
{
    private readonly ProjectRepository _projectRepo;
    private readonly GroupRepository _groupRepo;
    private readonly TaskRepository _taskRepo;
    private readonly ProjectMemberRepository _memberRepo;
    private readonly UserRepository _userRepo;
    private readonly CurrentUserService _currentUser;

    public ProjectsController(ProjectRepository projectRepo, GroupRepository groupRepo,
        TaskRepository taskRepo, ProjectMemberRepository memberRepo,
        UserRepository userRepo, CurrentUserService currentUser)
    {
        _projectRepo = projectRepo;
        _groupRepo = groupRepo;
        _taskRepo = taskRepo;
        _memberRepo = memberRepo;
        _userRepo = userRepo;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index()
    {
        IEnumerable<Project> projects;

        if (_currentUser.IsSuperAdmin)
            projects = await _projectRepo.GetAllAsync();
        else
            projects = await _projectRepo.GetAllAsync(_currentUser.GroupId, _currentUser.UserId);

        return View(projects);
    }

    public async Task<IActionResult> Details(int id)
    {
        var project = await _projectRepo.GetByIdAsync(id);
        if (project == null) return NotFound();

        var hasAccess = _currentUser.CanViewAll
            || project.GroupId == _currentUser.GroupId
            || await _memberRepo.HasAccessAsync(id, _currentUser.UserId);
        if (!hasAccess) return Forbid();

        var tasks = await _taskRepo.GetByProjectAsync(id);
        ViewBag.Tasks = tasks;

        // Üye yönetimi için
        ViewBag.DirectMembers = await _memberRepo.GetDirectMembersAsync(id);
        if (_currentUser.IsSuperAdmin)
            ViewBag.AllUsers = await _userRepo.GetAllAsync();

        return View(project);
    }

    // ===== PROJE ÜYE YÖNETİMİ =====

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SuperAdmin")]
    [Route("Projects/AddMember/{projectId}")]
    public async Task<IActionResult> AddMember(int projectId, int userId)
    {
        await _memberRepo.AddMemberAsync(projectId, userId);
        TempData["Success"] = "Kullanıcı projeye eklendi.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SuperAdmin")]
    [Route("Projects/RemoveMember/{projectId}")]
    public async Task<IActionResult> RemoveMember(int projectId, int userId)
    {
        await _memberRepo.RemoveMemberAsync(projectId, userId);
        TempData["Success"] = "Kullanıcı projeden çıkarıldı.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpGet]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Create()
    {
        var groups = await _groupRepo.GetAllAsync();
        var users  = await _userRepo.GetAllAsync();
        return View(new CreateProjectViewModel
        {
            Groups = groups.Select(g => new SelectListItem(g.Name, g.Id.ToString())).ToList(),
            Users  = users.Select(u => new SelectListItem(u.FullName, u.Id.ToString())).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Create(CreateProjectViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var groups = await _groupRepo.GetAllAsync();
            var users  = await _userRepo.GetAllAsync();
            model.Groups = groups.Select(g => new SelectListItem(g.Name, g.Id.ToString())).ToList();
            model.Users  = users.Select(u => new SelectListItem(u.FullName, u.Id.ToString())).ToList();
            return View(model);
        }

        var project = new Project
        {
            Name = model.Name,
            Description = model.Description,
            Key = model.Key.ToUpper(),
            GroupId = model.GroupId,
            CreatedById = _currentUser.UserId
        };

        var id = await _projectRepo.CreateAsync(project);

        // Seçilen kullanıcıları projeye üye olarak ekle
        foreach (var userId in model.MemberIds)
            await _memberRepo.AddMemberAsync(id, userId);

        TempData["Success"] = "Proje oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Edit(int id)
    {
        var project = await _projectRepo.GetByIdAsync(id);
        if (project == null) return NotFound();

        var groups         = await _groupRepo.GetAllAsync();
        var users          = await _userRepo.GetAllAsync();
        var currentMembers = (await _memberRepo.GetDirectMembersAsync(id)).Select(u => u.Id).ToList();

        return View(new CreateProjectViewModel
        {
            Name        = project.Name,
            Description = project.Description,
            Key         = project.Key ?? "",
            GroupId     = project.GroupId,
            MemberIds   = currentMembers,
            Groups      = groups.Select(g => new SelectListItem(g.Name, g.Id.ToString())).ToList(),
            Users       = users.Select(u => new SelectListItem(u.FullName, u.Id.ToString())).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Edit(int id, CreateProjectViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var groups = await _groupRepo.GetAllAsync();
            var users  = await _userRepo.GetAllAsync();
            model.Groups = groups.Select(g => new SelectListItem(g.Name, g.Id.ToString())).ToList();
            model.Users  = users.Select(u => new SelectListItem(u.FullName, u.Id.ToString())).ToList();
            return View(model);
        }

        var project = await _projectRepo.GetByIdAsync(id);
        if (project == null) return NotFound();

        project.Name        = model.Name;
        project.Description = model.Description;
        project.Key         = model.Key.ToUpper();
        project.GroupId     = model.GroupId;

        await _projectRepo.UpdateAsync(project);

        // Üyelik senkronizasyonu: mevcut üyeler ile yeni seçim karşılaştırılır
        var existingMembers = (await _memberRepo.GetDirectMembersAsync(id)).Select(u => u.Id).ToHashSet();
        var newMembers      = model.MemberIds.ToHashSet();

        foreach (var uid in newMembers.Except(existingMembers))
            await _memberRepo.AddMemberAsync(id, uid);

        foreach (var uid in existingMembers.Except(newMembers))
            await _memberRepo.RemoveMemberAsync(id, uid);

        TempData["Success"] = "Proje güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SuperAdmin")]
    [Route("Projects/Delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _projectRepo.DeleteAsync(id);
        TempData["Success"] = "Proje silindi.";
        return RedirectToAction(nameof(Index));
    }
}
