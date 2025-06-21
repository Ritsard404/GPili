namespace GPili.Presentation.Features.Manager
{
    public partial class ManagerViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;

        public ManagerViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        [RelayCommand]
        public async Task LogOut()
        {
            await _navigationService.Logout();
        }
    }
}
