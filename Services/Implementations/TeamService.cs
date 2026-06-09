using Microsoft.EntityFrameworkCore;
using AutoMapper;
using TrelloApi.Data;
using TrelloApi.DTOs.Team;
using TrelloApi.Helpers;
using TrelloApi.Models;
using TrelloApi.Repositories.Interfaces;
using TrelloApi.Services.Interfaces;

namespace TrelloApi.Services.Implementations;

public class TeamService : ITeamService
{
    private readonly ITeamRepository _teamRepo;
    private readonly IUserRepository _userRepo;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly IActivityLogService _activityLog;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public TeamService(
        ITeamRepository teamRepo, IUserRepository userRepo,
        IMapper mapper, INotificationService notificationService,
        IActivityLogService activityLog, ApplicationDbContext context,
        IEmailService emailService)
    {
        _teamRepo            = teamRepo;
        _userRepo            = userRepo;
        _mapper              = mapper;
        _notificationService = notificationService;
        _activityLog         = activityLog;
        _context             = context;
        _emailService        = emailService;
    }

    public async Task<ApiResponse<TeamResponseDto>> CreateTeamAsync(CreateTeamDto dto, long ownerId, CancellationToken ct = default)
    {
        var team = _mapper.Map<Team>(dto);
        team.OwnerId = ownerId;

        await _teamRepo.AddAsync(team, ct);
        await _teamRepo.SaveChangesAsync(ct);

        // Auto-add owner as Admin member
        var ownerMembership = new TeamMember { TeamId = team.Id, UserId = ownerId, TeamRole = "Admin" };
        await _teamRepo.GetMembershipAsync(team.Id, ownerId, ct); // Force context awareness
        team.Members.Add(ownerMembership);
        await _teamRepo.SaveChangesAsync(ct);

        var created = await _teamRepo.GetByIdWithMembersAsync(team.Id, ct);
        await _activityLog.LogAsync(ownerId, "TeamCreated", $"Team '{team.Name}' was created", "Team", team.Id, ct: ct);

        return ApiResponse<TeamResponseDto>.Ok(_mapper.Map<TeamResponseDto>(created!), "Team created successfully.");
    }

    public async Task<ApiResponse<IEnumerable<TeamResponseDto>>> GetMyTeamsAsync(long userId, CancellationToken ct = default)
    {
        var teams = await _teamRepo.GetTeamsByUserIdAsync(userId, ct);
        return ApiResponse<IEnumerable<TeamResponseDto>>.Ok(_mapper.Map<IEnumerable<TeamResponseDto>>(teams));
    }

    public async Task<ApiResponse<TeamResponseDto>> GetTeamByIdAsync(long teamId, long requestingUserId, CancellationToken ct = default)
    {
        var team = await _teamRepo.GetByIdWithMembersAsync(teamId, ct);
        if (team is null) return ApiResponse<TeamResponseDto>.Fail("Team not found.");

        // Business Rule: Only members can view team details
        if (!await _teamRepo.IsUserMemberAsync(teamId, requestingUserId, ct))
            return ApiResponse<TeamResponseDto>.Fail("You are not a member of this team.");

        return ApiResponse<TeamResponseDto>.Ok(_mapper.Map<TeamResponseDto>(team));
    }

    public async Task<ApiResponse<TeamResponseDto>> UpdateTeamAsync(long teamId, UpdateTeamDto dto, long requestingUserId, CancellationToken ct = default)
    {
        var team = await _teamRepo.GetByIdWithMembersAsync(teamId, ct);
        if (team is null) return ApiResponse<TeamResponseDto>.Fail("Team not found.");

        // Business Rule: Only team owner or admin can update
        if (team.OwnerId != requestingUserId && !await _teamRepo.IsUserAdminAsync(teamId, requestingUserId, ct))
            return ApiResponse<TeamResponseDto>.Fail("Only team admins can update team details.");

        _mapper.Map(dto, team);
        _teamRepo.Update(team);
        await _teamRepo.SaveChangesAsync(ct);

        return ApiResponse<TeamResponseDto>.Ok(_mapper.Map<TeamResponseDto>(team));
    }

    public async Task<ApiResponse> DeleteTeamAsync(long teamId, long requestingUserId, CancellationToken ct = default)
    {
        var team = await _teamRepo.GetByIdAsync(teamId, ct);
        if (team is null) return ApiResponse.Fail("Team not found.");

        // Business Rule: Only owner can delete the team
        if (team.OwnerId != requestingUserId)
            return ApiResponse.Fail("Only the team owner can delete the team.");

        team.IsDeleted = true;
        _teamRepo.Update(team);
        await _teamRepo.SaveChangesAsync(ct);

        await _activityLog.LogAsync(requestingUserId, "TeamDeleted", $"Team {teamId} was deleted", "Team", teamId, ct: ct);
        return ApiResponse.Ok("Team deleted successfully.");
    }

