namespace GPili.Presentation.Contents.Cashiering;

public partial class ProductSelectionView : ContentView
{
	public ProductSelectionView()
	{
		InitializeComponent();
	}
    protected override void OnParentSet()
    {
        base.OnParentSet();

        SearchEntry.Focus();

    }

    private void OnPointerEntered(object sender, PointerEventArgs e)
    {
        if (sender is Grid grid)
        {
            grid.BackgroundColor = Colors.LightGray; // or any hover color
        }
    }

    private void OnPointerExited(object sender, PointerEventArgs e)
    {
        if (sender is Grid grid)
        {
            grid.BackgroundColor = Colors.Transparent; // or your default color
        }
    }

}