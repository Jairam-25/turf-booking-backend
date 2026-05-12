using Application.Common.Settings;
using Application.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Persistence;
using Persistence.Context;
using System.Text;
using TurfBooking.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);


// Add Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Fluent Validation
builder.Services
    .AddFluentValidationAutoValidation();
builder.Services
    .AddValidatorsFromAssemblyContaining
        <RegisterRequestValidator>();

// Database Connection
builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString(
                "DefaultConnection")));

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

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// Run Application
app.Run();