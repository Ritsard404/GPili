namespace GPili.Mobile.Presentation.Features.Manager.Data;

public partial class CategoryPage : ContentPage
{
	public CategoryPage()
	{
		InitializeComponent();
    }
    protected async override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is DataViewModel vm)
        {
            vm.CurrenDataPage = DataPageType.Categories;
            await vm.InitializeData();
        }
    }
}