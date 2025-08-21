using GPili.Mobile.Utils;
using GPili.Mobile.Utils.State;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;

namespace GPili.Mobile.Services
{
    public interface INavigationService
    {
        Task InitializeAsync();
        Task NavigateToAsync(string route, IDictionary<string, object> routeParameters = null);
        Task GoBack();
        Task GoToManager(string? managerEmail = null, bool isDeveloper = false);
        Task Logout();
    }
    public class NavigationService(IAuth _auth, IGPiliTerminalMachine _terminalMachine) : INavigationService
    {
        public async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }
        public async Task GoToManager(string? managerEmail = null, bool isDeveloper = false)
        {
            await NavigateToAsync(AppRoutes.Manager, 
                new Dictionary<string, object>
                {
                    {"ManagerEmail", managerEmail },
                    {"IsDeveloper", isDeveloper }
                });
        }
        public async Task InitializeAsync()
        {
            var result = await _auth.HasPendingOrder();

            POSInfo.Terminal = await _terminalMachine.GetTerminalInfo();

            if (result.isSuccess)
            {

                CashierState.Info.UpdateCashierInfo(result.cashierName, result.cashierEmail, RoleType.Cashier);
                App.UserInfo = new UserInfo
                {
                    Email = result.cashierEmail,
                    Name = result.cashierName,
                    Role = RoleType.Cashier
                };

                await AppConstant.AddTabMenus();
            }
            else
            {
                CashierState.Info.Reset();
                await NavigateToAsync(AppRoutes.Login);
            }

        }
        public async Task Logout()
        {
            await NavigateToAsync(AppRoutes.Login);
            App.UserInfo = null;
            CashierState.Info.Reset();
        }
        public Task NavigateToAsync(string route, IDictionary<string, object> routeParameters =
            null)
        {
            return
                routeParameters != null
                    ? Shell.Current.GoToAsync(route, routeParameters)
                    : Shell.Current.GoToAsync(route);
        }
    }
}
