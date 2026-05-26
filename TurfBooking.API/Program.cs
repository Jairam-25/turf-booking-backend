using Application.Common.Settings;
using Application.Validators;
using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using HealthChecks.UI.Client;
using Infrastructure;
using Infrastructure.Hubs;
using Infrastructure.Services;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Persistence;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;
using TurfBooking.API.Middlewares;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();


// Add Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TurfXpert API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token here"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Register Redis Distributed Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    // Connection string for local Redis
    options.Configuration = "localhost:6379";

    // Prefix for all cache keys (avoids key conflicts)
    options.InstanceName = "TurfBooking_";
});

// Register Hangfire with SQL Server storage
builder.Services.AddHangfire(config =>
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseSqlServerStorage(
              builder.Configuration.GetConnectionString(
                  "DefaultConnection")));

// Added Hangfire background job server
builder.Services.AddHangfireServer();

// Register Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "sqlserver")
    .AddRedis(
        "localhost:6379",
        name: "redis");

// Define rate limiting policies
builder.Services.AddRateLimiter(options =>
{
    // Policy 1 : Login — max 5 attempts per minute per IP
    options.AddFixedWindowLimiter(
        policyName: "LoginPolicy",
        configureOptions: opt =>
        {
            opt.PermitLimit = 5;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0; // No queuing — reject immediately
        });

    // Policy 2 : ForgotPassword — max 3 attempts per 5 minutes per IP
    options.AddFixedWindowLimiter(
        policyName: "ForgotPasswordPolicy",
        configureOptions: opt =>
        {
            opt.PermitLimit = 3;
            opt.Window = TimeSpan.FromMinutes(5);
            opt.QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });

    // Policy 3 : Register — max 10 per minute
    options.AddFixedWindowLimiter(
        policyName: "RegisterPolicy",
        configureOptions: opt =>
        {
            opt.PermitLimit = 10;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueLimit = 0;
        });

    // Global : What to return when limit is exceeded
    options.RejectionStatusCode = 429; // 429 = Too Many Requests

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;

        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                success = false,
                message = "Too many requests. Please wait and try again.",
                retryAfter = "60 seconds"
            }, token);
    };
});

// Fluent Validation
builder.Services
    .AddFluentValidationAutoValidation();
builder.Services
    .AddValidatorsFromAssemblyContaining
        <RegisterRequestValidator>();

// Dependency Injection
builder.Services.AddPersistence(
    builder.Configuration);
builder.Services.AddInfrastructure();

// Mapster Configuration
builder.Services.AddMapster();
TypeAdapterConfig.GlobalSettings.Scan(typeof(AuthService).Assembly);

builder.Services.AddMediatR(typeof(AuthService).Assembly);

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// SignalR
builder.Services.AddSignalR();

//Configure Jwt Settings
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAngular",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
        });
});

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

var jwtSettings =
    builder.Configuration
        .GetSection("JwtSettings")
        .Get<JwtSettings>();

// JWT Authentication
builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)

    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,

                ValidateAudience = true,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    jwtSettings!.Issuer,

                ValidAudience =
                    jwtSettings.Audience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtSettings.Key))
            };
    });

builder.Services.AddAuthorization();

// Add API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Build App
var app = builder.Build();

// Middleware Pipeline

// Global Exception Middleware
app.UseMiddleware<ExceptionMiddleware>();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

// HTTPS
app.UseHttpsRedirection();

// CORS
app.UseCors("AllowAngular");

app.UseRateLimiter();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

// Map Health Checks Endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Map Controllers
app.MapControllers();

// Map SignalR hubs
app.MapHub<SlotHub>("/hubs/slots");

// Run Application
app.Run();

public partial class Program { }