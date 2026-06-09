using AutoMapper;
using TrelloApi.DTOs.Project;
using TrelloApi.Helpers;
using TrelloApi.Models;
using TrelloApi.Repositories.Interfaces;
using TrelloApi.Services.Interfaces;

namespace TrelloApi.Services.Implementations;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepo;
    private readonly ITeamRepository _teamRepo;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly IActivityLogService _activityLog;

    public ProjectService(
        IProjectRepository projectRepo, ITeamRepository teamRepo,
        IMapper mapper, INotificationService notificationService,
        IActivityLogService activityLog)
    {
        _projectRepo         = projectRepo;
        _teamRepo            = teamRepo;
        _mapper              = mapper;
        _notificationService = notificationService;
        _activityLog         = activityLog;
    }

    public async Task<ApiResponse<ProjectResponseDto>> CreateProjectAsync(
        CreateProjectDto dto, long ownerId, CancellationToken ct = default)
    {
        // Business Rule: If team specified, user must be a team member
        if (dto.TeamId.HasValue && !await _teamRepo.IsUserMemberAsync(dto.TeamId.Value, ownerId, ct))
            return ApiResponse<ProjectResponseDto>.Fail("You must be a team member to create a project under that team.");

        var project = _mapper.Map<Project>(dto);
        project.OwnerId = ownerId;

        await _projectRepo.AddAsync(project, ct);
        await _projectRepo.SaveChangesAsync(ct);

        // Auto-add creator as Owner member
        var ownerMembership = new ProjectMember
            { ProjectId = project.Id, UserId = ownerId, ProjectRole = "Owner" };
        project.Members.Add(ownerMembership);
        await _projectRepo.SaveChangesAsync(ct);

        var created = await _projectRepo.GetByIdWithDetailsAsync(project.Id, ct);
        await _activityLog.LogAsync(ownerId, "ProjectCreated", $"Project '{project.Name}' created", "Project", project.Id, ct: ct);

        return ApiResponse<ProjectResponseDto>.Ok(_mapper.Map<ProjectResponseDto>(created!), "Project created.");
    }

    public async Task<ApiResponse<IEnumerable<ProjectResponseDto>>> GetMyProjectsAsync(long userId, CancellationToken ct = default)
    {
        var projects = await _projectRepo.GetProjectsByUserIdAsync(userId, ct);
        return ApiResponse<IEnumerable<ProjectResponseDto>>.Ok(_mapper.Map<IEnumerable<ProjectResponseDto>>(projects));
    }

    public async Task<ApiResponse<ProjectResponseDto>> GetProjectByIdAsync(long projectId, long requestingUserId, CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdWithDetailsAsync(projectId, ct);
        if (project is null) return ApiResponse<ProjectResponseDto>.Fail("Project not found.");

        // Business Rule: Only project members can access project details
        if (!await _projectRepo.IsUserMemberAsync(projectId, requestingUserId, ct))
            return ApiResponse<ProjectResponseDto>.Fail("You are not a member of this project.");

        return ApiResponse<ProjectResponseDto>.Ok(_mapper.Map<ProjectResponseDto>(project));
    }

    public async Task<ApiResponse<ProjectResponseDto>> UpdateProjectAsync(
        long projectId, UpdateProjectDto dto, long requestingUserId, CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdWithDetailsAsync(projectId, ct);
        if (project is null) return ApiResponse<ProjectResponseDto>.Fail("Project not found.");

        // Business Rule: Only owner can update project
        if (project.OwnerId != requestingUserId)
            return ApiResponse<ProjectResponseDto>.Fail("Only the project owner can update this project.");

        _mapper.Map(dto, project);
        _projectRepo.Update(project);
        await _projectRepo.SaveChangesAsync(ct);

        // Notify all project members about the update
        foreach (var member in project.Members.Where(m => m.UserId != requestingUserId))
        {
            await _notificationService.CreateNotificationAsync(
                member.UserId, "ProjectUpdated",
                $"Project '{project.Name}' was updated",
                entityType: "Project", entityId: projectId, sentByUserId: requestingUserId);
        }

        await _activityLog.LogAsync(requestingUserId, "ProjectUpdated", $"Project {projectId} updated", "Project", projectId, ct: ct);
        return ApiResponse<ProjectResponseDto>.Ok(_mapper.Map<ProjectResponseDto>(project));
    }

    public async Task<ApiResponse> DeleteProjectAsync(long projectId, long requestingUserId, CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, ct);
        if (project is null) return ApiResponse.Fail("Project not found.");

        if (project.OwnerId != requestingUserId)
            return ApiResponse.Fail("Only the project owner can delete this project.");

        project.IsDeleted = true;
        _projectRepo.Update(project);
        await _projectRepo.SaveChangesAsync(ct);

        await _activityLog.LogAsync(requestingUserId, "ProjectDeleted", $"Project {projectId} deleted", "Project", projectId, ct: ct);
        return ApiResponse.Ok("Project deleted successfully.");
    }

    public async Task<ApiResponse<ProjectResponseDto>> UpdateProjectColumnsAsync(long projectId, string[] columns, long requestingUserId, CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdWithDetailsAsync(projectId, ct);
        if (project is null) return ApiResponse<ProjectResponseDto>.Fail("Project not found.");

        // Business Rule: Any project member can update columns
        if (!await _projectRepo.IsUserMemberAsync(projectId, requestingUserId, ct))
            return ApiResponse<ProjectResponseDto>.Fail("Only project members can update project columns.");

        project.Columns = columns;
        _projectRepo.Update(project);
        await _projectRepo.SaveChangesAsync(ct);

        await _activityLog.LogAsync(requestingUserId, "ProjectColumnsUpdated", $"Project {projectId} columns updated", "Project", projectId, ct: ct);
        return ApiResponse<ProjectResponseDto>.Ok(_mapper.Map<ProjectResponseDto>(project));
    }
}
