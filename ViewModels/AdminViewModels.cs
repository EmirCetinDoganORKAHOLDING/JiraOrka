using System.ComponentModel.DataAnnotations;
using Tezgah.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Tezgah.ViewModels;

public class CreateUserViewModel
{
    [Required(ErrorMessage = "Ad Soyad zorunludur")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kullanıcı adı zorunludur")]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    public int? GroupId { get; set; }
    public bool IsActive { get; set; } = true;

    public List<SelectListItem> Groups { get; set; } = new();
}

public class EditUserViewModel
{
    public int Id { get; set; }

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? NewPassword { get; set; }

    public int? GroupId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSuperAdmin { get; set; } = false;

    public List<SelectListItem> Groups { get; set; } = new();
}

public class CreateGroupViewModel
{
    [Required(ErrorMessage = "Grup adı zorunludur")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool CanCreateProjects { get; set; } = true;
    public bool CanManageTasks { get; set; } = true;
    public bool CanViewReports { get; set; } = false;
}

public class CreateProjectViewModel
{
    [Required(ErrorMessage = "Proje adı zorunludur")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Proje anahtarı zorunludur")]
    [StringLength(10, MinimumLength = 2)]
    public string Key { get; set; } = string.Empty;

    // Grup seçimi opsiyonel — bireysel kullanıcı eklenebilir
    public int? GroupId { get; set; }

    // Projeye direkt eklenecek kullanıcı ID'leri
    public List<int> MemberIds { get; set; } = new();

    public List<SelectListItem> Groups { get; set; } = new();
    public List<SelectListItem> Users  { get; set; } = new();
}
