using AutoMapper;
using TrelloApi.DTOs.User;
using TrelloApi.Helpers;
using TrelloApi.Repositories.Interfaces;
using TrelloApi.Services.Interfaces;

namespace TrelloApi.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IMapper _mapper;
    private readonly IActivityLogService _activityLog;

    public UserService(IUserRepository userRepo, IMapper mapper, IActivityLogService activityLog)
    {
        _userRepo    = userRepo;
        _mapper      = mapper;
        _activityLog = activityLog;
    }

    public async Task<ApiResponse<PagedResult<UserResponseDto>>> GetUsersAsync(
        int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        var (items, total) = await _userRepo.GetPagedAsync(page, pageSize, search, ct);
        var result = new PagedResult<UserResponseDto>
        {
            Items      = _mapper.Map<List<UserResponseDto>>(items),
            TotalCount = total,
            PageNumber = page,
            PageSize   = pageSize
        };
        return ApiResponse<PagedResult<UserResponseDto>>.Ok(result);
    }

    public async Task<ApiResponse<UserResponseDto>> GetUserByIdAsync(long id, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdWithRoleAsync(id, ct);
        if (user is null) return ApiResponse<UserResponseDto>.Fail("User not found.");
        return ApiResponse<UserResponseDto>.Ok(_mapper.Map<UserResponseDto>(user));
    }

    public async Task<ApiResponse<UserResponseDto>> UpdateUserAsync(long userId, UpdateUserDto dto, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdWithRoleAsync(userId, ct);
        if (user is null) return ApiResponse<UserResponseDto>.Fail("User not found.");

        _mapper.Map(dto, user);
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync(ct);

        await _activityLog.LogAsync(userId, "ProfileUpdated", "User profile was updated", ct: ct);
        return ApiResponse<UserResponseDto>.Ok(_mapper.Map<UserResponseDto>(user));
    }

    public async Task<ApiResponse> DeleteUserAsync(long userId, long requestingUserId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null) return ApiResponse.Fail("User not found.");

        // Soft delete
        user.IsDeleted = true;
        user.IsActive  = false;
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync(ct);

        await _activityLog.LogAsync(requestingUserId, "UserDeleted", $"User {userId} was deleted", ct: ct);
        return ApiResponse.Ok("User deleted successfully.");
    }
}
