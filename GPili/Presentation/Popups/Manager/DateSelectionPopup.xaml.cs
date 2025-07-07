using CommunityToolkit.Maui.Views;

namespace GPili.Presentation.Popups.Manager;

public partial class DateSelectionPopup : Popup
{
    public DateSelectionPopup(SelectionOfDateViewModel vm)
    {
        PopupState.PopupInfo.OpenPopup("Loading", "Loading");
        InitializeComponent();
        BindingContext = vm;
        Closed += (_, _) => PopupState.PopupInfo.ClosePopup();
    }
} 