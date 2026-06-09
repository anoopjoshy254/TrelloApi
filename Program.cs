using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TrelloApi.Data;
using TrelloApi.Helpers;
using TrelloApi.Middleware;
using TrelloApi.Repositories.Implementations;
using TrelloApi.Repositories.Interfaces;
using TrelloApi.Services.Implementations;
using TrelloApi.Services.Interfaces;
using TrelloApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════
// 1. DATABASE — MySQL via Pomelo EntityFrameworkCore
//    EF Core reads the connection string from appsettings.json and maps
//    all entities to MySQL tables using Fluent API in ApplicationDbContext.
// ═══════════════════════════════════════════════════════════════════════
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)), mySql =>
    {
        mySql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
        mySql.CommandTimeout(60);
    });

    if (builder.Environment.IsDevelopment())
        options.EnableSensitiveDataLogging().EnableDetailedErrors();
});

// ═══════════════════════════════════════════════════════════════════════
// 2. JWT AUTHENTICATION
//    The JWT middleware validates the Bearer token on every protected
//    endpoint. Claims (UserId, Email, Role) are decoded into HttpContext.User.
// ═══════════════════════════════════════════════════════════════════════
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtSecret   = jwtSettings["Secret"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer           = true,
        ValidIssuer              = jwtSettings["Issuer"],
        ValidateAudience         = true,
        ValidAudience            = jwtSettings["Audience"],
        ValidateLifetime         = true,
        ClockSkew                = TimeSpan.Zero // No tolerance for expired tokens
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = ctx =>
        {
            ctx.Response.Headers.Append("Token-Expired", ctx.Exception is SecurityTokenExpiredException ? "true" : "false");
            return System.Threading.Tasks.Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ═══════════════════════════════════════════════════════════════════════
// 3. CONTROLLERS + AUTOMAPPER + FLUENT VALIDATION
//    AutoMapper scans MappingProfile automatically.
//    FluentValidation scans all validators in the assembly.
// ═══════════════════════════════════════════════════════════════════════
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddAutoMapper(typeof(TrelloApi.Mappings.MappingProfile).Assembly);

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<TrelloApi.Validators.Auth.RegisterValidator>();

// ═══════════════════════════════════════════════════════════════════════
// 4. SWAGGER / OPENAPI
//    Configured with JWT Bearer Authorization so developers can test
//    protected endpoints directly in Swagger UI.
// ═══════════════════════════════════════════════════════════════════════
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "TrelloApi — Project Management System",
        Version     = "v1",
        Description = "A complete backend for a Trello-like academic project management system. " +
                      "Built with .NET 8, EF Core, MySQL, JWT Authentication, and Layered Architecture.",
        Contact = new OpenApiContact
        {
            Name  = "TrelloApi",
            Email = "admin@trello.com"
        }
    });

    // JWT Bearer Authorization in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token below. Example: Bearer eyJhbGci..."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    // Group endpoints by controller tag
    c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
    c.DocInclusionPredicate((_, _) => true);
});

// ═══════════════════════════════════════════════════════════════════════
// 5. REPOSITORIES (Scoped — new instance per HTTP request)
//    Repository layer: only responsible for data access, no business logic.
// ═══════════════════════════════════════════════════════════════════════
builder.Services.AddScoped<IUserRepository,         UserRepository>();
builder.Services.AddScoped<ITeamRepository,         TeamRepository>();
builder.Services.AddScoped<IProjectRepository,      ProjectRepository>();
builder.Services.AddScoped<ITaskRepository,         TaskRepository>();
builder.Services.AddScoped<ICommentRepository,      CommentRepository>();
builder.Services.AddScoped<IAttachmentRepository,   AttachmentRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IActivityLogRepository,  ActivityLogRepository>();

// ═══════════════════════════════════════════════════════════════════════
// 6. SERVICES (Scoped — contains all business logic and orchestration)
//    Service layer: validates business rules, orchestrates repositories,
//    sends notifications, logs activity.
// ═══════════════════════════════════════════════════════════════════════
builder.Services.AddScoped<IAuthService,         AuthService>();
builder.Services.AddScoped<IUserService,         UserService>();
builder.Services.AddScoped<ITeamService,         TeamService>();
builder.Services.AddScoped<IProjectService,      ProjectService>();
builder.Services.AddScoped<ITaskService,         TaskService>();
builder.Services.AddScoped<ICommentService,      CommentService>();
builder.Services.AddScoped<IAttachmentService,   AttachmentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IActivityLogService,  ActivityLogService>();
builder.Services.AddScoped<IEmailService,        SmtpEmailService>();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// ═══════════════════════════════════════════════════════════════════════
// 7. HELPERS (Scoped for FileHelper [uses IWebHostEnvironment], Singleton for JwtHelper)
// ═══════════════════════════════════════════════════════════════════════
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<FileHelper>();

// ═══════════════════════════════════════════════════════════════════════
// 8. CORS — Allow all in development (restrict in production)
// ═══════════════════════════════════════════════════════════════════════
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

// ═══════════════════════════════════════════════════════════════════════
// BUILD THE APP
// ═══════════════════════════════════════════════════════════════════════
var app = builder.Build();

// ═══════════════════════════════════════════════════════════════════════
// 9. RUN DATABASE MIGRATIONS + SEED ON STARTUP
//    Automatically creates the database and seeds default roles/admin user.
//    In production, consider running migrations as a separate deployment step.
// ═══════════════════════════════════════════════════════════════════════
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        await SeedData.SeedAsync(db);
        app.Logger.LogInformation("✅ Database migration and seeding completed.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "❌ Database migration failed. Ensure MySQL is running and credentials are correct.");
    }
}

// ═══════════════════════════════════════════════════════════════════════
// 10. MIDDLEWARE PIPELINE
//     Order matters! Exception handler must be FIRST so it catches errors
//     from all subsequent middleware.
//
//     Flow:
//     Request → GlobalExceptionMiddleware
//             → RequestLoggingMiddleware
//             → HTTPS Redirection
//             → CORS
//             → Authentication (JWT validation)
//             → Authorization (Role checks)
//             → Controller
// ═══════════════════════════════════════════════════════════════════════
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Swagger — available in all environments for this academic project
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TrelloApi v1");
    c.RoutePrefix       = string.Empty; // Serve at root: http://localhost:5000/
    c.DocumentTitle     = "TrelloApi Documentation";
    c.DisplayRequestDuration();
    c.EnableDeepLinking();
    c.EnableFilter();
});

app.UseAuthentication(); // Must come before UseAuthorization
app.UseAuthorization();

app.MapControllers();
app.MapHub<TrelloApi.Hubs.BoardHub>("/boardHub");

app.Run();
