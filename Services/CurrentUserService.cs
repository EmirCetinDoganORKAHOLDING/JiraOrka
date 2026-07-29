using System.Security.Claims;

namespace Tezgah.Services;

public class CurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public int UserId => int.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public string FullName => User?.FindFirstValue("FullName") ?? string.Empty;

    public string Email => User?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public string Username => User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public int? GroupId
    {
        get
        {
            var val = User?.FindFirstValue("GroupId");
            return int.TryParse(val, out var id) ? id : null;
        }
    }

    public bool IsSuperAdmin => User?.FindFirstValue("IsSuperAdmin") == "true";

    public bool IsReadOnly => User?.FindFirstValue("IsReadOnly") == "true";

    /// Hem süper admin hem de salt-okunur kullanıcılar tüm projeleri/görevleri görebilir
    public bool CanViewAll => IsSuperAdmin || IsReadOnly;

    public bool CanCreateProjects => IsSuperAdmin || User?.FindFirstValue("CanCreateProjects") == "true";

    public bool CanManageTasks => IsSuperAdmin || User?.FindFirstValue("CanManageTasks") == "true";

    public bool CanViewReports => IsSuperAdmin || User?.FindFirstValue("CanViewReports") == "true";
}