    public async Task<ApiResponse<TeamMemberResponseDto>> AddMemberAsync(long teamId, AddTeamMemberDto dto, long requestingUserId, CancellationToken ct = default)
    {
        var team = await _teamRepo.GetByIdWithMembersAsync(teamId, ct);
        if (team is null) return ApiResponse<TeamMemberResponseDto>.Fail("Team not found.");

        // Business Rule: Only team admin can add members
        if (team.OwnerId != requestingUserId && !await _teamRepo.IsUserAdminAsync(teamId, requestingUserId, ct))
            return ApiResponse<TeamMemberResponseDto>.Fail("Only team admins can add members.");

        // Business Rule: User must exist
        var userToAdd = await _userRepo.GetByIdWithRoleAsync(dto.UserId, ct);
        if (userToAdd is null) return ApiResponse<TeamMemberResponseDto>.Fail("User not found.");

        // Business Rule: User cannot be added twice
        if (await _teamRepo.IsUserMemberAsync(teamId, dto.UserId, ct))
            return ApiResponse<TeamMemberResponseDto>.Fail("User is already a member of this team.");

        var membership = new TeamMember { TeamId = teamId, UserId = dto.UserId, TeamRole = dto.TeamRole };
        team.Members.Add(membership);
        await _teamRepo.SaveChangesAsync(ct);

        // Send notification to invited user
        await _notificationService.CreateNotificationAsync(
            dto.UserId, "TeamInvitation",
            $"You've been added to team: {team.Name}",
            entityType: "Team", entityId: teamId, sentByUserId: requestingUserId);

        membership.User = userToAdd;
        return ApiResponse<TeamMemberResponseDto>.Ok(_mapper.Map<TeamMemberResponseDto>(membership), "Member added.");
    }

    public async Task<ApiResponse> RemoveMemberAsync(long teamId, long userId, long requestingUserId, CancellationToken ct = default)
    {
        var team = await _teamRepo.GetByIdAsync(teamId, ct);
        if (team is null) return ApiResponse.Fail("Team not found.");

        // Business Rule: Only team admin can remove members
        if (team.OwnerId != requestingUserId && !await _teamRepo.IsUserAdminAsync(teamId, requestingUserId, ct))
            return ApiResponse.Fail("Only team admins can remove members.");

        // Business Rule: Cannot remove the owner
        if (userId == team.OwnerId)
            return ApiResponse.Fail("The team owner cannot be removed.");

        var membership = await _teamRepo.GetMembershipAsync(teamId, userId, ct);
        if (membership is null) return ApiResponse.Fail("User is not a member of this team.");

        _context.TeamMembers.Remove(membership);
        await _context.SaveChangesAsync(ct);

        return ApiResponse.Ok("Member removed successfully.");
    }

    public async Task<ApiResponse<IEnumerable<TeamMemberResponseDto>>> GetMembersAsync(long teamId, long requestingUserId, CancellationToken ct = default)
    {
        if (!await _teamRepo.IsUserMemberAsync(teamId, requestingUserId, ct))
            return ApiResponse<IEnumerable<TeamMemberResponseDto>>.Fail("You are not a member of this team.");

        var team = await _teamRepo.GetByIdWithMembersAsync(teamId, ct);
        if (team is null) return ApiResponse<IEnumerable<TeamMemberResponseDto>>.Fail("Team not found.");

        return ApiResponse<IEnumerable<TeamMemberResponseDto>>.Ok(
            _mapper.Map<IEnumerable<TeamMemberResponseDto>>(team.Members));
    }

    public async Task<ApiResponse> InviteUserByEmailAsync(long teamId, string email, long requestingUserId, CancellationToken ct = default)
    {
        var team = await _teamRepo.GetByIdAsync(teamId, ct);
        if (team is null) return ApiResponse.Fail("Team not found.");

        if (team.OwnerId != requestingUserId && !await _teamRepo.IsUserAdminAsync(teamId, requestingUserId, ct))
            return ApiResponse.Fail("Only team admins can invite members.");

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (existingUser != null && await _teamRepo.IsUserMemberAsync(teamId, existingUser.Id, ct))
            return ApiResponse.Fail("User is already a member of this team.");

        var invitation = new TeamInvitation
        {
            TeamId = teamId,
            Email = email,
            InvitedByUserId = requestingUserId,
            Token = Guid.NewGuid().ToString()
        };

        _context.TeamInvitations.Add(invitation);
        await _context.SaveChangesAsync(ct);

        var inviteUrl = $"http://localhost:4200/accept-invite?token={invitation.Token}";
        var htmlBody = $"<h3>You have been invited to join team: {team.Name}</h3><p>Click <a href='{inviteUrl}'>here</a> to accept the invitation and join the workspace.</p>";

        await _emailService.SendEmailAsync(email, $"Invitation to join {team.Name}", htmlBody);

        return ApiResponse.Ok("Invitation sent successfully.");
    }

    public async Task<ApiResponse> AcceptInvitationAsync(string token, long userId, CancellationToken ct = default)
    {
        var invitation = await _context.TeamInvitations
            .Include(i => i.Team)
            .FirstOrDefaultAsync(i => i.Token == token, ct);

        if (invitation is null) return ApiResponse.Fail("Invalid invitation token.");
        if (invitation.IsAccepted) return ApiResponse.Fail("Invitation has already been accepted.");
        if (invitation.ExpiresAt < DateTime.UtcNow) return ApiResponse.Fail("Invitation has expired.");

        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null) return ApiResponse.Fail("User not found.");

        if (user.Email.ToLower() != invitation.Email.ToLower())
            return ApiResponse.Fail("This invitation was sent to a different email address.");

        if (await _teamRepo.IsUserMemberAsync(invitation.TeamId, userId, ct))
        {
            invitation.IsAccepted = true;
            await _context.SaveChangesAsync(ct);
            return ApiResponse.Ok("You are already a member of this team.");
        }

        var membership = new TeamMember { TeamId = invitation.TeamId, UserId = userId, TeamRole = "Member" };
        _context.TeamMembers.Add(membership);

        invitation.IsAccepted = true;

        await _context.SaveChangesAsync(ct);

        return ApiResponse.Ok("Invitation accepted successfully. You have joined the team.");
    }
}
