using ServiceLibrary.Models;

namespace GPili.Utils.State
{
    internal static class CashierState
    {
        public static CashierInfo Info { get; } = new();
    }

    public partial class CashierInfo : ObservableObject
    {
        [ObservableProperty]
        private string? _cashierName;
        [ObservableProperty]
        private string? _cashierEmail;

        public void UpdateCashierInfo(string? name, string? email)
        {
            CashierName = name;
            CashierEmail = email;
        }
    }
}
