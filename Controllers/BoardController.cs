using Tezgah.Models;
using Tezgah.Repositories;
using Tezgah.Services;
using Tezgah.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskStatus = Tezgah.Models.TaskStatus;

namespace Tezgah.Controllers;

[Authorize]
public class BoardController : Controller
{
    private readonly TaskRepository _taskRepo;
    private readonly ProjectRepository _projectRepo;
    private readonly ProjectMemberRepository _memberRepo;
    private readonly CurrentUserService _currentUser;

    public BoardController(TaskRepository taskRepo, ProjectRepository projectRepo,
        ProjectMemberRepository memberRepo, CurrentUserService currentUser)
    {
        _memberRepo = memberRepo;
        _taskRepo = taskRepo;
        _projectRepo = projectRepo;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(int? projectId = null)
    {
        IEnumerable<Project> projects;

        if (_currentUser.CanViewAll)
            projects = await _projectRepo.GetAllAsync();
        else
            projects = await _projectRepo.GetAllAsync(_currentUser.GroupId, _currentUser.UserId);

        var projectList = projects.ToList();

        if (!projectList.Any())
        {
            return View(new BoardViewModel { Projects = projectList });
        }

        var selectedProject = projectId.HasValue
            ? projectList.FirstOrDefault(p => p.Id == projectId)
            : projectList.First();

        if (selectedProject == null)
            return View(new BoardViewModel { Projects = projectList });

        if (!_currentUser.CanViewAll
            && selectedProject.GroupId != _currentUser.GroupId
            && !await _memberRepo.HasAccessAsync(selectedProject.Id, _currentUser.UserId))
            return Forbid();

        var tasks = (await _taskRepo.GetByProjectAsync(selectedProject.Id)).ToList();

        var vm = new BoardViewModel
        {
            ProjectId   = selectedProject.Id,
            ProjectName = selectedProject.Name,
            ProjectKey  = selectedProject.Key ?? selectedProject.Name.Substring(0, Math.Min(3, selectedProject.Name.Length)).ToUpper(),
            Projects    = projectList,
            TodoTasks = tasks.Where(t => t.Status == TaskStatus.Todo).ToList(),
            InProgressTasks = tasks.Where(t => t.Status == TaskStatus.InProgress).ToList(),
            InReviewTasks = tasks.Where(t => t.Status == TaskStatus.InReview).ToList(),
            DoneTasks = tasks.Where(t => t.Status == TaskStatus.Done).ToList()
        };

        return View(vm);
    }
}
