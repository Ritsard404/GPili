
namespace GPili.Presentation.Popups
{
    public partial class LoaderViewModel : ObservableObject
    {

        [ObservableProperty]
        private string _message;

        public LoaderViewModel(string message)
        {
            Message = message;
        }

    }
}
