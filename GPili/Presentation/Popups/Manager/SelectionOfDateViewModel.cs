using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace GPili.Presentation.Popups.Manager
{
    public partial class SelectionOfDateViewModel : ObservableObject
    {
        private readonly IPopupService _popupService;

        public SelectionOfDateViewModel(IPopupService popupService, bool isRangeMode = false)
        {
            _popupService = popupService;
            IsRangeMode = isRangeMode;
        }

        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Now;

        [ObservableProperty]
        private DateTime _selectedFromDate = DateTime.Now;
        [ObservableProperty]
        private DateTime _selectedToDate = DateTime.Now.AddDays(1);

        [ObservableProperty]
        private bool _isRangeMode;

        public double PopupWidth => Shell.Current.CurrentPage.Width * 0.35;
        public double PopupHeight => Shell.Current.CurrentPage.Height * 0.3;

        [RelayCommand]
        public async Task ReturnSelected()
        {
            if (IsRangeMode)
            {
                if (SelectedFromDate > SelectedToDate)
                {
                    await Snackbar.Make("Invalid date range selected.",
                        duration: TimeSpan.FromSeconds(1)).Show();

                    return;
                }
                await _popupService.ClosePopupAsync((SelectedFromDate, SelectedToDate));
            }
            else
            {
                await _popupService.ClosePopupAsync(SelectedDate);
            }
        }
    }
}
