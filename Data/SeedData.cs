using Microsoft.EntityFrameworkCore;
using TrelloApi.Models;

namespace TrelloApi.Data;

/// <summary>
/// Seeds default Roles and an admin user on application startup.
/// Called from Program.cs after EF migrations are applied.
/// </summary>
public static class SeedData
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // ─────────────────────────────────────────
        // 1. Seed Roles
        // ─────────────────────────────────────────
        if (!await context.Roles.AnyAsync())
        {
            var roles = new List<Role>
            {
                new() { Id = 1, Name = "Admin",  Description = "Full system access" },
                new() { Id = 2, Name = "Member", Description = "Standard user access" },
                new() { Id = 3, Name = "Viewer", Description = "Read-only access" }
            };
            context.Roles.AddRange(roles);
            await context.SaveChangesAsync();
        }

        // ─────────────────────────────────────────
        // 2. Seed Default Admin User
        // ─────────────────────────────────────────
        if (!await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == "admin@trello.com"))
        {
            var admin = new User
            {
                FirstName    = "System",
                LastName     = "Admin",
                Email        = "admin@trello.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
                RoleId       = 1,
                IsActive     = true,
                EmailVerified = true,
                CreatedAt    = DateTime.UtcNow
            };
            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}
