namespace TrelloApi.DTOs.Auth;

/// <summary>
/// Used when a new user creates an account.
/// </summary>
public class RegisterDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Used when a user logs in with email/password.
/// </summary>
public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Used to initiate a password reset via email.
/// </summary>
public class ForgotPasswordDto
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Used to set a new password using a reset token.
/// </summary>
public class ResetPasswordDto
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Used by an authenticated user to change their own password.
/// </summary>
public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Returned to the client after successful login or token refresh.
/// Contains the JWT access token and a refresh token.
/// </summary>
public class JwtResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserAuthInfoDto User { get; set; } = new();
}

/// <summary>
/// Minimal user identity embedded inside JwtResponseDto.
/// </summary>
public class UserAuthInfoDto
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// Used to get a new access token using an existing refresh token.
/// </summary>
public class RefreshTokenDto
{
    public string RefreshToken { get; set; } = string.Empty;
}
