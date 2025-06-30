namespace GPili.Utils.State
{
    public static class PopupState
    {
        public static PopupObservable PopupInfo { get; } = new();
    }

    public partial class PopupObservable : ObservableObject
    {
        [ObservableProperty]
        private bool _isPopupOpen;
        [ObservableProperty]
        private string _popupTitle = string.Empty;
        [ObservableProperty]
        private string _popupMessage = string.Empty;
        [ObservableProperty]
        private bool _isLoading;
        public void OpenPopup(string title, string message)
        {
            PopupTitle = title;
            PopupMessage = message;
            IsPopupOpen = true;
            IsLoading = false;
        }
        public void ClosePopup()
        {
            IsPopupOpen = false;
            PopupTitle = string.Empty;
            PopupMessage = string.Empty;
            IsLoading = false;
        }
    }
}
