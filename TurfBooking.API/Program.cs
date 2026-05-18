using Application.Common.Settings;
using Application.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Persistence;
using System.Text;
using TurfBooking.API.Middlewares;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);


// Add Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TurfBooking API",
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
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
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

// Map Controllers
app.MapControllers();

// Run Application
app.Run();