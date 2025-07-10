using EBISX_POS.API.Services.PDF;
using Microsoft.Extensions.DependencyInjection;
using ServiceLibrary.Services;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Services.PDF;
using ServiceLibrary.Services.Repositories;

namespace ServiceLibrary.Extension
{
    public static class ServiceLibraryExtension
    {
        public static IServiceCollection AddService(this IServiceCollection services)
        {
            // PDF
            services.AddScoped<ProductBarcodePDFService>();
            services.AddScoped<TransactionListPDFService>();
            services.AddScoped<AuditTrailPDFService>();
            services.AddScoped<SalesReportPDFService>();
            services.AddScoped<VoidedListPDFService>();


            services.AddScoped<IDatabaseInitializerService, DatabaseInitializerService>();
            services.AddScoped<DataSeedingService>();
            services.AddScoped<IPrinterService, PrinterService>();

            services.AddScoped<IAuditLog, AuditLogRepository>();
            services.AddScoped<IAuth, AuthRepository>();
            services.AddScoped<IGPiliTerminalMachine, GPiliTerminalMachineRepository>();
            services.AddScoped<IReport, ReportRepository>();
            services.AddScoped<IOrder, OrderRepository>();
            services.AddScoped<IInventory, InventoryRepository>();
            services.AddScoped<IEPayment, EPaymentRepository>();

            return services;
        }
    }
}
