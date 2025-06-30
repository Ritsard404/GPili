

using CommunityToolkit.Maui;
using GPili.Presentation.Contents.Cashiering;
using GPili.Presentation.Features.Cashiering;
using GPili.Presentation.Features.LogIn;
using GPili.Presentation.Features.Manager;
using GPili.Presentation.Popups;
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
        //var dbPath = Path.Combine(FileSystem.AppDataDirectory, "GPili.db");

        if(!Directory.Exists(FolderPath.Database.Test))
            Directory.CreateDirectory(FolderPath.Database.Test);

        var dbPath = Path.Combine(FolderPath.Database.Test, "GPili.db");
        var connectionString = $"Data Source={dbPath}";

        services.AddDbContext<DataContext>(options =>
            options.UseSqlite(connectionString, x => x.MigrationsAssembly(nameof(ServiceLibrary))));

        return services;
    }
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IPopUpService, PopUpService>();

        return services;
    }

    public static IServiceCollection RegisterViews(this IServiceCollection services)
    {
        // Register your views here
        services.AddSingleton<AppShell>();

        services.AddPageViewModel<LogInViewModel, LogInPage>();
        services.AddPageViewModel<CashieringViewModel, CashieringPage>();
        services.AddPageViewModel<ManagerViewModel, ManagerPage>();

        return services;
    }

    public static IServiceCollection RegisterPopups(this IServiceCollection services)
    {
        // Register your popups here
        services.AddTransientPopup<LoaderView, LoaderViewModel>();
        services.AddTransientPopup<ManagerAuthView, ManagerAuthViewModel>();
        services.AddTransientPopup<EditItemView, EditItemViewModel>();

        return services;
    }

    private static void AddPageViewModel<TViewModel, TView>(this IServiceCollection services)
        where TView : ContentPage, new()
        where TViewModel : class
    {
        services.AddTransient<TViewModel>();
        services.AddTransient<TView>(s => new TView() { BindingContext = s.GetRequiredService<TViewModel>() });
    }
}