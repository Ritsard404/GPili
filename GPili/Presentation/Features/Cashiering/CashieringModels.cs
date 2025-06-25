using ServiceLibrary.Models;

namespace GPili.Presentation.Features.Cashiering
{
    public partial class InitialItem : ObservableObject
    {

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsInitialDisplay))]
        private decimal? _initialQty = 1;

        private bool IsInitialDisplay => InitialQty != null;
    }

    public partial class ItemTotals : ObservableObject
    {
        [ObservableProperty]
        private decimal  _totalAmount = 0;
    }
}
