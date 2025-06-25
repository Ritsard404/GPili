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

}