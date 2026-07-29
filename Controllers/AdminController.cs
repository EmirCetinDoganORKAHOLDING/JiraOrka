using Tezgah.Models;
using Tezgah.Repositories;
using Tezgah.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Tezgah.Controllers;

[Authorize(Policy = "SuperAdmin")]
public class AdminController : Controller
{
    private readonly UserRepository _userRepo;
    private readonly GroupRepository _groupRepo;
    private readonly ProjectRepository _projectRepo;
    private readonly TaskRepository _taskRepo;
    private readonly ProjectMemberRepository _memberRepo;

    public AdminController(UserRepository userRepo, GroupRepository groupRepo,
        ProjectRepository projectRepo, TaskRepository taskRepo,
        ProjectMemberRepository memberRepo)
    {
        _memberRepo = memberRepo;
        _userRepo = userRepo;
        _groupRepo = groupRepo;
        _projectRepo = projectRepo;
        _taskRepo = taskRepo;
    }

    public async Task<IActionResult> Index()
    {
        var users    = (await _userRepo.GetAllAsync()).ToList();
        var groups   = await _groupRepo.GetAllAsync();
        var projects = await _projectRepo.GetAllAsync();
        var tasks    = (await _taskRepo.GetAllAsync()).ToList();

        var vm = new AdminDashboardViewModel
        {
            TotalUsers   = users.Count,
            TotalGroups  = groups.Count(),
            TotalProjects = projects.Count(),
            TotalTasks   = tasks.Count,
            OpenTasks    = tasks.Count(t => t.Status != Models.TaskStatus.Done),
            DoneTasks    = tasks.Count(t => t.Status == Models.TaskStatus.Done),
            Users        = users
        };

        return View(vm);
    }

