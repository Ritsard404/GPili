using CommunityToolkit.Maui;
using GPili.Mobile.Presentation.Features.Cashier;
using GPili.Mobile.Presentation.Features.LogIn;
using GPili.Mobile.Presentation.Features.Manager;
using GPili.Mobile.Presentation.Features.Manager.Data;
using GPili.Mobile.Presentation.Features.Manager.Report;
using GPili.Mobile.Presentation.Features.Manager.Sales;
using GPili.Mobile.Presentation.Popups;
using GPili.Mobile.Presentation.Popups.Manager;
using Microsoft.Data.Sqlite;
using ServiceLibrary.Extension;
using ServiceLibrary.Utils;

namespace GPili.Extensions;

internal static class ApplicationExtensions
{
    public static MauiAppBuilder ConfigureApplication(this MauiAppBuilder builder)
    {
        builder.Services
            .AddDatabase()
            .AddApplicationServices()
            .AddService()
            .RegisterViews()
            .RegisterPopups();

        return builder;
    }
    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        var basePath = Path.Combine(
            Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments).AbsolutePath,
            "GPili"
        );
        string dbPath = Path.Combine(basePath,
            "GPili.db"
        );

        // Ensure directory exists
        var dbDirectory = Path.GetDirectoryName(dbPath);
        if (!Directory.Exists(dbDirectory))
            Directory.CreateDirectory(dbDirectory);

        //string connectionString = $"Data Source={dbPath}";
        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Password = FolderPath.Database.Password
        }.ToString();

        services.AddDbContext<DataContext>(options =>
            options.UseSqlite(connectionString, x => x.MigrationsAssembly(nameof(ServiceLibrary))));

        return services;
    }
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        //services.AddSingleton<IPopUpService, PopUpService>();

        return services;
    }

    public static IServiceCollection RegisterViews(this IServiceCollection services)
    {
        // Register your views here
        //services.AddSingleton<AppShell>();

        services.AddPageViewModel<LogInViewModel, LogInPage>();

        // CashierViewModel is shared across CashierPage, TenderPage, CartPage
        services.AddPageViewModel<CashierViewModel, CashierPage>(shared: true);
        services.AddPageViewModel<CashierViewModel, TenderPage>(shared: true);
        services.AddPageViewModel<CashierViewModel, CartPage>(shared: true);

        // ManagerViewModel is shared across ManagerPage, ReportPage, DataPage
        services.AddPageViewModel<ManagerViewModel, ManagerPage>(shared: true);
        services.AddPageViewModel<ManagerViewModel, ReportPage>(shared: true);
        services.AddPageViewModel<ManagerViewModel, DataPage>(shared: true);

        // Sales
        services.AddPageViewModel<SalesViewModel, TranxListPage>(shared: true);
        services.AddPageViewModel<SalesViewModel, RefundInvoicePage>(shared: true);

        //Report
        services.AddPageViewModel<ReportViewModel, SalesHistoryPage>(shared: true);
        services.AddPageViewModel<ReportViewModel, AuditTrailPage>(shared: true);
        services.AddPageViewModel<ReportViewModel, DailyTranxPage>(shared: true);
        services.AddPageViewModel<ReportViewModel, VoidedListPage>(shared: true);
        services.AddPageViewModel<ReportViewModel, SalesBookPage>(shared: true);
        services.AddPageViewModel<ReportViewModel, PwdOrScListPage>(shared: true);

        // Data
        services.AddPageViewModel<ProductsViewModel, CategoryPage>(shared: true);
        services.AddPageViewModel<ProductsViewModel, ProductPage>(shared: true);
        services.AddPageViewModel<DataViewModel, SaleTypePage>(shared: true);
        services.AddPageViewModel<DataViewModel, SettingPage>(shared: true);
        services.AddPageViewModel<UsersViewModel, UsersPage>();

        return services;
    }

    public static IServiceCollection RegisterPopups(this IServiceCollection services)
    {
        // Register your popups here
        //services.AddTransientPopup<LoaderView, LoaderViewModel>();
        services.AddTransientPopup<ManagerAuthView, ManagerAuthViewModel>();
        services.AddTransientPopup<EditItemView, EditItemViewModel>();
        services.AddTransientPopup<EPaymentView, EPaymentViewModel>();
        services.AddTransientPopup<DiscountView, DiscountViewModel>();

        // Manager
        services.AddTransientPopup<DateSelectionPopup, SelectionOfDateViewModel>();
        //services.AddTransientPopup<TerminalMachinePopup, TerminalMachineViewModel>();
        //services.AddTransientPopup<SaveProduct, SaveProductViewModel>();
        //services.AddTransientPopup<CategoriesView, ProductsViewModel>();

        return services;
    }

    private static void AddPageViewModel<TViewModel, TView>(
    this IServiceCollection services,
    bool shared = false
)
    where TView : ContentPage, new()
    where TViewModel : class
    {
        if (shared)
            services.AddSingleton<TViewModel>();
        else
            services.AddTransient<TViewModel>();

        services.AddTransient<TView>(s => new TView
        {
            BindingContext = s.GetRequiredService<TViewModel>()
        });
    }

}