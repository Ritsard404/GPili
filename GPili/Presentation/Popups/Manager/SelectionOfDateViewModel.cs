using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace GPili.Presentation.Popups.Manager
{
    public partial class SelectionOfDateViewModel : ObservableObject
    {
        private readonly IPopupService _popupService;

        public SelectionOfDateViewModel(IPopupService popupService, bool isRangeMode = true, bool isPwdOrSenior = false)
        {
            _popupService = popupService;
            IsRangeMode = isRangeMode;
            IsPwdOrSenior = isPwdOrSenior;
        }

        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Now;

        [ObservableProperty]
        private DateTime _selectedFromDate = DateTime.Now;
        [ObservableProperty]
        private DateTime _selectedToDate = DateTime.Now.AddDays(1);

        [ObservableProperty]
        private bool _isRangeMode;
        [ObservableProperty]
        private bool _isPwdOrSenior;
        [ObservableProperty]
        private string _discType =  "";

        public double PopupWidth => Shell.Current.CurrentPage.Width * 0.35;
        public double PopupHeight => Shell.Current.CurrentPage.Height * 0.3;

        public event EventHandler<object?>? CloseRequested;

        [RelayCommand]
        public void ReturnSelected()
        {
            if (IsRangeMode)
            {
                if (SelectedFromDate > SelectedToDate)
                {
                    Shell.Current.DisplayAlert("Invalid Date!", "Invalid date range selected.", "Ok");
                    return;
                }
                if (IsPwdOrSenior)
                {
                    if (string.IsNullOrEmpty(DiscType))
                    {
                        Shell.Current.DisplayAlert("Invalid", "Select List Type.", "Ok");

                        return;
                    }

                    // trigger the event with the tuple
                    CloseRequested?.Invoke(this, (SelectedFromDate, SelectedToDate, DiscType));
                }
                else
                {
                    // trigger the event with the tuple
                    CloseRequested?.Invoke(this, (SelectedFromDate, SelectedToDate));
                }
            }
            else
            {
                CloseRequested?.Invoke(this, SelectedDate);
            }
        }
    }
}
