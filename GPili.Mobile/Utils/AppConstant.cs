using GPili.Mobile.Presentation.Features.Cashier;
using GPili.Mobile.Presentation.Features.Manager;
using ServiceLibrary.Utils;

namespace GPili.Mobile.Utils
{
    public class AppConstant
    {
        public async static Task AddTabMenus()
        {
            var routesToRemove = new[]{
                nameof(CartPage),
                nameof(CashierPage),
                nameof(TenderPage),
                nameof(ManagerPage),
                nameof(DataPage),
                nameof(ReportPage),
            };

            foreach (var route in routesToRemove)
            {
                var existingItem = AppShell.Current.Items.FirstOrDefault(f => f.Route == route);
                if (existingItem != null)
                {
                    AppShell.Current.Items.Remove(existingItem);
                }
            }

            if (string.IsNullOrEmpty(App.UserInfo.Email))
            {
                await Shell.Current.GoToAsync(AppRoutes.Login);
                return;
            }

            if (App.UserInfo.Role == RoleType.Cashier)
            {
                var tab = new TabBar()
                {
                    Title = "Menu Page",
                    Route = nameof(CashierPage),
                    Items =
                    {
                        new ShellContent
                        {
                            Icon = AppIcons.Menu,
                            Title = "Menu",
                            ContentTemplate = new DataTemplate(typeof(CashierPage)),
                        },
                        new ShellContent
                        {
                            Icon = AppIcons.Cart,
                            Title = "Cart",
                            ContentTemplate = new DataTemplate(typeof(CartPage)),
                        },
                        new ShellContent
                        {
                            Icon = AppIcons.Tender,
                            Title = "Tender",
                            ContentTemplate = new DataTemplate(typeof(TenderPage)),
                        },
                    }
                };

                if (!AppShell.Current.Items.Contains(tab))
                {
                    AppShell.Current.Items.Add(tab);
                    await Shell.Current.GoToAsync(AppRoutes.Cashiering);

                }

            }


            if (App.UserInfo.Role == RoleType.Manager || App.UserInfo.Role == RoleType.Developer)
            {
                var tab = new TabBar()
                {
                    Title = "Sales Page",
                    Route = nameof(ManagerPage),
                    Items =
                    {
                        new ShellContent
                        {
                            Icon = AppIcons.Manager,
                            Title = "Sales",
                            ContentTemplate = new DataTemplate(typeof(ManagerPage)),
                        },
                        new ShellContent
                        {
                            Icon = AppIcons.Report,
                            Title = "Report",
                            ContentTemplate = new DataTemplate(typeof(ReportPage)),
                        },
                        new ShellContent
                        {
                            Icon = AppIcons.Data,
                            Title = "Data",
                            ContentTemplate = new DataTemplate(typeof(DataPage)),
                        },
                    }
                };

                if (!AppShell.Current.Items.Contains(tab))
                {
                    AppShell.Current.Items.Add(tab);
                    await Shell.Current.GoToAsync(AppRoutes.Manager);

                }

            }

        }
    }
}
