namespace GPili.Presentation.Popups;

public partial class ManagerAuthView : Popup
{
	public ManagerAuthView(ManagerAuthViewModel vm)
    {
        PopupState.PopupInfo.OpenPopup("Loading", "Loading");

        InitializeComponent();
		BindingContext = vm;
        Opened += OnPopupOpened;

        Closed += (_, _) => PopupState.PopupInfo.ClosePopup();
    }

    private void OnPopupOpened(object? sender, EventArgs e)
    {
        AuthEntry.Focus();
    }

}