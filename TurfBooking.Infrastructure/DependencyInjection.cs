using Application.Interfaces;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Interfaces;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ITurfService, TurfService>();

        return services;
    }
}