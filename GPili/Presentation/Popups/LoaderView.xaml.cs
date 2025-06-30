namespace GPili.Presentation.Popups;

public partial class LoaderView : Popup
{
    public LoaderView(string message = "Loading…")
    {
        PopupState.PopupInfo.OpenPopup("Loading", "Loading");

        InitializeComponent();
        BindingContext = new LoaderViewModel(message);

        Closed += (_, _) => PopupState.PopupInfo.ClosePopup();
    }

    public LoaderViewModel ViewModel
        => (LoaderViewModel)BindingContext;
}