using Application.Interfaces;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Infrastructure.Hubs;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ITurfService, TurfService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<ISlotService, SlotService>();
        services.AddScoped<IReviewService, ReviewService>();

        // Background service: generates daily slots for all turfs on startup + every 24 h
        services.AddHostedService<DailySlotGeneratorService>();

        // SignalR hub context is provided by the framework when AddSignalR is called in the API project.

        return services;
    }
}