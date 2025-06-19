using GPili.Presentation.Features.Cashiering;
using GPili.Presentation.Features.LogIn;
using GPili.Presentation.Features.Manager;

namespace GPili
{
    public partial class AppShell : Shell
    {
        private readonly INavigationService _navigationService;
        public AppShell(INavigationService navigationService)
        {
            InitializeComponent();

            _navigationService = navigationService;
            RegisterRoutes();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            _navigationService.InitializeAsync();
        }

        private void RegisterRoutes()
        {
            Routing.RegisterRoute(AppRoutes.Login, typeof(LogInPage));
            Routing.RegisterRoute(AppRoutes.Cashiering, typeof(CashieringPage));
            Routing.RegisterRoute(AppRoutes.Manager, typeof(ManagerPage));
        }
    }
}
