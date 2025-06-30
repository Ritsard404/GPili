using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace GPili.Presentation.Popups
{
    public partial class EPaymentViewModel : ObservableValidator
    {
        private readonly IPopupService _popupService;
        private readonly IEPayment _ePayment;

        public EPaymentView Popup;

        [ObservableProperty]
        private ObservableCollection<EPaymentEntryViewModel> _paymentEntries = new();

        public double PopupWidth => Shell.Current.CurrentPage.Width * 0.5;
        public double PopupHeight => Shell.Current.CurrentPage.Height * 0.7;

        public EPaymentViewModel(IPopupService popupService, IEPayment ePayment)
        {
            _popupService = popupService;
            _ePayment = ePayment;
        }

        public async Task LoadSaleTypes()
        {
            PaymentEntries.Clear();
            var saleTypes = await _ePayment.SaleTypes();

            if (saleTypes == null || !saleTypes.Any())
            {
                await Shell.Current.DisplayAlert("Unavailalbe", "No E-Payments yet!", "Close");
                Popup.Close();
                return;
            }

            foreach (var saleType in saleTypes)
            {
                PaymentEntries.Add(new EPaymentEntryViewModel(saleType));
            }
        }

        [RelayCommand]
        private void Submit()
        {
            bool hasError = false;
            foreach (var entry in PaymentEntries)
            {
                entry.ValidateAll();
                if (entry.HasErrors)
                    hasError = true;
            }
            if (!hasError)
            {
                Popup?.CloseWithResult(PaymentEntries.ToList());
            }
        }
    }

    public partial class EPaymentEntryViewModel : ObservableValidator
    {
        public SaleType SaleType { get; }

        [ObservableProperty]
        [Required(ErrorMessage = "Reference is required.")]
        private string reference = string.Empty;

        [ObservableProperty]
        [Range(0.001, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        private decimal amount;

        public string Name => SaleType.Name;

        public EPaymentEntryViewModel(SaleType saleType)
        {
            SaleType = saleType;
        }
        public void ValidateAll()
        {
            ValidateAllProperties();
        }
    }
}
