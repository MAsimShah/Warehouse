using Microsoft.Extensions.DependencyInjection;
using Warehouse.Application.Auth;
using Warehouse.Application.Employees;
using Warehouse.Application.Profile;

namespace Warehouse.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IProfileService, ProfileService>();
        return services;
    }
}
