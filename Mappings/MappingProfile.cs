using AutoMapper;
using TrelloApi.DTOs.Auth;
using TrelloApi.DTOs.Attachment;
using TrelloApi.DTOs.Comment;
using TrelloApi.DTOs.Notification;
using TrelloApi.DTOs.Project;
using TrelloApi.DTOs.Task;
using TrelloApi.DTOs.Team;
using TrelloApi.DTOs.User;
using TrelloApi.Models;

namespace TrelloApi.Mappings;

/// <summary>
/// AutoMapper profile that defines all entity ↔ DTO mappings.
/// AutoMapper is registered as a singleton in DI via AddAutoMapper().
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ─────────────────────────────────────────
        // USER
        // ─────────────────────────────────────────
        CreateMap<User, UserResponseDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : string.Empty));

        CreateMap<User, UserAuthInfoDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : string.Empty));

        CreateMap<CreateUserDto, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()); // hashed in service

        CreateMap<UpdateUserDto, User>()
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        // RegisterDto → User (password hashed separately)
        CreateMap<RegisterDto, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.RoleId, opt => opt.MapFrom(_ => 2L)); // Default: Member

        // ─────────────────────────────────────────
        // TEAM
        // ─────────────────────────────────────────
        CreateMap<Team, TeamResponseDto>()
            .ForMember(dest => dest.OwnerName,   opt => opt.MapFrom(src => src.Owner != null ? $"{src.Owner.FirstName} {src.Owner.LastName}" : string.Empty))
            .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.Members != null ? src.Members.Count : 0));

        CreateMap<TeamMember, TeamMemberResponseDto>()
            .ForMember(dest => dest.FullName,    opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : string.Empty))
            .ForMember(dest => dest.Email,       opt => opt.MapFrom(src => src.User != null ? src.User.Email : string.Empty))
            .ForMember(dest => dest.AvatarUrl,   opt => opt.MapFrom(src => src.User != null ? src.User.AvatarUrl : null));

        CreateMap<CreateTeamDto, Team>();
        CreateMap<UpdateTeamDto, Team>()
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        // ─────────────────────────────────────────
        // PROJECT
        // ─────────────────────────────────────────
        CreateMap<Project, ProjectResponseDto>()
            .ForMember(dest => dest.OwnerName,         opt => opt.MapFrom(src => src.Owner != null ? $"{src.Owner.FirstName} {src.Owner.LastName}" : string.Empty))
            .ForMember(dest => dest.TeamName,           opt => opt.MapFrom(src => src.Team != null ? src.Team.Name : null))
            .ForMember(dest => dest.MemberCount,        opt => opt.MapFrom(src => src.Members != null ? src.Members.Count : 0))
            .ForMember(dest => dest.TaskCount,          opt => opt.MapFrom(src => src.Tasks != null ? src.Tasks.Count : 0))
            .ForMember(dest => dest.CompletedTaskCount, opt => opt.MapFrom(src => src.Tasks != null ? src.Tasks.Count(t => t.Status == "Done") : 0));

        CreateMap<CreateProjectDto, Project>();
        CreateMap<UpdateProjectDto, Project>()
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        // ─────────────────────────────────────────
        // TASK
        // ─────────────────────────────────────────
        CreateMap<TaskItem, TaskResponseDto>()
            .ForMember(dest => dest.ProjectName,    opt => opt.MapFrom(src => src.Project != null ? src.Project.Name : string.Empty))
            .ForMember(dest => dest.CreatedByName,  opt => opt.MapFrom(src => src.CreatedByUser != null ? $"{src.CreatedByUser.FirstName} {src.CreatedByUser.LastName}" : string.Empty))
            .ForMember(dest => dest.CommentCount,   opt => opt.MapFrom(src => src.Comments != null ? src.Comments.Count : 0))
            .ForMember(dest => dest.AttachmentCount,opt => opt.MapFrom(src => src.Attachments != null ? src.Attachments.Count : 0))
            .ForMember(dest => dest.Assignees,      opt => opt.MapFrom(src => src.Assignments != null
                ? src.Assignments.Select(a => new TaskAssigneeDto
                  {
                      UserId    = a.UserId,
                      FullName  = a.User != null ? $"{a.User.FirstName} {a.User.LastName}" : string.Empty,
                      AvatarUrl = a.User != null ? a.User.AvatarUrl : null,
                      IsCompleted = a.IsCompleted,
                      CompletedAt = a.CompletedAt
                  }).ToList()
                : new List<TaskAssigneeDto>()))
            .ForMember(dest => dest.Labels, opt => opt.MapFrom(src => src.TaskLabels != null
                ? src.TaskLabels.Select(tl => tl.Label).ToList()
                : new List<Label>()))
            .ForMember(dest => dest.Checklists, opt => opt.MapFrom(src => src.Checklists));

        CreateMap<Label, LabelDto>();
        CreateMap<CreateLabelDto, Label>();

        CreateMap<Checklist, ChecklistDto>();
        CreateMap<CreateChecklistDto, Checklist>();

        CreateMap<ChecklistItem, ChecklistItemDto>();
        CreateMap<CreateChecklistItemDto, ChecklistItem>();
        CreateMap<UpdateChecklistItemDto, ChecklistItem>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<CreateTaskDto, TaskItem>()
            .ForMember(dest => dest.Status,   opt => opt.MapFrom(_ => "Todo"))
            .ForMember(dest => dest.Position, opt => opt.MapFrom(_ => 0));

        CreateMap<UpdateTaskDto, TaskItem>()
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        // ─────────────────────────────────────────
        // COMMENT
        // ─────────────────────────────────────────
        CreateMap<Comment, CommentResponseDto>()
            .ForMember(dest => dest.AuthorName,      opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : string.Empty))
            .ForMember(dest => dest.AuthorAvatarUrl, opt => opt.MapFrom(src => src.User != null ? src.User.AvatarUrl : null));

        CreateMap<CreateCommentDto, Comment>();
        CreateMap<UpdateCommentDto, Comment>()
            .ForMember(dest => dest.IsEdited,  opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        // ─────────────────────────────────────────
        // ATTACHMENT
        // ─────────────────────────────────────────
        CreateMap<Attachment, AttachmentResponseDto>()
            .ForMember(dest => dest.UploadedByName, opt => opt.MapFrom(src => src.UploadedByUser != null ? $"{src.UploadedByUser.FirstName} {src.UploadedByUser.LastName}" : string.Empty))
            .ForMember(dest => dest.DownloadUrl,    opt => opt.MapFrom(src => $"/api/attachments/{src.Id}/download"));

        // ─────────────────────────────────────────
        // NOTIFICATION
        // ─────────────────────────────────────────
        CreateMap<Notification, NotificationResponseDto>();
    }
}
