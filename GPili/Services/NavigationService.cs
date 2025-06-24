
using GPili.Utils.State;
using ServiceLibrary.Services.Interfaces;

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
    public class NavigationService(IAuth _auth, IInventory _inventory) : INavigationService
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

            if (result.isSuccess)
            {
                CashierState.CashierName = result.cashierName;
                CashierState.CashierEmail = result.cashierEmail;
                
                await NavigateToAsync(AppRoutes.Cashiering);
            }
            else
            {
                CashierState.CashierName = string.Empty;
                CashierState.CashierEmail = string.Empty;

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
