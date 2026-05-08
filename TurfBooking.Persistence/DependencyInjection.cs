using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TurfBooking.Application.Interfaces;
using TurfBooking.Persistence.Context;
using TurfBooking.Persistence.Repositories;

namespace TurfBooking.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped(typeof(IGenericRepository<>),
                           typeof(GenericRepository<>));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}