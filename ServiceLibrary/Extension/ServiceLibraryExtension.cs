using Microsoft.Extensions.DependencyInjection;
using ServiceLibrary.Services;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Services.Repositories;

namespace ServiceLibrary.Extension
{
    public static class ServiceLibraryExtension
    {
        public static IServiceCollection AddService(this IServiceCollection services)
        {
            services.AddScoped<IDatabaseInitializerService, DatabaseInitializerService>();
            services.AddScoped<DataSeedingService>();

            services.AddScoped<IAuditLog, AuditLogRepository>();
            services.AddScoped<IAuth, AuthRepository>();
            services.AddScoped<IGPiliTerminalMachine, GPiliTerminalMachineRepository>();
            services.AddScoped<IReport, ReportRepository>();
            services.AddScoped<IOrder, OrderRepository>();

            return services;
        }
    }
}
