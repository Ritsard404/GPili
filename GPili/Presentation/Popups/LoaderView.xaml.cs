namespace GPili.Presentation.Popups;

public partial class LoaderView : Popup
{
    public LoaderView(string message = "Loading…")
    {
        InitializeComponent();
        BindingContext = new LoaderViewModel(message);
    }

    public LoaderViewModel ViewModel
        => (LoaderViewModel)BindingContext;
}