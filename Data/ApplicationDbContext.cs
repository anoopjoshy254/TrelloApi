using Microsoft.EntityFrameworkCore;
using TrelloApi.Models;

namespace TrelloApi.Data;

/// <summary>
/// Central EF Core DbContext for TrelloApi.
/// Implements Fluent API configuration, soft-delete global filters,
/// composite keys, cascade behavior, and unique constraints.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // ───────────────────────────────────────────────
    // DbSets
    // ───────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<TaskLabel> TaskLabels => Set<TaskLabel>();
    public DbSet<Checklist> Checklists => Set<Checklist>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<TeamInvitation> TeamInvitations => Set<TeamInvitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ═══════════════════════════════════════════
        // GLOBAL QUERY FILTERS (Soft Delete)
        // Entities with IsDeleted flag are filtered
        // automatically from all LINQ queries.
        // ═══════════════════════════════════════════
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Team>().HasQueryFilter(t => !t.IsDeleted);
        modelBuilder.Entity<Project>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<TaskItem>().HasQueryFilter(t => !t.IsDeleted);
        modelBuilder.Entity<Comment>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Attachment>().HasQueryFilter(a => !a.IsDeleted);

        // ═══════════════════════════════════════════
        // ROLE
        // ═══════════════════════════════════════════
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(r => r.Name).IsUnique();
        });

        // ═══════════════════════════════════════════
        // USER
        // ═══════════════════════════════════════════
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.IsDeleted).HasDefaultValue(false);
            entity.Property(u => u.IsActive).HasDefaultValue(true);

            // User -> Role (Many-to-One)
            entity.HasOne(u => u.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(u => u.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(u => u.RoleId);
        });

        // ═══════════════════════════════════════════
        // TEAM
        // ═══════════════════════════════════════════
        modelBuilder.Entity<Team>(entity =>
        {
            entity.ToTable("Teams");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(150);

            // Team -> Owner (User) (Many-to-One)
            entity.HasOne(t => t.Owner)
                  .WithMany(u => u.OwnedTeams)
                  .HasForeignKey(t => t.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(t => t.OwnerId);
        });

        // ═══════════════════════════════════════════
        // TEAM MEMBER (Join Table with extra payload)
        // ═══════════════════════════════════════════
        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.ToTable("TeamMembers");
            entity.HasKey(tm => tm.Id);

            // Unique: a user can only have one role per team
            entity.HasIndex(tm => new { tm.TeamId, tm.UserId }).IsUnique();

            entity.HasOne(tm => tm.Team)
                  .WithMany(t => t.Members)
                  .HasForeignKey(tm => tm.TeamId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tm => tm.User)
                  .WithMany(u => u.TeamMemberships)
                  .HasForeignKey(tm => tm.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ═══════════════════════════════════════════
        // PROJECT
        // ═══════════════════════════════════════════
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Status).HasDefaultValue("Active");

            entity.HasOne(p => p.Owner)
                  .WithMany(u => u.OwnedProjects)
                  .HasForeignKey(p => p.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Project -> Team (Many-to-One, optional)
            entity.HasOne(p => p.Team)
                  .WithMany(t => t.Projects)
                  .HasForeignKey(p => p.TeamId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);

            entity.HasIndex(p => p.OwnerId);
            entity.HasIndex(p => p.TeamId);
        });

        // ═══════════════════════════════════════════
        // PROJECT MEMBER (Join Table with extra payload)
        // ═══════════════════════════════════════════
        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.ToTable("ProjectMembers");
            entity.HasKey(pm => pm.Id);

            entity.HasIndex(pm => new { pm.ProjectId, pm.UserId }).IsUnique();

            entity.HasOne(pm => pm.Project)
                  .WithMany(p => p.Members)
                  .HasForeignKey(pm => pm.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pm => pm.User)
                  .WithMany(u => u.ProjectMemberships)
                  .HasForeignKey(pm => pm.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ═══════════════════════════════════════════
        // TASK ITEM
        // ═══════════════════════════════════════════
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).IsRequired().HasMaxLength(300);
            entity.Property(t => t.Status).HasDefaultValue("Todo");
            entity.Property(t => t.Priority).HasDefaultValue("Medium");
            entity.Property(t => t.Position).HasDefaultValue(0);

            entity.HasOne(t => t.Project)
                  .WithMany(p => p.Tasks)
                  .HasForeignKey(t => t.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(t => t.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(t => t.ProjectId);
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.Priority);
        });

        // ═══════════════════════════════════════════
        // LABEL
        // ═══════════════════════════════════════════
        modelBuilder.Entity<Label>(entity =>
        {
            entity.ToTable("Labels");
            entity.HasKey(l => l.Id);
            
            entity.HasOne(l => l.Project)
                  .WithMany(p => p.Labels)
                  .HasForeignKey(l => l.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasIndex(l => l.ProjectId);
        });

        // ═══════════════════════════════════════════
        // TASK LABEL (Join Table)
        // ═══════════════════════════════════════════
        modelBuilder.Entity<TaskLabel>(entity =>
        {
            entity.ToTable("TaskLabels");
            entity.HasKey(tl => new { tl.TaskId, tl.LabelId });

            entity.HasOne(tl => tl.Task)
                  .WithMany(t => t.TaskLabels)
                  .HasForeignKey(tl => tl.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tl => tl.Label)
                  .WithMany(l => l.TaskLabels)
                  .HasForeignKey(tl => tl.LabelId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ═══════════════════════════════════════════
        // CHECKLIST
        // ═══════════════════════════════════════════
        modelBuilder.Entity<Checklist>(entity =>
        {
            entity.ToTable("Checklists");
            entity.HasKey(c => c.Id);

            entity.HasOne(c => c.Task)
                  .WithMany(t => t.Checklists)
                  .HasForeignKey(c => c.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasIndex(c => c.TaskId);
        });

        // ═══════════════════════════════════════════
        // CHECKLIST ITEM
        // ═══════════════════════════════════════════
        modelBuilder.Entity<ChecklistItem>(entity =>
        {
            entity.ToTable("ChecklistItems");
            entity.HasKey(ci => ci.Id);

            entity.HasOne(ci => ci.Checklist)
                  .WithMany(c => c.Items)
                  .HasForeignKey(ci => ci.ChecklistId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasIndex(ci => ci.ChecklistId);
        });

        // ═══════════════════════════════════════════
        // TASK ASSIGNMENT (Join Table)
        // ═══════════════════════════════════════════
        modelBuilder.Entity<TaskAssignment>(entity =>
        {
            entity.ToTable("TaskAssignments");
            entity.HasKey(ta => ta.Id);

            entity.HasIndex(ta => new { ta.TaskId, ta.UserId }).IsUnique();

            entity.HasOne(ta => ta.Task)
                  .WithMany(t => t.Assignments)
                  .HasForeignKey(ta => ta.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ta => ta.User)
                  .WithMany(u => u.TaskAssignments)
                  .HasForeignKey(ta => ta.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            // AssignedByUser uses a separate FK to avoid multiple cascade paths
            entity.HasOne(ta => ta.AssignedByUser)
                  .WithMany()
                  .HasForeignKey(ta => ta.AssignedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ═══════════════════════════════════════════
        // COMMENT
        // ═══════════════════════════════════════════
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToTable("Comments");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Content).IsRequired().HasMaxLength(5000);

            entity.HasOne(c => c.Task)
                  .WithMany(t => t.Comments)
                  .HasForeignKey(c => c.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.User)
                  .WithMany(u => u.Comments)
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => c.TaskId);
        });

        // ═══════════════════════════════════════════
        // ATTACHMENT
        // ═══════════════════════════════════════════
        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.ToTable("Attachments");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.OriginalFileName).IsRequired().HasMaxLength(500);
            entity.Property(a => a.FilePath).IsRequired().HasMaxLength(1000);

            entity.HasOne(a => a.Task)
                  .WithMany(t => t.Attachments)
                  .HasForeignKey(a => a.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.UploadedByUser)
                  .WithMany(u => u.Attachments)
                  .HasForeignKey(a => a.UploadedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => a.TaskId);
        });

        // ═══════════════════════════════════════════
        // NOTIFICATION
        // ═══════════════════════════════════════════
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Type).IsRequired().HasMaxLength(100);
            entity.Property(n => n.Title).IsRequired().HasMaxLength(500);
            entity.Property(n => n.IsRead).HasDefaultValue(false);

            entity.HasOne(n => n.User)
                  .WithMany(u => u.Notifications)
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(n => n.UserId);
            entity.HasIndex(n => n.IsRead);
        });

        // ═══════════════════════════════════════════
        // ACTIVITY LOG (append-only / no soft delete)
        // ═══════════════════════════════════════════
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.ToTable("ActivityLogs");
            entity.HasKey(al => al.Id);
            entity.Property(al => al.Action).IsRequired().HasMaxLength(100);

            entity.HasOne(al => al.User)
                  .WithMany(u => u.ActivityLogs)
                  .HasForeignKey(al => al.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(al => al.UserId);
            entity.HasIndex(al => al.Timestamp);
            entity.HasIndex(al => al.EntityType);
        });

        // ═══════════════════════════════════════════
        // TEAM INVITATION
        // ═══════════════════════════════════════════
        modelBuilder.Entity<TeamInvitation>(entity =>
        {
            entity.ToTable("TeamInvitations");
            entity.HasKey(ti => ti.Id);
            entity.HasIndex(ti => ti.Token).IsUnique();

            entity.HasOne(ti => ti.Team)
                  .WithMany()
                  .HasForeignKey(ti => ti.TeamId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ti => ti.InvitedByUser)
                  .WithMany()
                  .HasForeignKey(ti => ti.InvitedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