    // ===== USERS =====
    public async Task<IActionResult> Users()
    {
        var users = await _userRepo.GetAllAsync();
        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> CreateUser()
    {
        var groups = await _groupRepo.GetAllAsync();
        return View(new CreateUserViewModel
        {
            Groups = groups.Select(g => new SelectListItem(g.Name, g.Id.ToString())).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var groups = await _groupRepo.GetAllAsync();
            model.Groups = groups.Select(g => new SelectListItem(g.Name, g.Id.ToString())).ToList();
            return View(model);
        }

        if (await _userRepo.EmailExistsAsync(model.Email))
        {
            ModelState.AddModelError("Email", "Bu email zaten kullanımda.");
            var groups = await _groupRepo.GetAllAsync();
            model.Groups = groups.Select(g => new SelectListItem(g.Name, g.Id.ToString())).ToList();
            return View(model);
        }

        if (await _userRepo.UsernameExistsAsync(model.Username))
        {
            ModelState.AddModelError("Username", "Bu kullanıcı adı zaten kullanımda.");
            var groups = await _groupRepo.GetAllAsync();
            model.Groups = groups.Select(g => new SelectListItem(g.Name, g.Id.ToString())).ToList();
            return View(model);
        }

        var user = new AppUser
        {
            FullName = model.FullName,
            Username = model.Username,
            Email = model.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            GroupId = model.GroupId,
            IsActive = model.IsActive,
            IsSuperAdmin = false
        };

        await _userRepo.CreateAsync(user);
        TempData["Success"] = "Kullanıcı başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(int id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return NotFound();

        // Orijinal süper admin (Id=1) düzenlenemez — diğerleri düzenlenebilir
        var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        if (user.Id == currentUserId && user.IsSuperAdmin)
        {
            TempData["Error"] = "Kendi hesabınızı bu ekrandan düzenleyemezsiniz.";
            return RedirectToAction(nameof(Users));
        }

        var groups = await _groupRepo.GetAllAsync();
        return View(new EditUserViewModel
        {
            Id          = user.Id,
            FullName    = user.FullName,
            Username    = user.Username,
            Email       = user.Email,
            GroupId     = user.GroupId,
            IsActive    = user.IsActive,
            IsSuperAdmin = user.IsSuperAdmin,
            Groups = groups.Select(g => new SelectListItem(g.Name, g.Id.ToString())).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var groups = await _groupRepo.GetAllAsync();
            model.Groups = groups.Select(g => new SelectListItem(g.Name, g.Id.ToString())).ToList();
            return View(model);
        }

        var user = await _userRepo.GetByIdAsync(model.Id);
        if (user == null) return NotFound();

        user.FullName    = model.FullName;
        user.Username    = model.Username;
        user.Email       = model.Email;
        user.GroupId     = model.GroupId;
        user.IsActive    = model.IsActive;
        user.IsSuperAdmin = model.IsSuperAdmin;

        await _userRepo.UpdateAsync(user);

        if (!string.IsNullOrEmpty(model.NewPassword))
        {
            await _userRepo.UpdatePasswordAsync(user.Id, BCrypt.Net.BCrypt.HashPassword(model.NewPassword));
        }

        TempData["Success"] = "Kullanıcı güncellendi.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Admin/DeleteUser/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        if (id == currentUserId)
        {
            TempData["Error"] = "Kendi hesabınızı silemezsiniz.";
            return RedirectToAction(nameof(Users));
        }

        // Hard delete yerine pasife al — FK kısıtlarını kırmaz
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return NotFound();

        user.IsActive = false;
        await _userRepo.UpdateAsync(user);

        TempData["Success"] = "Kullanıcı pasife alındı.";
        return RedirectToAction(nameof(Users));
    }

    // ===== GROUPS =====
    public async Task<IActionResult> Groups()
    {
        var groups = await _groupRepo.GetAllAsync();
        return View(groups);
    }

    [HttpGet]
    public IActionResult CreateGroup() => View(new CreateGroupViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroup(CreateGroupViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var group = new PermissionGroup
        {
            Name = model.Name,
            Description = model.Description,
            CanCreateProjects = model.CanCreateProjects,
            CanManageTasks = model.CanManageTasks,
            CanViewReports = model.CanViewReports
        };

        await _groupRepo.CreateAsync(group);
        TempData["Success"] = "Yetki grubu oluşturuldu.";
        return RedirectToAction(nameof(Groups));
    }

    [HttpGet]
    public async Task<IActionResult> EditGroup(int id)
    {
        var group = await _groupRepo.GetByIdAsync(id);
        if (group == null) return NotFound();

        return View(new CreateGroupViewModel
        {
            Name = group.Name,
            Description = group.Description,
            CanCreateProjects = group.CanCreateProjects,
            CanManageTasks = group.CanManageTasks,
            CanViewReports = group.CanViewReports
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditGroup(int id, CreateGroupViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var group = await _groupRepo.GetByIdAsync(id);
        if (group == null) return NotFound();

        group.Name = model.Name;
        group.Description = model.Description;
        group.CanCreateProjects = model.CanCreateProjects;
        group.CanManageTasks = model.CanManageTasks;
        group.CanViewReports = model.CanViewReports;

        await _groupRepo.UpdateAsync(group);
        TempData["Success"] = "Yetki grubu güncellendi.";
        return RedirectToAction(nameof(Groups));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Admin/DeleteGroup/{id}")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        await _groupRepo.DeleteAsync(id);
        TempData["Success"] = "Yetki grubu silindi.";
        return RedirectToAction(nameof(Groups));
    }

    // ===== USER DETAIL =====
    public async Task<IActionResult> UserDetail(int id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return NotFound();

        var assigned      = await _taskRepo.GetAllAssignedToUserAsync(id);
        var created       = await _taskRepo.GetCreatedByUserAsync(id);
        var extraProjects = await _memberRepo.GetUserProjectsAsync(id);
        var allProjects   = await _projectRepo.GetAllAsync();

        return View(new UserDetailViewModel
        {
            User          = user,
            AssignedTasks = assigned,
            CreatedTasks  = created,
            ExtraProjects = extraProjects,
            AllProjects   = allProjects
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Admin/AddUserProject/{userId}")]
    public async Task<IActionResult> AddUserProject(int userId, int projectId)
    {
        await _memberRepo.AddMemberAsync(projectId, userId);
        TempData["Success"] = "Proje erişimi eklendi.";
        return RedirectToAction(nameof(UserDetail), new { id = userId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Admin/RemoveUserProject/{userId}")]
    public async Task<IActionResult> RemoveUserProject(int userId, int projectId)
    {
        await _memberRepo.RemoveMemberAsync(projectId, userId);
        TempData["Success"] = "Proje erişimi kaldırıldı.";
        return RedirectToAction(nameof(UserDetail), new { id = userId });
    }

    // ===== USER KANBAN =====
    public async Task<IActionResult> UserKanban(int id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return NotFound();

        var tasks = (await _taskRepo.GetAllAssignedToUserAsync(id)).ToList();

        return View(new UserKanbanViewModel
        {
            User            = user,
            TodoTasks       = tasks.Where(t => t.Status == Models.TaskStatus.Todo).ToList(),
            InProgressTasks = tasks.Where(t => t.Status == Models.TaskStatus.InProgress).ToList(),
            InReviewTasks   = tasks.Where(t => t.Status == Models.TaskStatus.InReview).ToList(),
            DoneTasks       = tasks.Where(t => t.Status == Models.TaskStatus.Done).ToList()
        });
    }

    // ===== TASKS (filtered list) =====
    [HttpGet]
    public async Task<IActionResult> Tasks(
        DateTime? startDate     = null,
        DateTime? endDate       = null,
        string?   requesterName = null,
        int?      assignedToId  = null,
        int?      status        = null,
        int?      priority      = null,
        int?      projectId     = null)
    {
        var tasks    = await _taskRepo.GetFilteredAsync(startDate, endDate,
                            requesterName, assignedToId, status, priority, projectId);
        var users    = await _userRepo.GetAllAsync();
        var projects = await _projectRepo.GetAllAsync();

        var vm = new TaskFilterViewModel
        {
            StartDate     = startDate,
            EndDate       = endDate,
            RequesterName = requesterName,
            AssignedToId  = assignedToId,
            Status        = status,
            Priority      = priority,
            ProjectId     = projectId,
            Tasks    = tasks,
            Users    = users.Select(u => new SelectListItem(u.FullName, u.Id.ToString())).ToList(),
            Projects = projects.Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList()
        };

        return View(vm);
    }
}
