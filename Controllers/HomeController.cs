using Tezgah.Repositories;
using Tezgah.Services;
using Tezgah.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Tezgah.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly TaskRepository _taskRepo;
    private readonly ProjectRepository _projectRepo;
    private readonly UserRepository _userRepo;
    private readonly CurrentUserService _currentUser;

    public HomeController(TaskRepository taskRepo, ProjectRepository projectRepo,
        UserRepository userRepo, CurrentUserService currentUser)
    {
        _taskRepo = taskRepo;
        _projectRepo = projectRepo;
        _userRepo = userRepo;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index()
    {
        IEnumerable<Tezgah.Models.TaskItem> myTasks;
        IEnumerable<Tezgah.Models.Project> projects;

        if (_currentUser.CanViewAll)
        {
            myTasks = await _taskRepo.GetAllAsync();
            projects = await _projectRepo.GetAllAsync();
        }
        else
        {
            myTasks = await _taskRepo.GetAssignedToUserAsync(_currentUser.UserId);
            projects = await _projectRepo.GetAllAsync(_currentUser.GroupId, _currentUser.UserId);
        }

        var taskList = myTasks.ToList();
        var projectList = projects.ToList();

        ViewBag.MyTasks = taskList;
        ViewBag.Projects = projectList;
        ViewBag.TotalTasks = taskList.Count;
        ViewBag.OpenTasks = taskList.Count(t => t.Status != Tezgah.Models.TaskStatus.Done);
        ViewBag.DoneTasks = taskList.Count(t => t.Status == Tezgah.Models.TaskStatus.Done);
        ViewBag.TotalProjects = projectList.Count;

        if (_currentUser.CanViewAll)
            ViewBag.AllUsers = (await _userRepo.GetAllAsync()).ToList();

        return View();
    }

    [AllowAnonymous]
    public IActionResult Error()
    {
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        ViewBag.ErrorMessage = exceptionFeature?.Error?.Message;
        ViewBag.ErrorPath    = exceptionFeature?.Path;
        ViewBag.StackTrace   = exceptionFeature?.Error?.ToString();
        return View();
    }
}
