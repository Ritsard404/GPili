using GPili.Mobile.Utils.State;
using ServiceLibrary.Models;

namespace GPili.Mobile.Presentation.Popups;

public partial class EditItemView : Popup
{
	public EditItemView(EditItemViewModel vm)
    {
        PopupState.PopupInfo.OpenPopup("Edit Item", $"Editting item");

        InitializeComponent();

        BindingContext = vm;
        Closed += (_, _) => PopupState.PopupInfo.ClosePopup();

    }

    public void CloseWithResult(object? result = null)
        => Close(result);

    private async void Entry_Completed(object sender, EventArgs e)
    {

        if (BindingContext is EditItemViewModel vm)
        {
            await vm.SaveItem();
        }
    }
}