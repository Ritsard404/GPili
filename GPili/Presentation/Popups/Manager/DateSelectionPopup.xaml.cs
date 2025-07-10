using CommunityToolkit.Maui.Views;

namespace GPili.Presentation.Popups.Manager;

public partial class DateSelectionPopup : Popup
{
    public DateSelectionPopup(SelectionOfDateViewModel vm)
    {
        PopupState.PopupInfo.OpenPopup("Loading", "Loading");
        InitializeComponent();
        BindingContext = vm;

        // whenever the VM fires CloseRequested, close THIS popup with that result
        vm.CloseRequested += (_, result) =>
        {
            // this will cause ShowPopupAsync(...) to return ‘result’
            Close(result);
        };
        Closed += (_, _) => PopupState.PopupInfo.ClosePopup();
    }
} 