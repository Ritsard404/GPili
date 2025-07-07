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

        public double PopupWidth => Shell.Current.CurrentPage.Width * 4;
        public double PopupHeight => Shell.Current.CurrentPage.Height * 4;

        [RelayCommand]
        public void ReturnSelected()
        {
            if (IsRangeMode)
            {
                if (SelectedFromDate > SelectedToDate)
                {
                    Shell.Current.DisplayAlert("Error", "Invalid date range selected.", "OK");

                    return;
                }
                _popupService.ClosePopup((SelectedFromDate, SelectedToDate));
            }
            else
            {
                _popupService.ClosePopup(SelectedDate);
            }
        }
    }
}
