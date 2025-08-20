using Microsoft.EntityFrameworkCore;
using Api_TaskManager.Data;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Api_TaskManager.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// ============================
// JWT Configuration
// ============================
var jwtConfig = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtConfig["Key"]!);

// Add authentication with JWT Bearer
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtConfig["Issuer"],
        ValidAudience = jwtConfig["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
        ),
        RoleClaimType = ClaimTypes.Role // Use "Role" claim for role-based authorization
    };
});

// ============================
// Database Context
// ============================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// ============================
// Authorization & Controllers
// ============================
builder.Services.AddAuthorization();
builder.Services.AddControllers();

// ============================
// Swagger/OpenAPI
// ============================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaskManager API",
        Version = "v1"
    });

    // Enable JWT Bearer authentication in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// ============================
// Middlewares Pipeline
// ============================

// 1) Custom global error handling (must come first)
app.UseMiddleware<ErrorHandlingMiddleware>();

// 2) Swagger (only in Development mode)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskManager API v1");
        c.RoutePrefix = string.Empty; // Swagger at root (http://localhost:xxxx/)
    });
}

// 3) HTTPS redirection
app.UseHttpsRedirection();

// 4) Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 5) Controllers mapping
app.MapControllers();

// Run the application
app.Run();