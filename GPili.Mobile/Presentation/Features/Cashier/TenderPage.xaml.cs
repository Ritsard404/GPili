namespace GPili.Mobile.Presentation.Features.Cashier;

public partial class TenderPage : ContentPage
{
	public TenderPage()
	{
		InitializeComponent();
    }
    private void CashEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry && string.IsNullOrWhiteSpace(entry.Text))
            entry.Text = "0";
    }
}