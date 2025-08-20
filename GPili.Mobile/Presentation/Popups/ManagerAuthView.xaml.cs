using GPili.Mobile.Utils.State;

namespace GPili.Mobile.Presentation.Popups;

public partial class ManagerAuthView : Popup
{
    private readonly ManagerAuthViewModel _vm;
    public ManagerAuthView(ManagerAuthViewModel vm)
    {
        PopupState.PopupInfo.OpenPopup("Loading", "Loading");

        InitializeComponent();
        BindingContext = _vm = vm;
        Opened += OnPopupOpened;

        Closed += OnPopupClosed;
    }


    private async void OnPopupOpened(object? sender, EventArgs e)
    {
        AuthEntry.Focus();
        await _vm.InitializeAsync();
    }

    private async void OnPopupClosed(object? sender, PopupClosedEventArgs e)
    {
        PopupState.PopupInfo.ClosePopup();
         await _vm.DisposeAsync();
    }

}