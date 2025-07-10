using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace GPili.Presentation.Popups.Manager
{
    public partial class SelectionOfDateViewModel : ObservableObject
    {
        private readonly IPopupService _popupService;

        public SelectionOfDateViewModel(IPopupService popupService, bool isRangeMode = true)
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

        public event EventHandler<object?>? CloseRequested;

        [RelayCommand]
        public void ReturnSelected()
        {
            if (IsRangeMode)
            {
                if (SelectedFromDate > SelectedToDate)
                {
                    _ = Snackbar.Make("Invalid date range selected.", duration: TimeSpan.FromSeconds(1))
                                  .Show();
                    return;
                }

                // trigger the event with the tuple
                CloseRequested?.Invoke(this, (SelectedFromDate, SelectedToDate));
            }
            else
            {
                CloseRequested?.Invoke(this, SelectedDate);
            }
        }
    }
}
