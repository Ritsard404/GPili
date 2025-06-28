namespace GPili.Presentation.Popups;

public partial class ManagerAuthView : Popup
{
	public ManagerAuthView(ManagerAuthViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
        Opened += OnPopupOpened;
    }

    private void OnPopupOpened(object? sender, EventArgs e)
    {
        AuthEntry.Focus();
    }

    private async void Auth_Completed(object sender, EventArgs e)
    {

        if (BindingContext is ManagerAuthViewModel vm)
        {
            await vm.ValidateManagerEmail();
        }
    }
}