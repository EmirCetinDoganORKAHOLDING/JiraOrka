using System.ComponentModel.DataAnnotations;

namespace Tezgah.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir email girin")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Ad Soyad zorunludur")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kullanıcı adı zorunludur")]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir email girin")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Compare("NewPassword")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
