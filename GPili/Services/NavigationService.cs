
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
        Task GoToManager();
        Task Logout();
    }
    public class NavigationService(IAuth _auth, IInventory _inventory, IGPiliTerminalMachine _terminalMachine) : INavigationService
    {
        public async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }
        public async Task GoToManager()
        {
            await NavigateToAsync(AppRoutes.Manager);
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
                CashierState.Info.UpdateCashierInfo("", "", "");
                await NavigateToAsync(AppRoutes.Login);
            }

        }
        public async Task Logout()
        {
            await NavigateToAsync(AppRoutes.Login);
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
