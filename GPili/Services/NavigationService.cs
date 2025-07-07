
using GPili.Utils.State;
using Microsoft.Maui.ApplicationModel.Communication;
using ServiceLibrary.Services.Interfaces;
using ServiceLibrary.Utils;
using System.Xml.Linq;

namespace GPili.Services
{
    public interface INavigationService
    {
        Task InitializeAsync();
        Task NavigateToAsync(string route, IDictionary<string, object> routeParameters = null);
        Task GoBack();
        Task GoToManager(string? managerEmail);
        Task Logout();
    }
    public class NavigationService(IAuth _auth, IGPiliTerminalMachine _terminalMachine) : INavigationService
    {
        public async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }
        public async Task GoToManager(string? managerEmail)
        {
            await NavigateToAsync(AppRoutes.Manager, 
                new Dictionary<string, object>
                {
                    {"ManagerEmail", managerEmail }
                });
        }
        public async Task InitializeAsync()
        {
            var result = await _auth.HasPendingOrder();

            POSInfo.Terminal = await _terminalMachine.GetTerminalInfo();
            if (result.isSuccess)
            {

                CashierState.Info.UpdateCashierInfo(result.cashierName, result.cashierEmail, RoleType.Cashier);

                await NavigateToAsync(AppRoutes.Cashiering);
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
