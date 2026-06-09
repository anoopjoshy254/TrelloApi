using AutoMapper;
using TrelloApi.DTOs.Auth;
using TrelloApi.DTOs.User;
using TrelloApi.Helpers;
using TrelloApi.Models;
using TrelloApi.Repositories.Interfaces;
using TrelloApi.Services.Interfaces;

namespace TrelloApi.Services.Implementations;

/// <summary>
/// Handles all authentication operations including registration, login,
/// JWT token generation, refresh tokens, and password management.
///
/// WORKFLOW:
/// Register → hash password → save user → return JWT
/// Login    → verify password → generate access + refresh token → save refresh → return JWT
/// Refresh  → validate refresh token → issue new tokens
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IMapper _mapper;
    private readonly JwtHelper _jwtHelper;
    private readonly IActivityLogService _activityLog;
    private readonly IConfiguration _config;

    public AuthService(
        IUserRepository userRepo,
        IMapper mapper,
        JwtHelper jwtHelper,
        IActivityLogService activityLog,
        IConfiguration config)
    {
        _userRepo    = userRepo;
        _mapper      = mapper;
        _jwtHelper   = jwtHelper;
        _activityLog = activityLog;
        _config      = config;
    }

    public async Task<ApiResponse<JwtResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        // Business Rule: Email must be unique
        if (await _userRepo.AnyAsync(u => u.Email == dto.Email, ct))
            return ApiResponse<JwtResponseDto>.Fail("Email is already registered.");

        var user = _mapper.Map<User>(dto);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        user.RoleId = 2; // Default: Member role

        await _userRepo.AddAsync(user, ct);
        await _userRepo.SaveChangesAsync(ct);

        // Reload with role for JWT
        var saved = await _userRepo.GetByIdWithRoleAsync(user.Id, ct);
        await _activityLog.LogAsync(user.Id, "UserRegistered", $"New user registered: {user.Email}", ct: ct);

        return BuildJwtResponse(saved!);
    }

    public async Task<ApiResponse<JwtResponseDto>> LoginAsync(LoginDto dto, string? ipAddress = null, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByEmailAsync(dto.Email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return ApiResponse<JwtResponseDto>.Fail("Invalid email or password.");

        if (!user.IsActive)
            return ApiResponse<JwtResponseDto>.Fail("Your account has been deactivated.");

        // Issue tokens
        user.RefreshToken = _jwtHelper.GenerateRefreshToken();
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(
            _config.GetValue<int>("JwtSettings:RefreshTokenExpiryDays"));
        user.LastLoginAt = DateTime.UtcNow;

        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync(ct);

        await _activityLog.LogAsync(user.Id, "Login", "User logged in", ipAddress: ipAddress, ct: ct);
        return BuildJwtResponse(user);
    }

    public async Task<ApiResponse> LogoutAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null) return ApiResponse.Fail("User not found.");

        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync(ct);

        await _activityLog.LogAsync(userId, "Logout", "User logged out", ct: ct);
        return ApiResponse.Ok("Logged out successfully.");
    }

    public async Task<ApiResponse<JwtResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByRefreshTokenAsync(refreshToken, ct);

        if (user is null || user.RefreshTokenExpiry < DateTime.UtcNow)
            return ApiResponse<JwtResponseDto>.Fail("Invalid or expired refresh token.");

        user.RefreshToken = _jwtHelper.GenerateRefreshToken();
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(
            _config.GetValue<int>("JwtSettings:RefreshTokenExpiryDays"));

        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync(ct);

        return BuildJwtResponse(user);
    }

    public async Task<ApiResponse> ChangePasswordAsync(long userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null) return ApiResponse.Fail("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return ApiResponse.Fail("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync(ct);

        await _activityLog.LogAsync(userId, "PasswordChanged", "User changed their password", ct: ct);
        return ApiResponse.Ok("Password changed successfully.");
    }

    public async Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByEmailAsync(dto.Email, ct);
        // Always return OK to prevent email enumeration
        if (user is null) return ApiResponse.Ok("If that email exists, a reset link has been sent.");

        user.PasswordResetToken = Guid.NewGuid().ToString("N");
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync(ct);

        // TODO: Send email with reset token (email service integration)
        // e.g., await _emailService.SendPasswordResetAsync(user.Email, user.PasswordResetToken);

        await _activityLog.LogAsync(user.Id, "ForgotPassword", "Password reset requested", ct: ct);
        return ApiResponse.Ok("If that email exists, a reset link has been sent.");
    }

    public async Task<ApiResponse> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByResetTokenAsync(dto.Token, ct);

        if (user is null || user.Email != dto.Email)
            return ApiResponse.Fail("Invalid reset token.");

        if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
            return ApiResponse.Fail("Reset token has expired. Please request a new one.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync(ct);

        await _activityLog.LogAsync(user.Id, "PasswordReset", "Password was reset via token", ct: ct);
        return ApiResponse.Ok("Password has been reset successfully.");
    }

    // ─────────────────────────────────────────
    // Private: Build JWT response DTO
    // ─────────────────────────────────────────
    private ApiResponse<JwtResponseDto> BuildJwtResponse(User user)
    {
        var accessToken = _jwtHelper.GenerateAccessToken(user);
        var expiryMinutes = _config.GetValue<int>("JwtSettings:ExpiryMinutes");

        var response = new JwtResponseDto
        {
            AccessToken  = accessToken,
            RefreshToken = user.RefreshToken ?? string.Empty,
            ExpiresAt    = DateTime.UtcNow.AddMinutes(expiryMinutes),
            User         = _mapper.Map<UserAuthInfoDto>(user)
        };

        return ApiResponse<JwtResponseDto>.Ok(response, "Authentication successful.");
    }
}
